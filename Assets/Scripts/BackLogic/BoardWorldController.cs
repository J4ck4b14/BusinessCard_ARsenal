using System;
using UnityEngine;

public class BoardWorldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerVisualRoot;
    [SerializeField] private ChunkManager chunkManager;

    [Header("Board viewport")]
    [SerializeField] private float boardHalfWidth = 4f;
    [SerializeField] private float boardHalfHeight = 2.5f;

    [Header("Collision")]
    [SerializeField] private float playerCollisionRadius = 0.28f;
    [SerializeField] private float lineOfSightSampleSpacing = 0.2f;

    private Vector2 playerWorldPosition;
    private bool simulationActive;

    public event Action<Vector2> PlayerWorldPositionChanged;

    public Vector2 PlayerWorldPosition => playerWorldPosition;
    public float BoardHalfWidth => boardHalfWidth;
    public float BoardHalfHeight => boardHalfHeight;
    public bool SimulationActive => simulationActive;

    [SerializeField] private bool debugShowOffBoardEntities = false;
    public bool DebugShowOffBoardEntities => debugShowOffBoardEntities;

    private void Awake()
    {
        if (chunkManager == null)
        {
            BackTrackedContentHandler root = GetComponentInParent<BackTrackedContentHandler>(true);
            if (root != null)
                chunkManager = root.GetComponentInChildren<ChunkManager>(true);
        }
    }

    public void BeginRun()
    {
        SetPlayerWorldPosition(Vector2.zero);

        if (playerVisualRoot != null)
            playerVisualRoot.localPosition = Vector3.zero;
    }

    public void SetSimulationActive(bool active)
    {
        simulationActive = active;
    }

    public void SetPlayerWorldPosition(Vector2 newWorldPosition)
    {
        if (playerWorldPosition == newWorldPosition)
            return;

        playerWorldPosition = newWorldPosition;
        PlayerWorldPositionChanged?.Invoke(playerWorldPosition);
    }

    public bool TryMovePlayerWorld(Vector2 delta)
    {
        Vector2 candidate = playerWorldPosition + delta;

        if (IsObstacleBlocked(candidate, playerCollisionRadius))
            return false;

        SetPlayerWorldPosition(candidate);
        return true;
    }

    public void MovePlayerWorld(Vector2 delta)
    {
        TryMovePlayerWorld(delta);
    }

    public bool IsObstacleBlocked(Vector2 worldPosition, float radius = 0f)
    {
        return chunkManager != null && chunkManager.IsBlocked(worldPosition, radius);
    }

    // Sample the path against the same obstacle data used for movement.
    public bool HasClearLine(Vector2 from, Vector2 to, float radius = 0.04f)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.001f)
            return true;

        float spacing = Mathf.Max(0.05f, lineOfSightSampleSpacing);
        int samples = Mathf.Max(1, Mathf.CeilToInt(distance / spacing));

        for (int i = 1; i < samples; i++)
        {
            Vector2 point = Vector2.Lerp(from, to, i / (float)samples);
            if (IsObstacleBlocked(point, radius))
                return false;
        }

        return true;
    }

    public Vector3 WorldToBoardLocal(Vector2 worldPosition, float worldY = 0f)
    {
        Vector2 relative = worldPosition - playerWorldPosition;
        return new Vector3(relative.x, worldY, relative.y);
    }

    public Vector2 BoardLocalToWorld(Vector3 boardLocalPosition)
    {
        return playerWorldPosition + new Vector2(boardLocalPosition.x, boardLocalPosition.z);
    }

    public bool IsWorldPositionVisible(Vector2 worldPosition, Vector2 padding)
    {
        Vector2 relative = worldPosition - playerWorldPosition;

        return Mathf.Abs(relative.x) <= boardHalfWidth + padding.x &&
               Mathf.Abs(relative.y) <= boardHalfHeight + padding.y;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(boardHalfWidth * 2f, 0.01f, boardHalfHeight * 2f);
        Gizmos.DrawWireCube(center, size);
    }
}
