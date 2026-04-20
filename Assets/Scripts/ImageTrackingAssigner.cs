using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public sealed class ImageTrackingAssigner : MonoBehaviour
{
    [System.Serializable]
    private struct TrackedContentBinding
    {
        // Must match `XRReferenceImage.name` in the reference image library.
        public string referenceImageName;

        // Prefab spawned when that reference image is detected.
        public GameObject prefab;
    }

    private sealed class TrackedInstance
    {
        public GameObject root;
        public ITrackedContentHandler handler;
        public Coroutine pendingLostRoutine;
    }

    [Header("AR")]
    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private Camera arCamera;

    [Header("Bindings")]
    [SerializeField] private TrackedContentBinding[] bindings;

    [Header("Local offsets")]
    [SerializeField] private Vector3 contentLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 contentLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 contentLocalScale = Vector3.one;

    [Header("Front fill")]
    [Tooltip("Same meaning as your old script: amount added every 0.1 seconds.")]
    [SerializeField] private float fillSpeed = 0.1f;

    [Header("Tracking loss grace")]
    [Tooltip("Delay before calling OnTrackingLost when the image briefly drops out of full tracking.")]
    [SerializeField] private float lostTrackingGraceSeconds = 0.15f;

    private readonly Dictionary<string, GameObject> prefabByImageName = new();
    private readonly Dictionary<TrackableId, TrackedInstance> instances = new();

    private void Reset()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void Awake()
    {
        RebuildBindingLookup();
    }

    private void OnValidate()
    {
        fillSpeed = Mathf.Max(0.001f, fillSpeed);
        lostTrackingGraceSeconds = Mathf.Max(0f, lostTrackingGraceSeconds);
        RebuildBindingLookup();
    }

    private void OnEnable()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("ImageTrackingAssigner: Missing ARTrackedImageManager reference.", this);
            return;
        }

        if (arCamera == null)
            arCamera = Camera.main;

        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    private void OnDestroy()
    {
        foreach (var pair in instances)
        {
            var instance = pair.Value;
            if (instance == null)
                continue;

            CancelPendingLost(instance);

            if (instance.root != null)
                Destroy(instance.root);
        }

        instances.Clear();
    }

    /// <summary>
    /// Rebuilds the lookup dictionary that maps reference image names to their corresponding prefabs based on the current bindings. This method is called during `Awake` and `OnValidate` to ensure that any changes to the bindings are reflected in the lookup dictionary. It iterates through each binding, checks for valid reference image names and prefabs, and populates the `prefabByImageName` dictionary while logging warnings for any duplicate or invalid entries. This allows for efficient retrieval of prefabs based on tracked image names during runtime.
    /// </summary>
    private void RebuildBindingLookup()
    {
        prefabByImageName.Clear();

        if (bindings == null)
            return;

        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.referenceImageName))
                continue;

            if (binding.prefab == null)
                continue;

            if (prefabByImageName.ContainsKey(binding.referenceImageName))
            {
                Debug.LogWarning(
                    $"ImageTrackingAssigner: Duplicate binding for \"{binding.referenceImageName}\". Keeping the first one.",
                    this);
                continue;
            }

            prefabByImageName.Add(binding.referenceImageName, binding.prefab);
        }
    }

    /// <summary>
    /// Main event handler for tracked image changes. This method is called whenever the ARTrackedImageManager detects changes in the set of tracked images, including additions, updates, and removals. It iterates through each category of change and calls the appropriate method to handle the creation or updating of content for added and updated images, as well as the removal of content for removed images. This centralizes the logic for responding to tracking changes and ensures that the correct actions are taken based on the type of change detected.
    /// </summary>
    /// <param name="changes"></param>
    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (var trackedImage in changes.added)
            CreateOrUpdateContent(trackedImage);

        foreach (var trackedImage in changes.updated)
            CreateOrUpdateContent(trackedImage);

        foreach (var removed in changes.removed)
            RemoveContent(removed.Key);
    }

    /// <summary>
    /// This method handles both the creation of new content for newly detected images and the updating of existing content for images that are still being tracked. When an image is added or updated, it checks if there is a corresponding prefab for the reference image name. If there is no existing instance for the tracked image, it creates one using the associated prefab. Then it builds the context and checks the tracking state. If the image is currently being tracked, it cancels any pending loss routine and calls `OnTrackingFound` on the handler. If the image is not currently tracked, it schedules a call to `OnTrackingLost` after a grace period to debounce brief tracking dropouts.
    /// </summary>
    /// <param name="trackedImage"></param>
    private void CreateOrUpdateContent(ARTrackedImage trackedImage)
    {
        if (trackedImage == null)
            return;

        var imageName = trackedImage.referenceImage.name;
        if (!prefabByImageName.TryGetValue(imageName, out var prefab) || prefab == null)
            return;

        var trackableId = trackedImage.trackableId;

        if (!instances.TryGetValue(trackableId, out var instance) || instance == null || instance.root == null)
        {
            instance = CreateInstance(trackedImage, prefab, imageName);
            if (instance == null)
                return;

            instances[trackableId] = instance;
        }

        var context = BuildContext(trackedImage);
        var isTracked = trackedImage.trackingState == TrackingState.Tracking;

        if (isTracked)
        {
            // If a loss was pending, tracking came back before grace elapsed.
            CancelPendingLost(instance);
            instance.handler.OnTrackingFound(context);
        }
        else
        {
            // Debounce brief tracking dropouts.
            ScheduleTrackingLost(trackableId, instance);
        }
    }

    /// <summary>
    /// Creates a new instance of the prefab associated with the tracked image and initializes its handler. This method instantiates the prefab as a child of the tracked image's transform, applies the specified local position, rotation, and scale offsets, and then searches for a component that implements `ITrackedContentHandler` within the instantiated hierarchy. If a valid handler is found, it is initialized with the context built from the tracked image. The method returns a `TrackedInstance` object containing references to the root GameObject, the handler, and any pending loss routine (initially null). If there are any issues during this process (e.g., no handler found), it logs an error and returns null.
    /// </summary>
    /// <param name="trackedImage"></param>
    /// <param name="prefab"></param>
    /// <param name="imageName"></param>
    /// <returns></returns>
    private TrackedInstance CreateInstance(ARTrackedImage trackedImage, GameObject prefab, string imageName)
    {
        var root = Instantiate(prefab, trackedImage.transform);
        root.name = $"{prefab.name}_For_{trackedImage.trackableId}";

        root.transform.localPosition = contentLocalPosition;
        root.transform.localRotation = Quaternion.Euler(contentLocalEulerAngles);
        root.transform.localScale = contentLocalScale;

        var handler = FindHandler(root);
        if (handler == null)
        {
            Debug.LogError(
                $"ImageTrackingAssigner: Prefab \"{prefab.name}\" for image \"{imageName}\" needs a component that implements {nameof(ITrackedContentHandler)}.",
                this);
            Destroy(root);
            return null;
        }

        handler.Initialize(BuildContext(trackedImage));

        return new TrackedInstance
        {
            root = root,
            handler = handler,
            pendingLostRoutine = null
        };
    }

    /// <summary>
    /// Interface search utility. This method looks for any component in the prefab instance hierarchy that implements `ITrackedContentHandler` and returns the first one found. This allows for flexibility in prefab design, as the handler can be on the root GameObject or any child, but it also enforces that there must be exactly one handler component somewhere in the hierarchy to manage the tracked content's behavior.
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private static ITrackedContentHandler FindHandler(GameObject root)
    {
        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var behaviour in behaviours)
        {
            if (behaviour is ITrackedContentHandler handler)
                return handler;
        }

        return null;
    }

    /// <summary>
    /// Schedules the invocation of the `OnTrackingLost` method for a specific trackable ID after a predefined grace period. If tracking is lost for an image, this method checks if there is already a pending routine to call `OnTrackingLost` and if not, it starts a coroutine that will wait for the specified number of seconds before invoking the loss handler. If tracking is regained before the grace period elapses, the pending routine will be canceled to prevent calling `OnTrackingLost` erroneously. This allows for brief tracking dropouts without immediately treating them as lost, providing a smoother user experience.
    /// </summary>
    /// <param name="trackableId"></param>
    /// <param name="instance"></param>
    private void ScheduleTrackingLost(TrackableId trackableId, TrackedInstance instance)
    {
        if (instance == null || instance.handler == null)
            return;

        if (instance.pendingLostRoutine != null)
            return;

        if (lostTrackingGraceSeconds <= 0f)
        {
            instance.handler.OnTrackingLost();
            return;
        }

        instance.pendingLostRoutine = StartCoroutine(DelayedTrackingLost(trackableId));
    }

    /// <summary>
    /// Delays the invocation of the `OnTrackingLost` method for a specific trackable ID by a predefined grace period. This coroutine is started when tracking is lost for an image, and it waits for the specified number of seconds before checking if the instance still exists and if tracking has not been regained. If the instance is still valid and tracking has not come back, it calls the handler's `OnTrackingLost` method to notify that the image is considered lost. This allows for brief tracking dropouts without immediately treating them as lost, providing a smoother user experience.
    /// </summary>
    /// <param name="trackableId"></param>
    /// <returns></returns>
    private IEnumerator DelayedTrackingLost(TrackableId trackableId)
    {
        yield return new WaitForSeconds(lostTrackingGraceSeconds);

        if (!instances.TryGetValue(trackableId, out var instance) || instance == null)
            yield break;

        instance.pendingLostRoutine = null;
        instance.handler.OnTrackingLost();
    }

    /// <summary>
    /// Cancels any pending tracking lost routine for the given instance. This is called when tracking is regained before the grace period elapses, ensuring that the `OnTrackingLost` method is not called erroneously after tracking has already been restored. The method checks if there is an active coroutine for pending loss and stops it if necessary, then clears the reference to indicate that there is no longer a pending loss.
    /// </summary>
    /// <param name="instance"></param>
    private void CancelPendingLost(TrackedInstance instance)
    {
        if (instance == null || instance.pendingLostRoutine == null)
            return;

        StopCoroutine(instance.pendingLostRoutine);
        instance.pendingLostRoutine = null;
    }

    /// <summary>
    /// Removes the content associated with the given trackable ID. This is called when an image is removed from tracking (e.g., it goes out of view or is otherwise lost). The method checks if there is an existing instance for the trackable ID, and if so, it calls the handler's `OnTrackableRemoved` method, passing a callback that will destroy the instance once any necessary cleanup is done by the handler. If there is no instance or if the instance's root GameObject is already null, it simply removes the entry from the `instances` dictionary.
    /// </summary>
    /// <param name="trackableId"></param>
    private void RemoveContent(TrackableId trackableId)
    {
        if (!instances.TryGetValue(trackableId, out var instance) || instance == null)
        {
            instances.Remove(trackableId);
            return;
        }

        CancelPendingLost(instance);

        if (instance.root == null)
        {
            instances.Remove(trackableId);
            return;
        }

        instance.handler.OnTrackableRemoved(() => DestroyInstance(trackableId));
    }

    /// <summary>
    /// Destroys the instance associated with the given trackable ID, removing it from the `instances` dictionary. This is meant to be called as a callback from `OnTrackableRemoved`, ensuring that any necessary cleanup or final actions can be performed by the handler before the GameObject is destroyed and the instance is removed from tracking.
    /// </summary>
    /// <param name="trackableId"></param>
    private void DestroyInstance(TrackableId trackableId)
    {
        if (!instances.TryGetValue(trackableId, out var instance) || instance == null)
        {
            instances.Remove(trackableId);
            return;
        }

        CancelPendingLost(instance);

        if (instance.root != null)
            Destroy(instance.root);

        instances.Remove(trackableId);
    }

    /// <summary>
    /// This method builds the context object passed to handlers, ensuring consistency across all handler calls and centralizing the logic for how local offsets are applied.
    /// </summary>
    /// <param name="trackedImage"></param>
    /// <returns></returns>
    private TrackedContentContext BuildContext(ARTrackedImage trackedImage)
    {
        return new TrackedContentContext(
            arCamera,
            trackedImage.transform,
            contentLocalPosition,
            Quaternion.Euler(contentLocalEulerAngles),
            contentLocalScale,
            fillSpeed);
    }
}