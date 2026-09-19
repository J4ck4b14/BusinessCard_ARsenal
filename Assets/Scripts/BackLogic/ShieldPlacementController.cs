using System.Collections.Generic;
using UnityEngine;

public class ShieldPlacementController : MonoBehaviour
{
    public static ShieldPlacementController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BoardWorldController boardWorldController;
    [SerializeField] private PlayerTankController playerTankController;
    [SerializeField] private ShieldWallController shieldPrefab;
    [SerializeField] private Transform shieldsRoot;

    [Header("Placement")]
    [SerializeField] private int maxActiveShields = 3;
    [SerializeField] private float shieldLifetime = 10f;
    [SerializeField] private float snapSize = 0.5f;

    private readonly List<ShieldWallController> activeShields = new();

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TryPlaceShieldAtBoardLocal(Vector3 boardLocalPoint)
    {
        if (boardWorldController == null || !boardWorldController.SimulationActive || shieldPrefab == null)
            return;

        Vector3 snappedLocal = new(
            Snap(boardLocalPoint.x),
            0f,
            Snap(boardLocalPoint.z));

        Vector2 worldPosition = boardWorldController.BoardLocalToWorld(snappedLocal);

        // A shield cannot be spawned inside procedural cover.
        if (boardWorldController.IsObstacleBlocked(worldPosition, snapSize * 0.35f))
            return;

        float yaw = playerTankController != null ? playerTankController.HullYawDegrees : 0f;

        ShieldWallController shield = shieldsRoot != null
            ? Instantiate(shieldPrefab, shieldsRoot)
            : Instantiate(shieldPrefab);

        shield.Initialize(boardWorldController, worldPosition, yaw, shieldLifetime);
        activeShields.Add(shield);

        while (activeShields.Count > maxActiveShields)
        {
            if (activeShields[0] != null)
                Destroy(activeShields[0].gameObject);

            activeShields.RemoveAt(0);
        }
    }

    public bool IsPointBlockedForEnemy(Vector2 worldPoint)
    {
        CompactNulls();

        foreach (ShieldWallController shield in activeShields)
        {
            if (shield != null && shield.BlocksEnemy(worldPoint))
                return true;
        }

        return false;
    }

    public void ClearAll()
    {
        foreach (ShieldWallController shield in activeShields)
        {
            if (shield != null)
                Destroy(shield.gameObject);
        }

        activeShields.Clear();
    }

    private void CompactNulls()
    {
        for (int i = activeShields.Count - 1; i >= 0; i--)
        {
            if (activeShields[i] == null)
                activeShields.RemoveAt(i);
        }
    }

    private float Snap(float value)
    {
        return Mathf.Round(value / snapSize) * snapSize;
    }
}