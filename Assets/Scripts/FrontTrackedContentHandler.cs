using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

/// <summary>
/// Front-side tracked content behaviour.
/// 
/// Behaviour summary:
/// - When tracking is found: attach to the tracked image and animate a UI `Image.fillAmount` from 0 ? 1.
/// - When tracking is lost: detach (keep world pose) and animate fill from 1 ? 0, then disable.
/// - When trackable is removed: if currently visible, play the hide animation and destroy afterwards.
/// 
/// The actual spawning and lifecycle calls are managed by `ImageTrackingAssigner`.
/// </summary>
public sealed class FrontTrackedContentHandler : TrackedContentHandlerBase
{
    [Header("Optional references")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image fillImage;

    // Root transform that should face the AR camera (e.g., a billboard pivot).
    [SerializeField] private Transform facingRoot;

    private RotationConstraint rotationConstraint;
    private Coroutine animationRoutine;

    /// <summary>
    /// Minimal state machine preventing overlapping coroutines and supporting re-entrant events
    /// (tracking toggling quickly).
    /// </summary>
    private enum FrontState
    {
   Hidden,
        Showing,
  Visible,
    Hiding
    }

    private FrontState state = FrontState.Hidden;

    /// <summary>
    /// One-time component discovery and setup.
    /// </summary>
    protected override void OnInitialized(TrackedContentContext context)
    {
   // Resolve optional references for prefab variants.
   if (worldCanvas == null)
  worldCanvas = GetComponentInChildren<Canvas>(true);

    if (fillImage == null)
       fillImage = GetComponentInChildren<Image>(true);

        // If no explicit facing root is provided, face the camera with the whole object.
  if (facingRoot == null)
        facingRoot = transform;

   SetupCanvasAndConstraint(context);

        // Ensure we start hidden for fill-based variants.
        if (fillImage != null)
  fillImage.fillAmount = 0f;
    }

    /// <summary>
/// Refresh camera references / constraints if the context changes.
    /// </summary>
    protected override void OnContextRefreshed(TrackedContentContext context)
    {
    SetupCanvasAndConstraint(context);
    }

    /// <inheritdoc/>
public override void OnTrackingFound(TrackedContentContext context)
    {
        // Update ActiveContext, then attach under the tracked image transform.
    SetContext(context);
    AttachToTrackedImage(context);

  // Ensure visible while tracking.
        gameObject.SetActive(true);

    // If no fill image exists we treat this handler as an instant show/hide.
  if (fillImage == null)
        {
      state = FrontState.Visible;
       return;
  }

        // Ignore duplicate "tracking found" events during show/visible.
  if (state == FrontState.Visible || state == FrontState.Showing)
      return;

    StopAnimation();
  state = FrontState.Showing;
   animationRoutine = StartCoroutine(ShowRoutine());
}

    /// <inheritdoc/>
    public override void OnTrackingLost()
{
  // No fill image => just disable.
        if (fillImage == null)
  {
 gameObject.SetActive(false);
   state = FrontState.Hidden;
        return;
   }

    // Ignore duplicate "tracking lost" events during hide/hidden.
    if (state == FrontState.Hidden || state == FrontState.Hiding)
  return;

   StopAnimation();

    // Detach so the hide animation keeps the world-space pose even while the tracked image moves/disappears.
        DetachKeepWorldPose();

    // Keep it active while animating the fill down.
    gameObject.SetActive(true);

        state = FrontState.Hiding;
    animationRoutine = StartCoroutine(HideRoutine(destroyWhenDone: false, destroySelf: null));
    }

    /// <inheritdoc/>
    public override void OnTrackableRemoved(Action destroySelf)
    {
        // If we are already hidden (or have nothing to animate), destroy immediately.
   if (fillImage == null || state == FrontState.Hidden || fillImage.fillAmount <= 0f)
        {
       destroySelf?.Invoke();
  return;
    }

    StopAnimation();

        // Same approach as tracking lost: freeze pose and hide, but destroy at the end.
        DetachKeepWorldPose();
    gameObject.SetActive(true);

        state = FrontState.Hiding;
    animationRoutine = StartCoroutine(HideRoutine(destroyWhenDone: true, destroySelf: destroySelf));
    }

    /// <summary>
    /// Configures:
    /// - world-space canvas camera (`Canvas.worldCamera`)
    /// - a `RotationConstraint` on <see cref="facingRoot"/> so it faces the AR camera around Y.
    /// </summary>
    private void SetupCanvasAndConstraint(TrackedContentContext context)
    {
  if (context == null)
  return;

   if (worldCanvas != null)
        {
       worldCanvas.renderMode = RenderMode.WorldSpace;
       worldCanvas.worldCamera = context.ArCamera;
    }

    if (facingRoot == null || context.ArCamera == null)
   return;

        // Ensure we have a constraint component.
    rotationConstraint = facingRoot.GetComponent<RotationConstraint>();
    if (rotationConstraint == null)
        rotationConstraint = facingRoot.gameObject.AddComponent<RotationConstraint>();

    // Only rotate around Y to keep the content upright.
   rotationConstraint.rotationAxis = Axis.Y;

  // Keep exactly one source: the AR camera.
   for (int i = rotationConstraint.sourceCount - 1; i >= 0; i--)
   rotationConstraint.RemoveSource(i);

   ConstraintSource source = new ConstraintSource
    {
        sourceTransform = context.ArCamera.transform,
  weight = 1f
   };

   rotationConstraint.AddSource(source);
        rotationConstraint.constraintActive = true;
    }

    /// <summary>
    /// Fills the UI image until fully visible.
/// Speed is derived from `ImageTrackingAssigner.fillSpeed` via `TrackedContentContext.FillUnitsPerSecond`.
    /// </summary>
    private IEnumerator ShowRoutine()
    {
    while (fillImage != null && fillImage.fillAmount < 1f)
   {
   fillImage.fillAmount = Mathf.Min(
     1f,
      fillImage.fillAmount + ActiveContext.FillUnitsPerSecond * Time.deltaTime);

        yield return null;
    }

    animationRoutine = null;
    state = FrontState.Visible;
    }

    /// <summary>
    /// Un-fills the UI image until hidden, then disables or destroys.
    /// </summary>
    private IEnumerator HideRoutine(bool destroyWhenDone, Action destroySelf)
    {
   while (fillImage != null && fillImage.fillAmount > 0f)
  {
 fillImage.fillAmount = Mathf.Max(
      0f,
       fillImage.fillAmount - ActiveContext.HideFillUnitsPerSecond * Time.deltaTime);

 yield return null;
        }

   animationRoutine = null;

        if (destroyWhenDone)
        {
      // `ImageTrackingAssigner` expects this callback to be invoked to finalize removal.
   destroySelf?.Invoke();
 yield break;
    }

    gameObject.SetActive(false);
        state = FrontState.Hidden;
    }

    /// <summary>
    /// Stops the currently running show/hide coroutine (if any).
    /// </summary>
    private void StopAnimation()
    {
   if (animationRoutine != null)
    {
  StopCoroutine(animationRoutine);
  animationRoutine = null;
   }
    }
}