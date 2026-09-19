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

    [Header("Wave population")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private int baseEnemyCount = 3;
    [SerializeField] private int extraEnemiesPerWave = 1;
    [SerializeField] private int bonusEnemyEveryNWaves = 3;
    [SerializeField] private int maxEnemyCount = 14;

    [Header("Enemy role unlocks")]
    [SerializeField] private int gunnerUnlockWave = 2;
    [SerializeField] private int heavyUnlockWave = 4;
    [SerializeField, Range(0f, 1f)] private float baseGunnerChance = 0.20f;
    [SerializeField, Range(0f, 1f)] private float baseHeavyChance = 0.10f;

    [Header("Spawn")]
    [SerializeField] private float minSpawnRadius = 5f;
    [SerializeField] private float maxSpawnRadius = 8f;
    [SerializeField] private float enemySpawnClearance = 0.4f;
    [SerializeField] private int maxSpawnAttemptsPerEnemy = 12;

    private readonly List<EnemyTankController> liveEnemies = new();
    private bool clearing;
    private int activeWaveIndex;

    public int RemainingEnemies => liveEnemies.Count;

    public void StartWave(int waveIndex)
    {
        ClearWave();

        if (enemyPrefab == null || boardWorldController == null)
            return;

        activeWaveIndex = waveIndex;
        int enemyCount = CalculateEnemyCount(waveIndex);
        // Keeping the seed tied to the wave is what makes the layout repeatable.
        // I leave the 7919 alone unless I change how a wave is built, it just needs to spread nearby wave seeds apart.
        System.Random rng = new(seed + waveIndex * 7919);
        Vector2 playerWorldPosition = boardWorldController.PlayerWorldPosition;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnPosition = FindSpawnPosition(rng, playerWorldPosition);
            EnemyTankController.EnemyRole role = ChooseRole(rng, waveIndex, i);

            EnemyTankController enemy = enemiesRoot != null
                ? Instantiate(enemyPrefab, enemiesRoot)
                : Instantiate(enemyPrefab);

            enemy.name = $"Enemy_{role}_W{waveIndex}_{i}";
            enemy.Initialize(
                this,
                boardWorldController,
                shieldPlacementController,
                enemyProjectilesRoot,
                spawnPosition,
                waveIndex,
                role);

            liveEnemies.Add(enemy);
        }
    }

    public void NotifyEnemyKilled(EnemyTankController enemy)
    {
        if (enemy != null)
            boardGameController?.RegisterEnemyKill(enemy.ScoreValue);

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
        DestroyChildren(enemyProjectilesRoot);
        clearing = false;
    }

    private int CalculateEnemyCount(int waveIndex)
    {
        int waveOffset = Mathf.Max(0, waveIndex - 1);
        int bonus = bonusEnemyEveryNWaves > 0 ? waveOffset / bonusEnemyEveryNWaves : 0;
        int count = baseEnemyCount + waveOffset * extraEnemiesPerWave + bonus;
        return Mathf.Clamp(count, 1, Mathf.Max(1, maxEnemyCount));
    }

    private EnemyTankController.EnemyRole ChooseRole(System.Random rng, int waveIndex, int enemyIndex)
    {
        // Guarantee that the player sees each newly unlocked behaviour immediately.
        if (waveIndex == heavyUnlockWave && enemyIndex == 0)
            return EnemyTankController.EnemyRole.Heavy;

        if (waveIndex == gunnerUnlockWave && enemyIndex == 0)
            return EnemyTankController.EnemyRole.Gunner;

        double roll = rng.NextDouble();

        if (waveIndex >= heavyUnlockWave)
        {
            float heavyChance = Mathf.Min(0.28f, baseHeavyChance + (waveIndex - heavyUnlockWave) * 0.02f);
            if (roll < heavyChance)
                return EnemyTankController.EnemyRole.Heavy;

            roll = (roll - heavyChance) / Mathf.Max(0.0001f, 1f - heavyChance);
        }

        if (waveIndex >= gunnerUnlockWave)
        {
            float gunnerChance = Mathf.Min(0.42f, baseGunnerChance + (waveIndex - gunnerUnlockWave) * 0.025f);
            if (roll < gunnerChance)
                return EnemyTankController.EnemyRole.Gunner;
        }

        return EnemyTankController.EnemyRole.Assault;
    }

    private Vector2 FindSpawnPosition(System.Random rng, Vector2 playerWorldPosition)
    {
        Vector2 fallback = playerWorldPosition + Vector2.up * minSpawnRadius;

        for (int attempt = 0; attempt < Mathf.Max(1, maxSpawnAttemptsPerEnemy); attempt++)
        {
            float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
            float radius = Mathf.Lerp(minSpawnRadius, maxSpawnRadius, (float)rng.NextDouble());
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 candidate = playerWorldPosition + dir * radius;

            fallback = candidate;

            if (!boardWorldController.IsObstacleBlocked(candidate, enemySpawnClearance))
                return candidate;
        }

        return fallback;
    }

    private static void DestroyChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
