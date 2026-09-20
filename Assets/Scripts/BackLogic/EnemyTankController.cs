using System.Collections.Generic;
using UnityEngine;

public class EnemyTankController : MonoBehaviour
{
    public enum EnemyRole
    {
        Assault,
        Gunner,
        Heavy
    }

    public static IReadOnlyCollection<EnemyTankController> ActiveEnemies => activeEnemies;
    private static readonly HashSet<EnemyTankController> activeEnemies = new();

    [Header("References")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Transform turretRoot;
    [SerializeField] private Transform enemyProjectilesRoot;
    [SerializeField] private ProjectileController projectilePrefab;

    [Header("Base stats")]
    [SerializeField] private float moveSpeed = 1.35f;
    [SerializeField] private float maxHealth = 2f;
    [SerializeField] private float bodyRadius = 0.3f;
    [SerializeField] private float contactDamage = 1f;
    [SerializeField] private float contactInterval = 1f;
    [SerializeField] private float fireRange = 4.5f;
    [SerializeField] private float shotsPerSecond = 0.5f;
    [SerializeField] private float projectileSpeed = 4.5f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private Vector2 visibilityPadding = new(0.6f, 0.6f);

    [Header("Gunner spacing")]
    [SerializeField] private float gunnerMinDistance = 2.4f;
    [SerializeField] private float gunnerPreferredDistance = 3.8f;

    private WaveDirector waveDirector;
    private BoardWorldController boardWorldController;
    private ShieldPlacementController shieldPlacementController;

    private Vector2 worldPosition;
    private float currentHealth;
    private float contactCooldown;
    private float fireCooldown;
    private bool initialized;
    private int strafeSign = 1;
    private EnemyRole role;
    private int scoreValue;

    public Vector2 WorldPosition => worldPosition;
    public EnemyRole Role => role;
    public int ScoreValue => scoreValue;
    public float CollisionRadius => bodyRadius;

    public void Initialize(
        WaveDirector director,
        BoardWorldController worldController,
        ShieldPlacementController shieldController,
        Transform projectilesRoot,
        Vector2 startWorldPosition,
        int waveIndex,
        EnemyRole enemyRole)
    {
        waveDirector = director;
        boardWorldController = worldController;
        shieldPlacementController = shieldController;
        enemyProjectilesRoot = projectilesRoot;
        worldPosition = startWorldPosition;
        role = enemyRole;
        strafeSign = startWorldPosition.x + startWorldPosition.y >= 0f ? 1 : -1;

        ApplyRoleAndWaveScaling(waveIndex);
        currentHealth = maxHealth;

        if (visualRoot == null)
            visualRoot = gameObject;

        activeEnemies.Add(this);
        initialized = true;
        RefreshVisual(Vector2.down);
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

    private void FixedUpdate()
    {
        if (!initialized || boardWorldController == null || !boardWorldController.SimulationActive)
            return;

        if (PlayerTankController.Instance == null)
            return;

        float deltaTime = Time.fixedDeltaTime;
        contactCooldown = Mathf.Max(0f, contactCooldown - deltaTime);
        fireCooldown = Mathf.Max(0f, fireCooldown - deltaTime);

        Vector2 playerWorldPosition = PlayerTankController.Instance.WorldPosition;
        Vector2 toPlayer = playerWorldPosition - worldPosition;
        float distanceToPlayer = toPlayer.magnitude;
        Vector2 direction = distanceToPlayer > 0.001f ? toPlayer / distanceToPlayer : Vector2.zero;

        HandleMovement(direction, distanceToPlayer, deltaTime);

        if (distanceToPlayer <= 0.55f && contactCooldown <= 0f)
        {
            PlayerTankController.Instance.TakeDamage(contactDamage);
            contactCooldown = contactInterval;
        }

        bool clearShot = boardWorldController.HasClearLine(worldPosition, playerWorldPosition);
        if (projectilePrefab != null &&
            clearShot &&
            distanceToPlayer <= fireRange &&
            fireCooldown <= 0f)
        {
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

    private void HandleMovement(Vector2 toPlayerDirection, float distanceToPlayer, float deltaTime)
    {
        if (role != EnemyRole.Gunner)
        {
            if (distanceToPlayer > 0.55f)
                TryAdvance(toPlayerDirection, deltaTime);
            return;
        }

        if (distanceToPlayer > gunnerPreferredDistance)
        {
            TryAdvance(toPlayerDirection, deltaTime);
            return;
        }

        if (distanceToPlayer < gunnerMinDistance)
        {
            TryAdvance(-toPlayerDirection, deltaTime);
            return;
        }

        // Gunners strafe while maintaining firing distance instead of dog-piling the player.
        Vector2 tangent = strafeSign > 0
            ? new Vector2(-toPlayerDirection.y, toPlayerDirection.x)
            : new Vector2(toPlayerDirection.y, -toPlayerDirection.x);

        if (!TryAdvance(tangent, deltaTime * 0.65f))
            strafeSign *= -1;
    }

    private bool TryAdvance(Vector2 direction, float deltaTime)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();
        float step = moveSpeed * deltaTime;
        Vector2 forwardCandidate = worldPosition + direction * step;

        if (CanOccupy(forwardCandidate))
        {
            worldPosition = forwardCandidate;
            return true;
        }

        Vector2 tangentLeft = new(-direction.y, direction.x);
        Vector2 tangentRight = -tangentLeft;

        Vector2 leftCandidate = worldPosition + tangentLeft * step;
        if (CanOccupy(leftCandidate))
        {
            worldPosition = leftCandidate;
            return true;
        }

        Vector2 rightCandidate = worldPosition + tangentRight * step;
        if (CanOccupy(rightCandidate))
        {
            worldPosition = rightCandidate;
            return true;
        }

        return false;
    }

    private bool CanOccupy(Vector2 candidate)
    {
        if (shieldPlacementController != null && shieldPlacementController.IsPointBlockedForEnemy(candidate))
            return false;

        if (boardWorldController.IsObstacleBlocked(candidate, bodyRadius))
            return false;

        foreach (EnemyTankController other in activeEnemies)
        {
            if (other == null || other == this)
                continue;

            float minDistance = (bodyRadius + other.bodyRadius) * 0.8f;
            if ((other.worldPosition - candidate).sqrMagnitude < minDistance * minDistance)
                return false;
        }

        return true;
    }

    private void ApplyRoleAndWaveScaling(int waveIndex)
    {
        int waveOffset = Mathf.Max(0, waveIndex - 1);
        float healthScale = 1f + waveOffset * 0.10f;
        float speedScale = 1f + Mathf.Min(0.28f, waveOffset * 0.025f);
        float fireScale = 1f + Mathf.Min(0.40f, waveOffset * 0.035f);
        float projectileScale = 1f + Mathf.Min(0.30f, waveOffset * 0.025f);

        switch (role)
        {
            case EnemyRole.Assault:
                scoreValue = 50;
                moveSpeed *= 1.05f;
                break;

            case EnemyRole.Gunner:
                scoreValue = 80;
                moveSpeed *= 0.82f;
                maxHealth *= 0.90f;
                shotsPerSecond *= 1.55f;
                fireRange *= 1.30f;
                projectileSpeed *= 1.12f;
                contactDamage *= 0.65f;
                transform.localScale *= 0.92f;
                break;

            case EnemyRole.Heavy:
                scoreValue = 140;
                moveSpeed *= 0.68f;
                maxHealth *= 2.25f;
                shotsPerSecond *= 0.75f;
                projectileDamage *= 1.45f;
                bodyRadius *= 1.20f;
                contactDamage *= 1.5f;
                transform.localScale *= 1.20f;
                break;
        }

        maxHealth *= healthScale;
        moveSpeed *= speedScale;
        shotsPerSecond *= fireScale;
        projectileSpeed *= projectileScale;
        scoreValue = Mathf.RoundToInt(scoreValue * (1f + waveOffset * 0.08f));
    }

    private void RefreshVisual(Vector2 facingDirection)
    {
        bool visible = boardWorldController.IsWorldPositionVisible(worldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(worldPosition, 0f);

        if (facingDirection.sqrMagnitude <= 0.0001f)
            return;

        float yaw = Mathf.Atan2(facingDirection.x, facingDirection.y) * Mathf.Rad2Deg;

        if (visualRoot != null)
            visualRoot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        if (turretRoot != null)
            turretRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void Die()
    {
        waveDirector?.NotifyEnemyKilled(this);
        Destroy(gameObject);
    }
}
