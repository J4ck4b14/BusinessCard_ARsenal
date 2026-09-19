using System;
using UnityEngine;

// Tracking wrapper for the game side. It only decides which roots are visible;
// the actual run state stays inside BoardGameController.
public class BackTrackedContentHandler : TrackedContentHandlerBase
{
    public enum BackBoardState
    {
        Hidden,
        Idle,
        GalleryOpen,
        Playing
    }

    [Header("Root references")]
    [SerializeField] private GameObject visibilityRoot;
    [SerializeField] private GameObject boardRoot;
    [SerializeField] private GameObject galleryRoot;
    [SerializeField] private GameObject coinRoot;
    [SerializeField] private GameObject gameViewportRoot;

    [Header("Debug / initial state")]
    [SerializeField] private BackBoardState initialTrackedState = BackBoardState.Idle;

    private bool isVisible;
    private BackBoardState currentState = BackBoardState.Hidden;

    public BackBoardState CurrentState => currentState;

    protected override void OnInitialized(TrackedContentContext context)
    {
        if (visibilityRoot == null)
            visibilityRoot = gameObject;

        if (boardRoot == null)
            boardRoot = visibilityRoot;

        currentState = initialTrackedState;
        ApplyVisualState(false);
    }

    public override void OnTrackingFound(TrackedContentContext context)
    {
        SetContext(context);
        AttachToTrackedImage(context);

        if (!isVisible && visibilityRoot != null)
            visibilityRoot.SetActive(true);

        isVisible = true;

        if (currentState == BackBoardState.Hidden)
            currentState = BackBoardState.Idle;

        ApplyVisualState(true);
    }

    public override void OnTrackingLost()
    {
        if (!isVisible)
            return;

        Pause();

        // Keep the board where it was for the remainder of this frame instead of
        // inheriting a bad pose from a marker that has just dropped tracking.
        DetachKeepWorldPose();

        isVisible = false;
        ApplyVisualState(false);
    }

    public override void OnTrackableRemoved(Action destroySelf)
    {
        Pause();
        destroySelf?.Invoke();
    }

    public void SetIdle() => SetState(BackBoardState.Idle);
    public void OpenGallery() => SetState(BackBoardState.GalleryOpen);
    public void CloseGallery() => SetState(BackBoardState.Idle);
    public void StartPlaying() => SetState(BackBoardState.Playing);
    public void StopPlaying() => SetState(BackBoardState.Idle);

    public void SetCoinsVisible(bool visible)
    {
        if (coinRoot != null)
            coinRoot.SetActive(visible);
    }

    protected virtual void Pause()
    {
    }

    private void SetState(BackBoardState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        ApplyVisualState(isVisible);
    }

    private void ApplyVisualState(bool forceShow)
    {
        bool rootVisible = forceShow && currentState != BackBoardState.Hidden;

        if (visibilityRoot != null)
            visibilityRoot.SetActive(rootVisible);

        if (!rootVisible)
            return;

        bool showBoard = currentState == BackBoardState.Idle ||
                         currentState == BackBoardState.GalleryOpen ||
                         currentState == BackBoardState.Playing;

        if (boardRoot != null)
            boardRoot.SetActive(showBoard);

        if (coinRoot != null)
            coinRoot.SetActive(currentState == BackBoardState.Idle);

        if (galleryRoot != null)
            galleryRoot.SetActive(currentState == BackBoardState.GalleryOpen);

        if (gameViewportRoot != null)
            gameViewportRoot.SetActive(currentState == BackBoardState.Playing);
    }
}
