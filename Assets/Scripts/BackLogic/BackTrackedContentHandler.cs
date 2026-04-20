using System;
using UnityEngine;

/// <summary>
/// Back-side tracked content behaviour.
/// 
/// Compared to `FrontTrackedContentHandler`, this implementation is intentionally simple:
/// - When tracking is found: attach to the tracked image and enable a visibility root.
/// - When tracking is lost: optionally pause behaviour, detach (keep world pose), and disable.
/// - When trackable is removed: pause and destroy immediately.
/// 
/// The lifecycle calls are driven by `ImageTrackingAssigner`.
/// </summary>
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


    /// <summary>
    /// One-time setup.
    /// </summary>
    protected override void OnInitialized(TrackedContentContext context)
    {
        // Default to controlling the whole object.
        if (visibilityRoot == null)
            visibilityRoot = gameObject;

        if (boardRoot == null)
            boardRoot = visibilityRoot;

        ApplyVisualState(forceShow: false);
    }

    /// <inheritdoc/>
    public override void OnTrackingFound(TrackedContentContext context)
    {
        SetContext(context);
        AttachToTrackedImage(context);

        if (!isVisible && visibilityRoot != null)
            visibilityRoot.SetActive(true);

        isVisible = true;

        if (currentState == BackBoardState.Hidden)
            currentState = BackBoardState.Idle;

        ApplyVisualState(forceShow: true);
    }

    /// <inheritdoc/>
    public override void OnTrackingLost()
    {
        if (!isVisible)
            return;

        // Give derived classes a chance to stop audio, particles, timelines, etc.
        Pause();

        // Detach so it no longer depends on the tracked image hierarchy while hidden.
        // This keeps the last world pose if the tracked image transform disappears.
        DetachKeepWorldPose();

        isVisible = false;
        ApplyVisualState(forceShow: false);
    }

    /// <inheritdoc/>
    public override void OnTrackableRemoved(Action destroySelf)
    {
        // No hide animation here; destroy immediately.
        Pause();
        destroySelf?.Invoke();
    }

    public void SetIdle()
    {
        SetState(BackBoardState.Idle);
    }

    public void OpenGallery()
    {
        SetState(BackBoardState.GalleryOpen);
    }

    public void CloseGallery()
    {
        SetState(BackBoardState.Idle);
    }

    public void StartPlaying()
    {
        SetState(BackBoardState.Playing);
    }

    public void StopPlaying()
    {
        SetState(BackBoardState.Idle);
    }

    public void SetCoinsVisible(bool visible)
    {
        if (coinRoot != null)
            coinRoot.SetActive(visible);
    }

    /// <summary>
    /// Hook for derived behaviours to pause work while not tracked.
    /// 
    /// Intentionally empty in the base back handler.
    /// </summary>
    protected virtual void Pause()
    {
        // Future pause logic here.
    }

    /// <summary>
    /// Set the current state and apply visual changes.
    /// </summary>
    /// <param name="newState"></param>
    private void SetState(BackBoardState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        ApplyVisualState(forceShow: isVisible);
    }

    /// <summary>
    /// Toggle visibility of root objects based on the current state.
    /// </summary>
    /// <param name="forceShow"></param>
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

        bool showCoins = currentState == BackBoardState.Idle;
        bool showGallery = currentState == BackBoardState.GalleryOpen;
        bool showViewport = currentState == BackBoardState.Playing;

        if (boardRoot != null)
            boardRoot.SetActive(showBoard);

        if (coinRoot != null)
            coinRoot.SetActive(showCoins);

        if (galleryRoot != null)
            galleryRoot.SetActive(showGallery);

        if (gameViewportRoot != null)
            gameViewportRoot.SetActive(showViewport);
    }
}