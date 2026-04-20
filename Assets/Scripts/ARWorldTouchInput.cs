using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ARWorldTouchInput : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableMask = ~0;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Update()
    {
        HandleTouch();
        HandleMouse();
    }

    private void HandleTouch()
    {
        if (Touchscreen.current == null)
            return;

        var touch = Touchscreen.current.primaryTouch;

        if (!touch.press.wasPressedThisFrame)
            return;

        Vector2 screenPos = touch.position.ReadValue();

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TryPressAt(screenPos);
    }

    private void HandleMouse()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        TryPressAt(screenPos);
#endif
    }

    private void TryPressAt(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
        {
            ArsenalInteractable interactable = hit.collider.GetComponentInParent<ArsenalInteractable>();
            if (interactable != null)
            {
                interactable.Press();
                return;
            }

            BoardSurfaceClickReceiver boardSurface = hit.collider.GetComponentInParent<BoardSurfaceClickReceiver>();
            if (boardSurface != null)
            {
                boardSurface.ReceiveBoardHit(hit.point);
                return;
            }
        }
    }
}