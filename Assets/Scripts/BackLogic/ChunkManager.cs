using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    private sealed class ChunkRecord
    {
        public GameObject root;
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
    [SerializeField] private float playerStartSafeRadius = 2.5f;

    private readonly Dictionary<Vector2Int, ChunkRecord> activeChunks = new();
    private Vector2Int currentCenterChunk;
    private bool hasCenterChunk;

    private void Reset()
    {
        boardWorldController = GetComponentInParent<BoardWorldController>();
    }

    private void Start()
    {
        RefreshAroundPlayer(force: true);
    }

    private void Update()
    {
        RefreshAroundPlayer(force: false);
    }

    public void RebuildAll()
    {
        foreach (var pair in activeChunks)
        {
            if (pair.Value != null && pair.Value.root != null)
                Destroy(pair.Value.root);
        }

        activeChunks.Clear();
        hasCenterChunk = false;
        RefreshAroundPlayer(force: true);
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
                Vector2Int coord = new Vector2Int(currentCenterChunk.x + x, currentCenterChunk.y + y);
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
        GameObject chunkRoot = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkRoot.transform.SetParent(obstaclesRoot, false);

        float chunkWorldSize = chunkSizeInCells * cellSize;
        Vector2 chunkOrigin = new Vector2(chunkCoord.x * chunkWorldSize, chunkCoord.y * chunkWorldSize);

        for (int cy = 0; cy < chunkSizeInCells; cy++)
        {
            for (int cx = 0; cx < chunkSizeInCells; cx++)
            {
                Vector2 cellCenter = chunkOrigin + new Vector2(
                    (cx + 0.5f) * cellSize,
                    (cy + 0.5f) * cellSize);

                if (cellCenter.sqrMagnitude <= playerStartSafeRadius * playerStartSafeRadius)
                    continue;

                if (!ShouldPlaceObstacle(chunkCoord, cx, cy))
                    continue;

                WorldEntityView obstacle = Instantiate(obstacleViewPrefab, chunkRoot.transform);
                obstacle.name = $"Obstacle_{chunkCoord.x}_{chunkCoord.y}_{cx}_{cy}";
                obstacle.Bind(boardWorldController);
                obstacle.SetWorldPosition(cellCenter);
                obstacle.SetWorldY(0f);
                obstacle.RefreshNow();
            }
        }

        activeChunks.Add(chunkCoord, new ChunkRecord
        {
            root = chunkRoot
        });
    }

    private void RemoveChunk(Vector2Int chunkCoord)
    {
        if (!activeChunks.TryGetValue(chunkCoord, out ChunkRecord record))
            return;

        if (record != null && record.root != null)
            Destroy(record.root);

        activeChunks.Remove(chunkCoord);
    }

    private Vector2Int WorldToChunk(Vector2 worldPosition)
    {
        float chunkWorldSize = chunkSizeInCells * cellSize;

        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x / chunkWorldSize),
            Mathf.FloorToInt(worldPosition.y / chunkWorldSize));
    }

    private bool ShouldPlaceObstacle(Vector2Int chunkCoord, int cellX, int cellY)
    {
        uint h = 2166136261u;

        h = Mix(h, (uint)worldSeed);
        h = Mix(h, (uint)chunkCoord.x);
        h = Mix(h, (uint)chunkCoord.y);
        h = Mix(h, (uint)cellX);
        h = Mix(h, (uint)cellY);

        float normalized = (h % 10000u) / 10000f;
        return normalized < obstacleChance;
    }

    private static uint Mix(uint h, uint value)
    {
        h ^= value + 0x9e3779b9u + (h << 6) + (h >> 2);
        return h;
    }
}