using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public sealed class FrontTrackedContentHandler : TrackedContentHandlerBase
{
    [Header("References")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private Image fillImage;
    [SerializeField] private Transform facingRoot;

    private RotationConstraint rotationConstraint;
    private CanvasGroup canvasGroup;
    private Coroutine animationRoutine;

    private enum FrontState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }

    private FrontState state = FrontState.Hidden;

    protected override void OnInitialized(TrackedContentContext context)
    {
        if (worldCanvas == null)
            worldCanvas = GetComponentInChildren<Canvas>(true);

        if (fillImage == null)
            fillImage = GetComponentInChildren<Image>(true);

        if (facingRoot == null)
            facingRoot = transform;

        if (worldCanvas != null)
        {
            canvasGroup = worldCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = worldCanvas.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (fillImage != null)
            fillImage.fillAmount = 0f;

        SetupCanvasAndConstraint(context);
    }

    protected override void OnContextRefreshed(TrackedContentContext context)
    {
        SetupCanvasAndConstraint(context);
    }

    public override void OnTrackingFound(TrackedContentContext context)
    {
        SetContext(context);
        AttachToTrackedImage(context);
        gameObject.SetActive(true);

        if (state == FrontState.Visible || state == FrontState.Showing)
            return;

        StopAnimation();
        state = FrontState.Showing;
        animationRoutine = StartCoroutine(ShowRoutine());
    }

    public override void OnTrackingLost()
    {
        if (state == FrontState.Hidden || state == FrontState.Hiding)
            return;

        StopAnimation();

        // Keep the last pose while the interface closes. It feels much less jumpy than
        // following a marker whose pose is already becoming unreliable.
        DetachKeepWorldPose();
        gameObject.SetActive(true);

        state = FrontState.Hiding;
        animationRoutine = StartCoroutine(HideRoutine(false, null));
    }

    public override void OnTrackableRemoved(Action destroySelf)
    {
        if (state == FrontState.Hidden || GetVisibleAmount() <= 0f)
        {
            destroySelf?.Invoke();
            return;
        }

        StopAnimation();
        DetachKeepWorldPose();
        gameObject.SetActive(true);

        state = FrontState.Hiding;
        animationRoutine = StartCoroutine(HideRoutine(true, destroySelf));
    }

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

        rotationConstraint = facingRoot.GetComponent<RotationConstraint>();
        if (rotationConstraint == null)
            rotationConstraint = facingRoot.gameObject.AddComponent<RotationConstraint>();

        rotationConstraint.rotationAxis = Axis.Y;

        for (int i = rotationConstraint.sourceCount - 1; i >= 0; i--)
            rotationConstraint.RemoveSource(i);

        rotationConstraint.AddSource(new ConstraintSource
        {
            sourceTransform = context.ArCamera.transform,
            weight = 1f
        });

        rotationConstraint.constraintActive = true;
    }

    private IEnumerator ShowRoutine()
    {
        float amount = GetVisibleAmount();
        float speed = ActiveContext != null ? ActiveContext.FillUnitsPerSecond : 4f;

        while (amount < 1f)
        {
            amount = Mathf.Min(1f, amount + speed * Time.deltaTime);
            ApplyVisibleAmount(amount);
            yield return null;
        }

        SetInteractionEnabled(true);
        animationRoutine = null;
        state = FrontState.Visible;
    }

    private IEnumerator HideRoutine(bool destroyWhenDone, Action destroySelf)
    {
        SetInteractionEnabled(false);

        float amount = GetVisibleAmount();
        float speed = ActiveContext != null ? ActiveContext.HideFillUnitsPerSecond : 6f;

        while (amount > 0f)
        {
            amount = Mathf.Max(0f, amount - speed * Time.deltaTime);
            ApplyVisibleAmount(amount);
            yield return null;
        }

        animationRoutine = null;
        state = FrontState.Hidden;

        if (destroyWhenDone)
        {
            destroySelf?.Invoke();
            yield break;
        }

        gameObject.SetActive(false);
    }

    private float GetVisibleAmount()
    {
        if (canvasGroup != null)
            return canvasGroup.alpha;

        return fillImage != null ? fillImage.fillAmount : 0f;
    }

    private void ApplyVisibleAmount(float amount)
    {
        if (fillImage != null)
            fillImage.fillAmount = amount;

        if (canvasGroup != null)
            canvasGroup.alpha = amount;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private void StopAnimation()
    {
        if (animationRoutine == null)
            return;

        StopCoroutine(animationRoutine);
        animationRoutine = null;
    }
}
