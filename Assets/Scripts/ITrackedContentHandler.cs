using System;

// Small contract shared by both sides of the card. The tracking manager only needs
// to know these four calls, the front/back prefabs decide what each one actually means.
public interface ITrackedContentHandler
{
    void Initialize(TrackedContentContext context);
    void OnTrackingFound(TrackedContentContext context);
    void OnTrackingLost();
    void OnTrackableRemoved(Action destroySelf);
}
