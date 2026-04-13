using UnityEngine;

/// <summary>
/// Convenience base class for `ITrackedContentHandler` implementations.
/// 
/// Responsibilities:
/// - Stores the current `TrackedContentContext` (`ActiveContext`).
/// - Provides a one-time initialization hook plus a context refresh hook.
/// - Provides helper methods to attach/detach the content to the tracked image transform.
/// 
/// See also:
/// - `ImageTrackingAssigner` (calls the interface methods)
/// - `FrontTrackedContentHandler` / `BackTrackedContentHandler` (concrete implementations)
/// </summary>
public abstract class TrackedContentHandlerBase : MonoBehaviour, ITrackedContentHandler
{
    /// <summary>
    /// The most recently supplied context.
    /// Prefer reading from this rather than caching camera/offsets elsewhere.
    /// </summary>
    protected TrackedContentContext ActiveContext { get; private set; }

    // Tracks whether `OnInitialized` has run already.
    private bool initialized;

    /// <summary>
    /// Sets the current context and invokes initialization/refresh hooks.
    /// This is called by `ImageTrackingAssigner` after instantiating the prefab.
    /// </summary>
    public void Initialize(TrackedContentContext context)
    {
        ActiveContext = context;

        // Initialize exactly once; subsequent calls are treated as a context refresh.
        // This helps when values like camera reference or local offsets change.
        if (!initialized)
        {
            initialized = true;
            OnInitialized(context);
        }
        else
        {
            OnContextRefreshed(context);
        }
    }

    /// <summary>
    /// Updates the context and calls `OnContextRefreshed`.
    /// Intended for use by derived classes at the start of `OnTrackingFound`.
    /// </summary>
    protected void SetContext(TrackedContentContext context)
    {
        ActiveContext = context;
        OnContextRefreshed(context);
    }

    /// <summary>
    /// Called once the first time `Initialize` is invoked.
    /// Use this to resolve optional references and perform one-time setup.
    /// </summary>
    protected virtual void OnInitialized(TrackedContentContext context) { }

    /// <summary>
    /// Called whenever the context is replaced after initialization.
    /// Use this to update camera references, constraints, etc.
    /// </summary>
    protected virtual void OnContextRefreshed(TrackedContentContext context) { }

    /// <summary>
    /// Parents this GameObject under the tracked image transform and applies the local offsets
    /// from the context.
    /// 
    /// Commonly called by `OnTrackingFound`.
    /// </summary>
    protected void AttachToTrackedImage(TrackedContentContext context)
    {
        if (context == null || context.TrackedImageTransform == null)
            return;

        // `worldPositionStays: false` ensures the local offsets are applied exactly.
        transform.SetParent(context.TrackedImageTransform, false);
        transform.localPosition = context.LocalPosition;
        transform.localRotation = context.LocalRotation;
        transform.localScale = context.LocalScale;
    }

    /// <summary>
    /// Detaches this GameObject from any parent while preserving its current world pose.
    /// 
    /// This is useful when tracking is lost but you want the content to remain "frozen" in place
    /// during a hide animation (see `FrontTrackedContentHandler.OnTrackingLost`).
    /// </summary>
    protected void DetachKeepWorldPose()
    {
        transform.SetParent(null, true);
    }

    /// <inheritdoc/>
    public abstract void OnTrackingFound(TrackedContentContext context);

    /// <inheritdoc/>
    public abstract void OnTrackingLost();

    /// <inheritdoc/>
    public abstract void OnTrackableRemoved(System.Action destroySelf);
}