using UnityEngine;

public class ShieldWallController : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Vector2 blockSize = new(2f, 1f);
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float boardYOffset = 0.05f;
    [SerializeField] private Vector2 visibilityPadding = new(1f, 1f);

    private BoardWorldController boardWorldController;
    private Vector2 centerWorldPosition;
    private float yawDegrees;
    private float remainingLife;

    public void Initialize(BoardWorldController controller, Vector2 worldPosition, float yaw, float lifeTimeSeconds)
    {
        boardWorldController = controller;
        centerWorldPosition = worldPosition;
        yawDegrees = yaw;
        remainingLife = lifeTimeSeconds;

        if (visualRoot == null)
            visualRoot = gameObject;
    }

    public bool BlocksEnemy(Vector2 worldPoint)
    {
        Vector2 delta = worldPoint - centerWorldPosition;
        float radians = -yawDegrees * Mathf.Deg2Rad;

        float localX = delta.x * Mathf.Cos(radians) - delta.y * Mathf.Sin(radians);
        float localZ = delta.x * Mathf.Sin(radians) + delta.y * Mathf.Cos(radians);

        return Mathf.Abs(localX) <= blockSize.x * 0.5f &&
               Mathf.Abs(localZ) <= blockSize.y * 0.5f;
    }

    private void Update()
    {
        if (boardWorldController == null)
        {
            Destroy(gameObject);
            return;
        }

        if (boardWorldController.SimulationActive)
        {
            remainingLife -= Time.deltaTime;
            if (remainingLife <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        bool visible = boardWorldController.IsWorldPositionVisible(centerWorldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(centerWorldPosition, boardYOffset);
        transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
    }
}