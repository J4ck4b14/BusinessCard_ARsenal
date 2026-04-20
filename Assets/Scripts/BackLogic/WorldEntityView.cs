using UnityEngine;

/// <summary>
/// Called like this 'cause it controls the visual representation of an entity in the world, and it needs to be able to refresh itself when the world changes (e.g., when the player moves).
/// </summary>
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

    private void LateUpdate()
    {
        if (autoRefresh)
            RefreshNow();
    }

    public void Bind(BoardWorldController controller)
    {
        boardWorldController = controller;
    }

    public void SetWorldPosition(Vector2 newWorldPosition)
    {
        worldPosition = newWorldPosition;
    }

    public void SetWorldY(float newWorldY)
    {
        worldY = newWorldY;
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
}