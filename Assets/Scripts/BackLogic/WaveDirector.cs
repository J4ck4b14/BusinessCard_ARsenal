using System;
using System.Collections.Generic;
using UnityEngine;

public class WaveDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardGameController boardGameController;
    [SerializeField] private BoardWorldController boardWorldController;
    [SerializeField] private ShieldPlacementController shieldPlacementController;
    [SerializeField] private EnemyTankController enemyPrefab;
    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private Transform enemyProjectilesRoot;

    [Header("Spawn")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private int baseEnemyCount = 3;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private float minSpawnRadius = 5f;
    [SerializeField] private float maxSpawnRadius = 8f;

    private readonly List<EnemyTankController> liveEnemies = new();
    private bool clearing;

    public void StartWave(int waveIndex)
    {
        ClearWave();

        if (enemyPrefab == null || boardWorldController == null)
            return;

        int enemyCount = baseEnemyCount + Mathf.Max(0, waveIndex - 1) * extraEnemiesPerWave;
        System.Random rng = new(seed + waveIndex * 7919);

        Vector2 playerWorldPosition = boardWorldController.PlayerWorldPosition;

        for (int i = 0; i < enemyCount; i++)
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float radius = Mathf.Lerp(minSpawnRadius, maxSpawnRadius, (float)rng.NextDouble());

            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 spawnPosition = playerWorldPosition + dir * radius;

            if (Vector2.Distance(spawnPosition, playerWorldPosition) < minSpawnRadius)
                spawnPosition = playerWorldPosition + dir * minSpawnRadius;

            EnemyTankController enemy = enemiesRoot != null
                ? Instantiate(enemyPrefab, enemiesRoot)
                : Instantiate(enemyPrefab);

            enemy.name = $"Enemy_W{waveIndex}_{i}";
            enemy.Initialize(
                this,
                boardWorldController,
                shieldPlacementController,
                enemyProjectilesRoot,
                spawnPosition,
                waveIndex);

            liveEnemies.Add(enemy);
        }
    }

    public void NotifyEnemyKilled(EnemyTankController enemy)
    {
        liveEnemies.Remove(enemy);

        if (!clearing && liveEnemies.Count == 0 && boardGameController != null)
            boardGameController.CompleteCurrentWave();
    }

    public void ClearWave()
    {
        clearing = true;

        foreach (EnemyTankController enemy in liveEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        liveEnemies.Clear();
        clearing = false;
    }
}