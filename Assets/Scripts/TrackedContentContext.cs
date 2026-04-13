using UnityEngine;

/// <summary>
/// Immutable snapshot of parameters needed by an `ITrackedContentHandler` implementation.
/// 
/// This is built by `ImageTrackingAssigner.BuildContext` and passed into handlers so they don't
/// need to reach back into the manager to fetch camera/offset/speed values.
/// </summary>
public sealed class TrackedContentContext
{
    /// <summary>
    /// AR camera used for world-space UI rendering and "face camera" constraints.
    /// See `FrontTrackedContentHandler.SetupCanvasAndConstraint`.
    /// </summary>
    public Camera ArCamera { get; }

    /// <summary>
    /// Transform of the `ARTrackedImage`. Content is typically parented under this transform
    /// while tracking is active (see `TrackedContentHandlerBase.AttachToTrackedImage`).
    /// </summary>
    public Transform TrackedImageTransform { get; }

    /// <summary>
    /// Local pose to apply when attaching under <see cref="TrackedImageTransform"/>.
    /// </summary>
    public Vector3 LocalPosition { get; }

    /// <inheritdoc cref="LocalPosition"/>
    public Quaternion LocalRotation { get; }

    /// <inheritdoc cref="LocalPosition"/>
    public Vector3 LocalScale { get; }

    /// <summary>
    /// Fill speed expressed as "fill amount units per second".
    /// Used by `FrontTrackedContentHandler.ShowRoutine`.
    /// </summary>
    public float FillUnitsPerSecond { get; }

    /// <summary>
    /// Hide speed expressed as "fill amount units per second".
    /// Used by `FrontTrackedContentHandler.HideRoutine`.
    /// </summary>
    public float HideFillUnitsPerSecond { get; }

    /// <param name="arCamera">AR camera used by UI/constraints.</param>
    /// <param name="trackedImageTransform">Transform of the tracked image.</param>
    /// <param name="localPosition">Local position offset for content when attached.</param>
    /// <param name="localRotation">Local rotation offset for content when attached.</param>
    /// <param name="localScale">Local scale for content when attached.</param>
    /// <param name="fillStepPerTenthSecond">
    /// Compatibility parameter: amount added every 0.1 seconds (as configured in `ImageTrackingAssigner.fillSpeed`).
    /// Internally converted to per-second rates.
    /// </param>
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

        // Caller provides a step that used to be applied every 0.1s ("tenth second").
        // Convert to a per-second rate so handlers can multiply by Time.deltaTime.
        FillUnitsPerSecond = fillStepPerTenthSecond / 0.1f;

        // Hide slightly faster than show to feel more responsive.
        HideFillUnitsPerSecond = FillUnitsPerSecond * 1.5f;
    }
}