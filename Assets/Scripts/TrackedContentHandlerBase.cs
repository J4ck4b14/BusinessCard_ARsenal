using UnityEngine;

// Common tracking setup. Front and back only have to worry about what happens when
// the marker appears or disappears, not about repeating the parenting code.
public abstract class TrackedContentHandlerBase : MonoBehaviour, ITrackedContentHandler
{
    protected TrackedContentContext ActiveContext { get; private set; }

    private bool initialized;

    public void Initialize(TrackedContentContext context)
    {
        ActiveContext = context;

        if (!initialized)
        {
            initialized = true;
            OnInitialized(context);
            return;
        }

        OnContextRefreshed(context);
    }

    protected void SetContext(TrackedContentContext context)
    {
        ActiveContext = context;
        OnContextRefreshed(context);
    }

    protected virtual void OnInitialized(TrackedContentContext context) { }
    protected virtual void OnContextRefreshed(TrackedContentContext context) { }

    protected void AttachToTrackedImage(TrackedContentContext context)
    {
        if (context == null || context.TrackedImageTransform == null)
            return;

        transform.SetParent(context.TrackedImageTransform, false);
        transform.localPosition = context.LocalPosition;
        transform.localRotation = context.LocalRotation;
        transform.localScale = context.LocalScale;
    }

    // Useful while hiding: the content keeps its last world pose even if ARFoundation
    // moves or removes the image transform underneath it.
    protected void DetachKeepWorldPose()
    {
        transform.SetParent(null, true);
    }

    public abstract void OnTrackingFound(TrackedContentContext context);
    public abstract void OnTrackingLost();
    public abstract void OnTrackableRemoved(System.Action destroySelf);
}
