using System.Collections.Generic;
using UnityEngine;

public class EnemyTankController : MonoBehaviour
{
    public static IReadOnlyCollection<EnemyTankController> ActiveEnemies => activeEnemies;
    private static readonly HashSet<EnemyTankController> activeEnemies = new();

    [Header("References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform turretRoot;
    [SerializeField] private Transform enemyProjectilesRoot;
    [SerializeField] private ProjectileController projectilePrefab;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 1.35f;
    [SerializeField] private float maxHealth = 2f;
    [SerializeField] private float contactDamage = 1f;
    [SerializeField] private float contactInterval = 1f;
    [SerializeField] private float fireRange = 4.5f;
    [SerializeField] private float shotsPerSecond = 0.5f;
    [SerializeField] private float projectileSpeed = 4.5f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private Vector2 visibilityPadding = new(0.6f, 0.6f);

    private WaveDirector waveDirector;
    private BoardWorldController boardWorldController;
    private ShieldPlacementController shieldPlacementController;

    private Vector2 worldPosition;
    private float currentHealth;
    private float contactCooldown;
    private float fireCooldown;
    private bool initialized;

    public Vector2 WorldPosition => worldPosition;

    public void Initialize(
        WaveDirector director,
        BoardWorldController worldController,
        ShieldPlacementController shieldController,
        Transform projectilesRoot,
        Vector2 startWorldPosition,
        int waveIndex)
    {
        waveDirector = director;
        boardWorldController = worldController;
        shieldPlacementController = shieldController;
        enemyProjectilesRoot = projectilesRoot;
        worldPosition = startWorldPosition;

        currentHealth = maxHealth + Mathf.Floor(waveIndex / 3f);
        moveSpeed += (waveIndex - 1) * 0.05f;

        if (visualRoot == null)
            visualRoot = gameObject;

        activeEnemies.Add(this);
        initialized = true;
    }

    private void OnDestroy()
    {
        activeEnemies.Remove(this);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    private void Update()
    {
        if (!initialized || boardWorldController == null || !boardWorldController.SimulationActive)
            return;

        if (PlayerTankController.Instance == null)
            return;

        contactCooldown -= Time.deltaTime;
        fireCooldown -= Time.deltaTime;

        Vector2 playerWorldPosition = PlayerTankController.Instance.WorldPosition;
        Vector2 toPlayer = playerWorldPosition - worldPosition;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer / distanceToPlayer : Vector2.zero;

        if (distanceToPlayer > 0.55f)
        {
            Vector2 candidate = worldPosition + direction * moveSpeed * Time.deltaTime;

            if (shieldPlacementController == null || !shieldPlacementController.IsPointBlockedForEnemy(candidate))
                worldPosition = candidate;
        }
        else if (contactCooldown <= 0f)
        {
            Debug.Log($"Enemy contact hit player. Enemy world pos: {worldPosition}");
            PlayerTankController.Instance.TakeDamage(contactDamage);
            contactCooldown = contactInterval;
        }

        if (projectilePrefab != null && distanceToPlayer <= fireRange && fireCooldown <= 0f)
        {
            Debug.Log($"Enemy firing at distance {distanceToPlayer}");
            fireCooldown = 1f / Mathf.Max(0.01f, shotsPerSecond);

            ProjectileController projectile = enemyProjectilesRoot != null
                ? Instantiate(projectilePrefab, enemyProjectilesRoot)
                : Instantiate(projectilePrefab);

            projectile.Initialize(
                boardWorldController,
                worldPosition + direction * 0.5f,
                direction,
                projectileSpeed,
                projectileDamage,
                projectileLifetime,
                ProjectileController.ProjectileOwner.Enemy);
        }

        RefreshVisual(direction);
    }

    private void RefreshVisual(Vector2 facingDirection)
    {
        bool visible = boardWorldController.IsWorldPositionVisible(worldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(worldPosition, 0f);

        if (visualRoot != null && facingDirection.sqrMagnitude > 0.0001f)
        {
            float yaw = Mathf.Atan2(facingDirection.x, facingDirection.y) * Mathf.Rad2Deg;
            visualRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        if (turretRoot != null && facingDirection.sqrMagnitude > 0.0001f)
        {
            float yaw = Mathf.Atan2(facingDirection.x, facingDirection.y) * Mathf.Rad2Deg;
            turretRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    private void Die()
    {
        waveDirector?.NotifyEnemyKilled(this);
        Destroy(gameObject);
    }
}