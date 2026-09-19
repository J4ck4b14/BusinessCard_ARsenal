using UnityEngine;

// Converts stored world coordinates into the scrolling board-space view.
public class WorldEntityView : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private bool hideWhenOutsideViewport = true;

    [Header("World placement")]
    [SerializeField] private Vector2 worldPosition;
    [SerializeField] private float worldY = 0f;
    [SerializeField] private Vector2 visibilityPadding = Vector2.zero;

    private BoardWorldController boardWorldController;

    public Vector2 WorldPosition => worldPosition;

    private void Reset()
    {
        if (visualRoot == null)
            visualRoot = gameObject;
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void Bind(BoardWorldController controller)
    {
        if (boardWorldController == controller)
            return;

        Unsubscribe();
        boardWorldController = controller;

        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged += OnPlayerWorldPositionChanged;

        if (autoRefresh)
            RefreshNow();
    }

    public void SetWorldPosition(Vector2 newWorldPosition)
    {
        worldPosition = newWorldPosition;
        if (autoRefresh)
            RefreshNow();
    }

    public void SetWorldY(float newWorldY)
    {
        worldY = newWorldY;
        if (autoRefresh)
            RefreshNow();
    }

    public void RefreshNow()
    {
        if (boardWorldController == null)
            return;

        bool visible = boardWorldController.IsWorldPositionVisible(worldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || !hideWhenOutsideViewport || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(worldPosition, worldY);
    }

    private void OnPlayerWorldPositionChanged(Vector2 _)
    {
        if (autoRefresh)
            RefreshNow();
    }

    private void Unsubscribe()
    {
        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged -= OnPlayerWorldPositionChanged;
    }
}
