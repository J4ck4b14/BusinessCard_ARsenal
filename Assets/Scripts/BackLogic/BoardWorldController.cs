using UnityEngine;
using UnityEngine.InputSystem;

public class BoardWorldController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerVisualRoot;

    [Header("Board viewport")]
    [SerializeField] private float boardHalfWidth = 4f;
    [SerializeField] private float boardHalfHeight = 2.5f;

    [Header("Editor-only movement")]
    [SerializeField] private float editorMoveSpeed = 4f;

    private Vector2 playerWorldPosition;
    private bool simulationActive;

    public Vector2 PlayerWorldPosition => playerWorldPosition;
    public float BoardHalfWidth => boardHalfWidth;
    public float BoardHalfHeight => boardHalfHeight;
    public bool SimulationActive => simulationActive;

    [SerializeField] private bool debugShowOffBoardEntities = true;
    public bool DebugShowOffBoardEntities => debugShowOffBoardEntities;

    public void BeginRun()
    {
        playerWorldPosition = Vector2.zero;

        if (playerVisualRoot != null)
            playerVisualRoot.localPosition = Vector3.zero;
    }

    public void SetSimulationActive(bool active)
    {
        simulationActive = active;
    }

    public void SetPlayerWorldPosition(Vector2 newWorldPosition)
    {
        playerWorldPosition = newWorldPosition;
    }

    public void MovePlayerWorld(Vector2 delta)
    {
        playerWorldPosition += delta;
    }

    public Vector3 WorldToBoardLocal(Vector2 worldPosition, float worldY = 0f)
    {
        Vector2 relative = worldPosition - playerWorldPosition;
        return new Vector3(relative.x, worldY, relative.y);
    }

    public Vector2 BoardLocalToWorld(Vector3 boardLocalPosition)
    {
        return playerWorldPosition + new Vector2(boardLocalPosition.x, boardLocalPosition.z);
    }

    public bool IsWorldPositionVisible(Vector2 worldPosition, Vector2 padding)
    {
        Vector2 relative = worldPosition - playerWorldPosition;

        return Mathf.Abs(relative.x) <= boardHalfWidth + padding.x &&
               Mathf.Abs(relative.y) <= boardHalfHeight + padding.y;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!simulationActive || Keyboard.current == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;

        if (input.sqrMagnitude > 1f)
            input.Normalize();

        if (input != Vector2.zero)
            MovePlayerWorld(input * editorMoveSpeed * Time.deltaTime);
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(boardHalfWidth * 2f, 0.01f, boardHalfHeight * 2f);
        Gizmos.DrawWireCube(center, size);
    }
}