using System.Collections;
using UnityEngine;

public class ShieldWallController : MonoBehaviour
{
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Vector2 blockSize = new(2f, 1f);
    [SerializeField] private float lifetime = 10f;
    [SerializeField] private float boardYOffset = 0.05f;
    [SerializeField] private Vector2 visibilityPadding = new(1f, 1f);
    [SerializeField] private float lifetimeTick = 0.05f;

    private BoardWorldController boardWorldController;
    private Vector2 centerWorldPosition;
    private float yawDegrees;
    private float remainingLife;
    private Coroutine lifetimeCoroutine;

    public void Initialize(BoardWorldController controller, Vector2 worldPosition, float yaw, float lifeTimeSeconds)
    {
        Unsubscribe();

        boardWorldController = controller;
        centerWorldPosition = worldPosition;
        yawDegrees = yaw;
        remainingLife = lifeTimeSeconds;

        if (visualRoot == null)
            visualRoot = gameObject;

        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged += OnPlayerWorldPositionChanged;

        RefreshVisual();
        lifetimeCoroutine = StartCoroutine(LifetimeCoroutine());
    }

    private void OnDestroy()
    {
        Unsubscribe();
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

    private IEnumerator LifetimeCoroutine()
    {
        WaitForSeconds wait = new(Mathf.Max(0.01f, lifetimeTick));

        while (boardWorldController != null && remainingLife > 0f)
        {
            if (boardWorldController.SimulationActive)
                remainingLife -= Mathf.Max(0.01f, lifetimeTick);

            yield return wait;
        }

        lifetimeCoroutine = null;

        if (this != null && gameObject != null)
            Destroy(gameObject);
    }

    private void OnPlayerWorldPositionChanged(Vector2 _)
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (boardWorldController == null)
            return;

        bool visible = boardWorldController.IsWorldPositionVisible(centerWorldPosition, visibilityPadding);
        bool forceShow = boardWorldController.DebugShowOffBoardEntities;

        if (visualRoot != null)
            visualRoot.SetActive(forceShow || visible);

        transform.localPosition = boardWorldController.WorldToBoardLocal(centerWorldPosition, boardYOffset);
        transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
    }

    private void Unsubscribe()
    {
        if (boardWorldController != null)
            boardWorldController.PlayerWorldPositionChanged -= OnPlayerWorldPositionChanged;
    }
}
