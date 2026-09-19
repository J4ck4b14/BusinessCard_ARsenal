using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// One pointer path for mouse and touch. UI gets first refusal, then we raycast the AR world.
public sealed class ARWorldTouchInput : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableMask = ~0;

    private InputAction pressAction;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        pressAction = new InputAction(
            name: "WorldPress",
            type: InputActionType.Button,
            binding: "<Pointer>/press");

        pressAction.started += OnPressStarted;
    }

    private void OnEnable()
    {
        pressAction?.Enable();
    }

    private void OnDisable()
    {
        pressAction?.Disable();
    }

    private void OnDestroy()
    {
        if (pressAction == null)
            return;

        pressAction.started -= OnPressStarted;
        pressAction.Dispose();
    }

    private void OnPressStarted(InputAction.CallbackContext context)
    {
        if (arCamera == null || Pointer.current == null)
            return;

        // World presses should never leak through a UI button.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        TryPressAt(Pointer.current.position.ReadValue());
    }

    private void TryPressAt(Vector2 screenPosition)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, interactableMask))
            return;

        ArsenalInteractable interactable = hit.collider.GetComponentInParent<ArsenalInteractable>();
        if (interactable != null)
        {
            interactable.Press();
            return;
        }

        BoardSurfaceClickReceiver boardSurface = hit.collider.GetComponentInParent<BoardSurfaceClickReceiver>();
        if (boardSurface != null)
            boardSurface.ReceiveBoardHit(hit.point);
    }
}
