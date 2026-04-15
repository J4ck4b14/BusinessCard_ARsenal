using UnityEngine;
using UnityEngine.EventSystems;

public class ARWorldTouchInput : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, interactableLayerMask))
        {
            ArsenalInteractable interactable = hit.collider.GetComponentInParent<ArsenalInteractable>();

            if (interactable != null)
                interactable.Press();
        }

#if UNITY_EDITOR
        // For testing in the editor with mouse input
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Ray mouseRay = arCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit mouseHit, 100f, interactableLayerMask))
            {
                ArsenalInteractable interactable = mouseHit.collider.GetComponentInParent<ArsenalInteractable>();
                if (interactable != null)
                    interactable.Press();
            }
        }
#endif
    }
}
