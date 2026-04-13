using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackingAssigner : MonoBehaviour
{
    [System.Serializable]
    private struct TrackedContentBinding
    {
        // Must match `XRReferenceImage.name` in the reference image library.
        public string referenceImageName;

        // Prefab spawned when that reference image is detected.
        public GameObject prefab;
    }

    /// <summary>
    /// Runtime record for a single spawned prefab keyed by `TrackableId`.
    /// </summary>
    private sealed class TrackedInstance
    {
        public string referenceImageName;
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

    // Lookup table built from `bindings` for quick resolve by reference image name.
    private readonly Dictionary<string, GameObject> prefabByImageName = new();

    // Active spawned instances keyed by ARFoundation trackable id.
    private readonly Dictionary<TrackableId, TrackedInstance> instances = new();

    private void Reset()
    {
        // Convenience: auto-wire when the component is added.
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    private void Awake()
    {
        RebuildBindingLookup();
    }

    private void OnValidate()
    {
        // Prevent division by 0 / "never fills" behaviour.
        fillSpeed = Mathf.Max(0.001f, fillSpeed);
        lostTrackingGraceSeconds = Mathf.Max(0f, lostTrackingGraceSeconds);
        RebuildBindingLookup();
    }

    private void OnEnable()
    {
        if (trackedImageManager == null)
        {
            Debug.LogError("ImageTrackingAssigner: Missing ARTrackedImageManager reference.");
            return;
        }

        // Allow leaving this unassigned in the inspector.
        if (arCamera == null)
            arCamera = Camera.main;

        // Subscribe to ARFoundation changes.
        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    private void OnDisable()
    {
        if (trackedImageManager != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    private void OnDestroy()
    {
        // Ensure spawned prefabs are destroyed when this manager is destroyed.
        foreach (KeyValuePair<TrackableId, TrackedInstance> pair in instances)
        {
            if (pair.Value == null)
                continue;

            CancelPendingLost(pair.Value);

            if (pair.Value.root != null)
                Destroy(pair.Value.root);
        }

        instances.Clear();
    }

    /// <summary>
    /// Builds/refreshes the `referenceImageName -> prefab` lookup.
    /// Call this if you modify `bindings` at runtime.
    /// </summary>
    private void RebuildBindingLookup()
    {
        prefabByImageName.Clear();

        foreach (TrackedContentBinding binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.referenceImageName))
                continue;

            if (binding.prefab == null)
                continue;

            if (prefabByImageName.ContainsKey(binding.referenceImageName))
            {
                Debug.LogWarning($"ImageTrackingAssigner: Duplicate binding for \"{binding.referenceImageName}\". Keeping the first one.");
                continue;
            }

            prefabByImageName.Add(binding.referenceImageName, binding.prefab);
        }
    }

    /// <summary>
    /// ARFoundation callback: added/updated/removed tracked images.
    /// We treat added+updated similarly: (create if needed) then drive handler tracking state.
    /// </summary>
    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (ARTrackedImage trackedImage in changes.added)
            CreateOrUpdateContent(trackedImage);

        foreach (ARTrackedImage trackedImage in changes.updated)
            CreateOrUpdateContent(trackedImage);

        // Note: `removed` is a NativeArray<KeyValuePair<TrackableId, ARTrackedImage>> in recent ARFoundation versions.
        foreach (var removed in changes.removed)
            RemoveContent(removed.Key);
    }

    /// <summary>
    /// Ensures there is a spawned instance for the tracked image and forwards tracking state.
    /// </summary>
    private void CreateOrUpdateContent(ARTrackedImage trackedImage)
    {
        if (trackedImage == null)
            return;

        // IMPORTANT: binding key is `XRReferenceImage.name`.
        string imageName = trackedImage.referenceImage.name;

        if (!prefabByImageName.TryGetValue(imageName, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"ImageTrackingAssigner: No prefab bound for reference image \"{imageName}\".");
            return;
        }

        // Create the instance on first sighting (or if it was destroyed unexpectedly).
        if (!instances.TryGetValue(trackedImage.trackableId, out TrackedInstance instance) ||
            instance == null ||
            instance.root == null)
        {
            instance = CreateInstance(trackedImage, imageName, prefab);

            if (instance == null)
                return;

            instances[trackedImage.trackableId] = instance;
        }

        // Context can change if inspector fields are updated or camera changes.
        TrackedContentContext context = BuildContext(trackedImage);
        bool isTracked = trackedImage.trackingState == TrackingState.Tracking;

        // Drive behaviour based on tracking state.
        if (isTracked)
            instance.handler.OnTrackingFound(context);
        else
            instance.handler.OnTrackingLost();
    }

    /// <summary>
    /// Instantiates the prefab as a child of the tracked image transform and finds the handler.
    /// </summary>
    private TrackedInstance CreateInstance(ARTrackedImage trackedImage, string imageName, GameObject prefab)
    {
        // Spawn under the tracked image so it inherits motion/pose while tracking.
        GameObject root = Instantiate(prefab, trackedImage.transform);
        root.name = $"{prefab.name}_For_{trackedImage.trackableId}";

        // Apply local offsets controlled by this manager.
        root.transform.localPosition = contentLocalPosition;
        root.transform.localRotation = Quaternion.Euler(contentLocalEulerAngles);
        root.transform.localScale = contentLocalScale;

        // Expect the prefab to contain exactly one component that implements ITrackedContentHandler.
        ITrackedContentHandler handler = FindHandler(root);

        if (handler == null)
        {
            Debug.LogError(
                $"ImageTrackingAssigner: Prefab \"{prefab.name}\" for image \"{imageName}\" needs a component that implements ITrackedContentHandler.");
            Destroy(root);
            return null;
        }

        // One-time initialization hook (see `TrackedContentHandlerBase.Initialize`).
        handler.Initialize(BuildContext(trackedImage));

        return new TrackedInstance
        {
            referenceImageName = imageName,
            root = root,
            handler = handler,
            pendingLostRoutine = null
        };
    }

    private ITrackedContentHandler FindHandler(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ITrackedContentHandler handler)
                return handler;
        }

        return null;
    }

    private void ScheduleTrackingLost(TrackableId trackableId, TrackedInstance instance)
    {
        if (instance == null)
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

    private IEnumerator DelayedTrackingLost(TrackableId trackableId)
    {
        yield return new WaitForSeconds(lostTrackingGraceSeconds);

        if (!instances.TryGetValue(trackableId, out TrackedInstance instance) || instance == null)
            yield break;

        instance.pendingLostRoutine = null;
        instance.handler.OnTrackingLost();
    }

    private void CancelPendingLost(TrackedInstance instance)
    {
        if (instance == null || instance.pendingLostRoutine == null)
            return;

        StopCoroutine(instance.pendingLostRoutine);
        instance.pendingLostRoutine = null;
    }


    /// <summary>
    /// Handles trackable removal by delegating to the handler.
    /// The handler is responsible for calling back into <see cref="DestroyInstance"/>
    /// (immediately or after a hide animation).
    /// </summary>
    private void RemoveContent(TrackableId trackableId)
    {
        if (!instances.TryGetValue(trackableId, out TrackedInstance instance) || instance == null)
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
    /// Finalizes destruction and removes the instance from the dictionary.
    /// </summary>
    private void DestroyInstance(TrackableId trackableId)
    {
        if (!instances.TryGetValue(trackableId, out TrackedInstance instance) || instance == null)
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
    /// Creates a context snapshot passed into handler calls.
    /// </summary>
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