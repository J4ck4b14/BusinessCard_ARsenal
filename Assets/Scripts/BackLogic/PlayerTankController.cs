using System;
using UnityEngine;

public class PlayerTankController : MonoBehaviour
{
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
    [SerializeField] private Camera arCamera;
    [SerializeField] private Transform boardSpaceRoot;
    [SerializeField] private Transform hullRoot;
    [SerializeField] private Transform turretRoot;
    [SerializeField] private Transform projectilesRoot;

    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2.75f;
    [SerializeField] private float turnDegreesPerSecond = 70f;
    [SerializeField] private float maxHealth = 5f;

    [Header("Weapon")]
    [SerializeField] private WeaponConfig primaryWeapon = new();

    private float hullYawDegrees;
    private float currentHealth;
    private float fireCooldown;

    public Vector2 WorldPosition => boardWorldController != null ? boardWorldController.PlayerWorldPosition : Vector2.zero;
    public float HullYawDegrees => hullYawDegrees;

    private void Awake()
    {
        Instance = this;

        if (arCamera == null)
            arCamera = Camera.main;

        currentHealth = maxHealth;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ResetForRun()
    {
        currentHealth = maxHealth;
        hullYawDegrees = 0f;
        fireCooldown = 0f;

        if (hullRoot != null)
            hullRoot.localRotation = Quaternion.identity;

        if (turretRoot != null)
            turretRoot.localRotation = Quaternion.identity;
    }

    public void TakeDamage(float amount)
    {
        if (boardGameController == null || boardGameController.CurrentState != BoardGameController.BoardGameState.Playing)
            return;

        currentHealth -= amount;
        Debug.Log($"PLAYER HIT for {amount}. Health now: {currentHealth}");

        if (currentHealth <= 0f)
        {
            Debug.Log("PLAYER DEAD -> EndRun()");
            if (boardGameController != null)
                boardGameController.EndRun();
        }
    }

    private void Update()
    {
        if (boardWorldController == null || commandInput == null || !boardWorldController.SimulationActive)
            return;

        fireCooldown -= Time.deltaTime;

        HandleMovement();
        HandleTurretFollow();
        HandleFire();
    }

    private void HandleMovement()
    {
        // Tank-style: allow forward/back and turning at the same time.
        float throttle = commandInput.Throttle; // -1..1
        float steer = commandInput.Steer;       // -1..1

        if (Mathf.Abs(steer) > 0.0001f)
            hullYawDegrees += steer * turnDegreesPerSecond * Time.deltaTime;

        if (Mathf.Abs(throttle) > 0.0001f)
            boardWorldController.MovePlayerWorld(GetHullForward() * (throttle * moveSpeed * Time.deltaTime));

        if (hullRoot != null)
            hullRoot.localRotation = Quaternion.Euler(0f, hullYawDegrees, 0f);
    }

    private void HandleTurretFollow()
    {
        if (turretRoot == null || arCamera == null)
            return;

        Transform root = boardSpaceRoot != null ? boardSpaceRoot : turretRoot.parent;
        Vector3 localForward = root != null
            ? root.InverseTransformDirection(arCamera.transform.forward)
            : arCamera.transform.forward;

        localForward.y = 0f;

        if (localForward.sqrMagnitude < 0.0001f)
            return;

        turretRoot.localRotation = Quaternion.LookRotation(localForward.normalized, Vector3.up);
    }

    private void HandleFire()
    {
        if (!commandInput.IsFireHeld)
            return;

        if (primaryWeapon.projectilePrefab == null)
            return;

        if (fireCooldown > 0f)
            return;

        fireCooldown = 1f / Mathf.Max(0.01f, primaryWeapon.shotsPerSecond);

        Vector2 aimDirection = GetAimDirection();
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

    private Vector2 GetAimDirection()
    {
        if (turretRoot == null)
            return GetHullForward();

        Transform root = boardSpaceRoot != null ? boardSpaceRoot : turretRoot.parent;
        Vector3 localForward = root != null
            ? root.InverseTransformDirection(turretRoot.forward)
            : turretRoot.forward;

        localForward.y = 0f;

        if (localForward.sqrMagnitude < 0.0001f)
            return GetHullForward();

        return new Vector2(localForward.x, localForward.z).normalized;
    }
}