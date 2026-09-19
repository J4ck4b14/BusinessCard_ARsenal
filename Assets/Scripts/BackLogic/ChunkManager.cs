using System.Collections.Generic;
using UnityEngine;

// Obstacles are deterministic inside one wave, then reseeded for the next one.
public class ChunkManager : MonoBehaviour
{
    private sealed class ChunkRecord
    {
        public GameObject root;
        public readonly List<Vector2Int> occupiedCells = new();
    }

    [Header("References")]
    [SerializeField] private BoardWorldController boardWorldController;
    [SerializeField] private Transform obstaclesRoot;
    [SerializeField] private WorldEntityView obstacleViewPrefab;

    [Header("Chunk settings")]
    [SerializeField] private int worldSeed = 12345;
    [SerializeField] private int chunkSizeInCells = 8;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private int activeChunkRadius = 2;

    [Header("Generation")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleChance = 0.18f;
    [SerializeField] private float playerStartSafeRadius = 1.25f;
    [Range(0.1f, 0.5f)]
    [SerializeField] private float obstacleHalfExtentInCells = 0.42f;

    private readonly Dictionary<Vector2Int, ChunkRecord> activeChunks = new();
    private readonly HashSet<Vector2Int> occupiedCells = new();

    private Vector2Int currentCenterChunk;
    private bool hasCenterChunk;
    private int currentWaveSeed;

    private void Reset()
    {
        boardWorldController = GetComponentInParent<BoardWorldController>();
    }

    private void Awake()
    {
        currentWaveSeed = worldSeed;
    }

    private void OnEnable()
    {
        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged += OnPlayerWorldPositionChanged;
    }

    private void Start()
    {
        RefreshAroundPlayer(force: true);
    }

    private void OnDisable()
    {
        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged -= OnPlayerWorldPositionChanged;
    }

    public void RebuildForWave(int waveIndex)
    {
        unchecked
        {
            currentWaveSeed = worldSeed ^ (waveIndex * 73856093);
        }

        RebuildAll();
    }

    public void RebuildAll()
    {
        foreach (var pair in activeChunks)
        {
            if (pair.Value != null && pair.Value.root != null)
                Destroy(pair.Value.root);
        }

        activeChunks.Clear();
        occupiedCells.Clear();
        hasCenterChunk = false;
        RefreshAroundPlayer(force: true);
    }

    public bool IsBlocked(Vector2 worldPoint, float radius = 0f)
    {
        if (cellSize <= 0f || occupiedCells.Count == 0)
            return false;

        int centerX = Mathf.FloorToInt(worldPoint.x / cellSize);
        int centerY = Mathf.FloorToInt(worldPoint.y / cellSize);

        int searchRadius = Mathf.Max(1, Mathf.CeilToInt((radius + cellSize * obstacleHalfExtentInCells) / cellSize));
        float halfExtent = cellSize * obstacleHalfExtentInCells;

        for (int y = -searchRadius; y <= searchRadius; y++)
        {
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                Vector2Int cell = new(centerX + x, centerY + y);
                if (!occupiedCells.Contains(cell))
                    continue;

                Vector2 obstacleCenter = CellToWorldCenter(cell);
                Vector2 delta = worldPoint - obstacleCenter;

                float nearestX = Mathf.Clamp(delta.x, -halfExtent, halfExtent);
                float nearestY = Mathf.Clamp(delta.y, -halfExtent, halfExtent);
                Vector2 nearest = obstacleCenter + new Vector2(nearestX, nearestY);

                if ((worldPoint - nearest).sqrMagnitude <= radius * radius + 0.000001f)
                    return true;

                if (radius <= 0f &&
                    Mathf.Abs(delta.x) <= halfExtent &&
                    Mathf.Abs(delta.y) <= halfExtent)
                    return true;
            }
        }

        return false;
    }

    private void OnPlayerWorldPositionChanged(Vector2 _)
    {
        RefreshAroundPlayer(force: false);
    }

    private void RefreshAroundPlayer(bool force)
    {
        if (boardWorldController == null || obstacleViewPrefab == null)
            return;

        Vector2Int newCenterChunk = WorldToChunk(boardWorldController.PlayerWorldPosition);

        if (!force && hasCenterChunk && newCenterChunk == currentCenterChunk)
            return;

        currentCenterChunk = newCenterChunk;
        hasCenterChunk = true;

        HashSet<Vector2Int> required = new();

        for (int y = -activeChunkRadius; y <= activeChunkRadius; y++)
        {
            for (int x = -activeChunkRadius; x <= activeChunkRadius; x++)
            {
                Vector2Int coord = new(currentCenterChunk.x + x, currentCenterChunk.y + y);
                required.Add(coord);

                if (!activeChunks.ContainsKey(coord))
                    CreateChunk(coord);
            }
        }

        List<Vector2Int> toRemove = new();

        foreach (var pair in activeChunks)
        {
            if (!required.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (Vector2Int coord in toRemove)
            RemoveChunk(coord);
    }

    private void CreateChunk(Vector2Int chunkCoord)
    {
        GameObject chunkRoot = new($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkRoot.transform.SetParent(obstaclesRoot, false);

        ChunkRecord record = new() { root = chunkRoot };

        for (int cy = 0; cy < chunkSizeInCells; cy++)
        {
            for (int cx = 0; cx < chunkSizeInCells; cx++)
            {
                Vector2Int globalCell = new(
                    chunkCoord.x * chunkSizeInCells + cx,
                    chunkCoord.y * chunkSizeInCells + cy);

                Vector2 cellCenter = CellToWorldCenter(globalCell);

                // Keep the player's current position playable when a new wave rebuilds the field.
                Vector2 playerPosition = boardWorldController.PlayerWorldPosition;
                if ((cellCenter - playerPosition).sqrMagnitude <= playerStartSafeRadius * playerStartSafeRadius)
                    continue;

                if (!ShouldPlaceObstacle(globalCell))
                    continue;

                occupiedCells.Add(globalCell);
                record.occupiedCells.Add(globalCell);

                WorldEntityView obstacle = Instantiate(obstacleViewPrefab, chunkRoot.transform);
                obstacle.name = $"Obstacle_{globalCell.x}_{globalCell.y}";
                obstacle.Bind(boardWorldController);
                obstacle.SetWorldPosition(cellCenter);
                obstacle.SetWorldY(0f);
                obstacle.RefreshNow();
            }
        }

        activeChunks.Add(chunkCoord, record);
    }

    private void RemoveChunk(Vector2Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out ChunkRecord record))
            return;

        if (record != null)
        {
            foreach (Vector2Int cell in record.occupiedCells)
                occupiedCells.Remove(cell);

            if (record.root != null)
                Destroy(record.root);
        }

        activeChunks.Remove(chunkCoord);
    }

    private Vector2Int WorldToChunk(Vector2 worldPosition)
    {
        float chunkWorldSize = chunkSizeInCells * cellSize;

        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkWorldSize),
            Mathf.FloorToInt(worldPosition.y / chunkWorldSize));
    }

    private Vector2 CellToWorldCenter(Vector2Int cell)
    {
        return new Vector2(
            (cell.x + 0.5f) * cellSize,
            (cell.y + 0.5f) * cellSize);
    }

    private bool ShouldPlaceObstacle(Vector2Int globalCell)
    {
        uint h = 2166136261u;

        h = Mix(h, (uint)currentWaveSeed);
        h = Mix(h, (uint)globalCell.x);
        h = Mix(h, (uint)globalCell.y);

        float normalized = (h % 10000u) / 10000f;
        return normalized < obstacleChance;
    }

    private static uint Mix(uint h, uint value)
    {
        h ^= value + 0x9e3779b9u + (h << 6) + (h >> 2);
        return h;
    }
}
