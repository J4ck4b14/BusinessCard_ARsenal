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
        // Must be exactly the same as the name in the reference image library.
        public string referenceImageName;
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
    [Tooltip("Amount added every 0.1 seconds, kept this way so the old tuning still matches.")]
    [SerializeField] private float fillSpeed = 0.1f;

    [Header("Tracking loss")]
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
            TrackedInstance instance = pair.Value;
            if (instance == null)
                continue;

            CancelPendingLost(instance);

            if (instance.root != null)
                Destroy(instance.root);
        }

        instances.Clear();
    }

    private void RebuildBindingLookup()
    {
        prefabByImageName.Clear();

        if (bindings == null)
            return;

        foreach (TrackedContentBinding binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.referenceImageName) || binding.prefab == null)
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

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> changes)
    {
        foreach (ARTrackedImage trackedImage in changes.added)
            CreateOrUpdateContent(trackedImage);

        foreach (ARTrackedImage trackedImage in changes.updated)
            CreateOrUpdateContent(trackedImage);

        foreach (var removed in changes.removed)
            RemoveContent(removed.Key);
    }

    private void CreateOrUpdateContent(ARTrackedImage trackedImage)
    {
        if (trackedImage == null)
            return;

        string imageName = trackedImage.referenceImage.name;
        if (!prefabByImageName.TryGetValue(imageName, out GameObject prefab) || prefab == null)
            return;

        TrackableId trackableId = trackedImage.trackableId;

        if (!instances.TryGetValue(trackableId, out TrackedInstance instance) ||
            instance == null || instance.root == null)
        {
            instance = CreateInstance(trackedImage, prefab, imageName);
            if (instance == null)
                return;

            instances[trackableId] = instance;
        }

        TrackedContentContext context = BuildContext(trackedImage);
        bool isTracked = trackedImage.trackingState == TrackingState.Tracking;

        if (isTracked)
        {
            CancelPendingLost(instance);
            instance.handler.OnTrackingFound(context);
        }
        else
        {
            // A tiny grace window stops the content blinking out on one bad camera frame.
            ScheduleTrackingLost(trackableId, instance);
        }
    }

    private TrackedInstance CreateInstance(ARTrackedImage trackedImage, GameObject prefab, string imageName)
    {
        GameObject root = Instantiate(prefab, trackedImage.transform);
        root.name = $"{prefab.name}_For_{trackedImage.trackableId}";

        root.transform.localPosition = contentLocalPosition;
        root.transform.localRotation = Quaternion.Euler(contentLocalEulerAngles);
        root.transform.localScale = contentLocalScale;

        ITrackedContentHandler handler = FindHandler(root);
        if (handler == null)
        {
            Debug.LogError(
                $"ImageTrackingAssigner: Prefab \"{prefab.name}\" for image \"{imageName}\" needs an ITrackedContentHandler.",
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

    private static ITrackedContentHandler FindHandler(GameObject root)
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
        if (instance == null || instance.handler == null || instance.pendingLostRoutine != null)
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
