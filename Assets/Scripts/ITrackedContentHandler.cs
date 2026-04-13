using System;

/// <summary>
/// Contract implemented by content prefabs that are spawned/managed by `ImageTrackingAssigner`.
/// 
/// Lifecycle (typical):
/// 1) `Initialize` is called once right after instantiation.
/// 2) `OnTrackingFound` / `OnTrackingLost` are called as the `ARTrackedImage` tracking state changes.
/// 3) `OnTrackableRemoved` is called once when the underlying trackable is removed.
/// </summary>
public interface ITrackedContentHandler
{
    /// <summary>
    /// Provides initial dependencies and parameters (camera, tracked image transform, offsets, speeds).
    /// 
    /// Note: this can be called again by a manager if the context needs to be refreshed
    /// (see `TrackedContentHandlerBase.Initialize`).
    /// </summary>
    /// <param name="context">Current tracking/content configuration. See `TrackedContentContext`.</param>
    void Initialize(TrackedContentContext context);

    /// <summary>
    /// Called when the linked `ARTrackedImage` is actively tracked.
    /// Implementations typically attach (or re-attach) themselves under the tracked image transform.
    /// </summary>
    /// <param name="context">Current context for this trackable.</param>
    void OnTrackingFound(TrackedContentContext context);

    /// <summary>
    /// Called when the linked `ARTrackedImage` is not currently tracked.
    /// Implementations typically hide and optionally detach to keep the last world pose.
    /// </summary>
    void OnTrackingLost();

    /// <summary>
    /// Called when the underlying trackable is removed from ARFoundation.
    /// 
    /// Implementations must invoke <paramref name="destroySelf"/> when they are done (immediately or after an animation)
    /// so `ImageTrackingAssigner` can destroy and forget the spawned instance.
    /// </summary>
    /// <param name="destroySelf">Callback to destroy the spawned prefab instance.</param>
    void OnTrackableRemoved(Action destroySelf);
}