using System;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
    public enum UpgradeType
    {
        RapidFire,
        HeavyShells,
        Mobility
    }

    [Serializable]
    public sealed class WeaponConfig
    {
        public ProjectileController projectilePrefab;
        public float shotsPerSecond = 2f;
        public float projectileSpeed = 9f;
        public float damage = 1f;
        public float lifetime = 3f;
        public float spawnDistance = 0.8f;
    }

    public static PlayerTankController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BoardWorldController boardWorldController;
    [SerializeField] private BoardGameController boardGameController;
    [SerializeField] private PlayerCommandInput commandInput;
    [SerializeField] private Transform boardSpaceRoot;
    [SerializeField] private Transform hullRoot;
    [SerializeField] private Transform turretRoot;
    [SerializeField] private Transform projectilesRoot;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2.75f;
    [SerializeField] private float turnDegreesPerSecond = 70f;
    [SerializeField] private float maxHealth = 5f;

    [Header("Targeting")]
    [SerializeField] private float targetRange = 10f;

    [Header("Weapon")]
    [SerializeField] private WeaponConfig primaryWeapon = new();

    private float baseMoveSpeed;
    private float baseMaxHealth;
    private float baseShotsPerSecond;
    private float baseProjectileSpeed;
    private float baseDamage;

    private float hullYawDegrees;
    private float currentHealth;
    private float fireCooldown;
    private Vector2 currentAimDirection = Vector2.up;
    private bool hasTarget;

    public Vector2 WorldPosition => boardWorldController != null ? boardWorldController.PlayerWorldPosition : Vector2.zero;
    public float HullYawDegrees => hullYawDegrees;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool HasTarget => hasTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one PlayerTankController is active. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;

        baseMoveSpeed = moveSpeed;
        baseMaxHealth = maxHealth;
        baseShotsPerSecond = primaryWeapon.shotsPerSecond;
        baseProjectileSpeed = primaryWeapon.projectileSpeed;
        baseDamage = primaryWeapon.damage;

        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetForRun()
    {
        moveSpeed = baseMoveSpeed;
        maxHealth = baseMaxHealth;
        primaryWeapon.shotsPerSecond = baseShotsPerSecond;
        primaryWeapon.projectileSpeed = baseProjectileSpeed;
        primaryWeapon.damage = baseDamage;

        currentHealth = maxHealth;
        hullYawDegrees = 0f;
        fireCooldown = 0f;
        currentAimDirection = Vector2.up;
        hasTarget = false;

        DestroyChildren(projectilesRoot);

        if (hullRoot != null)
            hullRoot.localRotation = Quaternion.identity;

        if (turretRoot != null)
            turretRoot.localRotation = Quaternion.identity;
    }

    public void ApplyUpgrade(UpgradeType upgrade)
    {
        switch (upgrade)
        {
            case UpgradeType.RapidFire:
                primaryWeapon.shotsPerSecond *= 1.25f;
                break;

            case UpgradeType.HeavyShells:
                primaryWeapon.damage *= 1.35f;
                primaryWeapon.projectileSpeed *= 1.08f;
                break;

            case UpgradeType.Mobility:
                moveSpeed *= 1.15f;
                currentHealth = Mathf.Min(maxHealth, currentHealth + 1f);
                break;
        }
    }

    public void TakeDamage(float amount)
    {
        if (boardGameController == null || boardGameController.CurrentState != BoardGameController.BoardGameState.Playing)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"PLAYER HIT for {amount}. Health now: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Debug.Log("PLAYER DEAD -> EndRun()");
            boardGameController.EndRun();
        }
    }

    private void FixedUpdate()
    {
        if (boardWorldController == null || commandInput == null || !boardWorldController.SimulationActive)
            return;

        float deltaTime = Time.fixedDeltaTime;
        fireCooldown = Mathf.Max(0f, fireCooldown - deltaTime);

        HandleMovement(deltaTime);
        HandleTargeting();
        HandleFire();
    }

    private void HandleMovement(float deltaTime)
    {
        float throttle = commandInput.Throttle;
        float steer = commandInput.Steer;

        if (Mathf.Abs(steer) > 0.0001f)
            hullYawDegrees += steer * turnDegreesPerSecond * deltaTime;

        if (Mathf.Abs(throttle) > 0.0001f)
        {
            Vector2 delta = GetHullForward() * (throttle * moveSpeed * deltaTime);
            boardWorldController.TryMovePlayerWorld(delta);
        }

        if (hullRoot != null)
            hullRoot.localRotation = Quaternion.Euler(0f, hullYawDegrees, 0f);
    }

    private void HandleTargeting()
    {
        Vector2 playerPosition = WorldPosition;
        float bestDistanceSq = targetRange * targetRange;
        EnemyTankController bestEnemy = null;

        foreach (EnemyTankController enemy in EnemyTankController.ActiveEnemies)
        {
            if (enemy == null)
                continue;

            float distanceSq = (enemy.WorldPosition - playerPosition).sqrMagnitude;
            if (distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestEnemy = enemy;
        }

        hasTarget = bestEnemy != null;
        currentAimDirection = hasTarget
            ? (bestEnemy.WorldPosition - playerPosition).normalized
            : GetHullForward();

        if (turretRoot != null && currentAimDirection.sqrMagnitude > 0.0001f)
        {
            float yaw = Mathf.Atan2(currentAimDirection.x, currentAimDirection.y) * Mathf.Rad2Deg;
            turretRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    private void HandleFire()
    {
        if (!commandInput.IsFireHeld || primaryWeapon.projectilePrefab == null || fireCooldown > 0f)
            return;

        fireCooldown = 1f / Mathf.Max(0.01f, primaryWeapon.shotsPerSecond);

        Vector2 aimDirection = currentAimDirection.sqrMagnitude > 0.0001f
            ? currentAimDirection.normalized
            : GetHullForward();

        Vector2 spawnWorldPosition = WorldPosition + aimDirection * primaryWeapon.spawnDistance;

        ProjectileController projectile = projectilesRoot != null
            ? Instantiate(primaryWeapon.projectilePrefab, projectilesRoot)
            : Instantiate(primaryWeapon.projectilePrefab);

        projectile.Initialize(
            boardWorldController,
            spawnWorldPosition,
            aimDirection,
            primaryWeapon.projectileSpeed,
            primaryWeapon.damage,
            primaryWeapon.lifetime,
            ProjectileController.ProjectileOwner.Player);
    }

    private Vector2 GetHullForward()
    {
        float radians = hullYawDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)).normalized;
    }

    private static void DestroyChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }
}
