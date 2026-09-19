using UnityEngine;

// Everything a tracked prefab needs from the image manager at a given moment.
// Keeping it here avoids having the front/back scripts reach into the manager directly.
public sealed class TrackedContentContext
{
    public Camera ArCamera { get; }
    public Transform TrackedImageTransform { get; }
    public Vector3 LocalPosition { get; }
    public Quaternion LocalRotation { get; }
    public Vector3 LocalScale { get; }
    public float FillUnitsPerSecond { get; }
    public float HideFillUnitsPerSecond { get; }

    public TrackedContentContext(
        Camera arCamera,
        Transform trackedImageTransform,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        float fillStepPerTenthSecond)
    {
        ArCamera = arCamera;
        TrackedImageTransform = trackedImageTransform;
        LocalPosition = localPosition;
        LocalRotation = localRotation;
        LocalScale = localScale;

        // This value originally meant "amount every 0.1s". Converting it once here
        // keeps the reveal independent from frame rate.
        FillUnitsPerSecond = fillStepPerTenthSecond / 0.1f;
        HideFillUnitsPerSecond = FillUnitsPerSecond * 1.5f;
    }
}
