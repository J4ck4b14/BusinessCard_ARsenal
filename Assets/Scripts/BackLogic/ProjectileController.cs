using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public enum ProjectileOwner
    {
        Player,
        Enemy
    }

    [SerializeField] private GameObject visualRoot;
    [SerializeField] private float hitRadius = 0.35f;
    [SerializeField] private Vector2 visibilityPadding = new(0.5f, 0.5f);
    [SerializeField] private float boardYOffset = 0.15f;

    private BoardWorldController boardWorldController;
    private Vector2 worldPosition;
    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float age;
    private ProjectileOwner owner;

    public void Initialize(
        BoardWorldController worldController,
        Vector2 startWorldPosition,
        Vector2 travelDirection,
        float projectileSpeed,
        float projectileDamage,
        float projectileLifetime,
        ProjectileOwner projectileOwner)
    {
        boardWorldController = worldController;
        worldPosition = startWorldPosition;
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        damage = projectileDamage;
        lifetime = projectileLifetime;
        owner = projectileOwner;

        if (visualRoot == null)
            visualRoot = gameObject;

        RefreshVisual();
    }

    private void FixedUpdate()
    {
        if (boardWorldController == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!boardWorldController.SimulationActive)
            return;

        float deltaTime = Time.fixedDeltaTime;
        age += deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 previous = worldPosition;
        Vector2 next = worldPosition + direction * speed * deltaTime;

        // Sample the middle as well so fast projectiles do not tunnel through a thin obstacle cell.
        Vector2 middle = Vector2.Lerp(previous, next, 0.5f);
        if (boardWorldController.IsObstacleBlocked(middle) || boardWorldController.IsObstacleBlocked(next))
        {
            Destroy(gameObject);
            return;
        }

        worldPosition = next;

        if (owner == ProjectileOwner.Enemy &&
            ShieldPlacementController.Instance != null &&
            ShieldPlacementController.Instance.IsPointBlockedForEnemy(worldPosition))
        {
            Destroy(gameObject);
            return;
        }

        if (owner == ProjectileOwner.Player)
        {
            foreach (EnemyTankController enemy in EnemyTankController.ActiveEnemies)
            {
                if (enemy == null)
                    continue;

                if (Vector2.Distance(worldPosition, enemy.WorldPosition) <= hitRadius)
                {
                    enemy.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else if (PlayerTankController.Instance != null &&
                 Vector2.Distance(worldPosition, PlayerTankController.Instance.WorldPosition) <= hitRadius)
        {
            PlayerTankController.Instance.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (boardWorldController == null)
            return;

        bool visible = boardWorldController.IsWorldPositionVisible(worldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(worldPosition, boardYOffset);
    }
}
