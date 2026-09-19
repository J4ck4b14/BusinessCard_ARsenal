using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Keyboard, gamepad and the mobile buttons all end up as the same tank commands.
public sealed class PlayerCommandInput : MonoBehaviour
{
    public enum MoveCommand
    {
        None,
        Forward,
        Backward,
        TurnLeft,
        TurnRight
    }

    [Header("Input System")]
    [SerializeField] private bool enableKeyboardInEditor = true;
    [SerializeField] private bool enableGamepad = true;

    private InputAction moveAction;
    private InputAction fireAction;

    private Vector2 actionMove;
    private bool actionFireHeld;

    private bool uiForward;
    private bool uiBackward;
    private bool uiLeft;
    private bool uiRight;
    private bool uiFireHeld;

    private double forwardPressedAt;
    private double backwardPressedAt;
    private double leftPressedAt;
    private double rightPressedAt;

    public event Action<Vector2> MoveChanged;
    public event Action<bool> FireChanged;

    // X steers, Y is throttle.
    public Vector2 Move
    {
        get
        {
            Vector2 combined = actionMove + GetUiMove();
            return Vector2.ClampMagnitude(combined, 1f);
        }
    }

    public float Throttle => Move.y;
    public float Steer => Move.x;
    public bool IsFireHeld => actionFireHeld || uiFireHeld;

    // Kept for compatibility with old UI/debug code.
    public MoveCommand CurrentMoveCommand => EvaluateMoveCommand();

    private void Awake()
    {
        BuildActions();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        fireAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        fireAction?.Disable();

        actionMove = Vector2.zero;
        actionFireHeld = false;
        ClearUiState();
    }

    private void OnDestroy()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Dispose();
        }

        if (fireAction != null)
        {
            fireAction.started -= OnFireStarted;
            fireAction.canceled -= OnFireCanceled;
            fireAction.Dispose();
        }
    }

    public void SetUiHeld(MoveCommand command, bool held)
    {
        double now = Time.realtimeSinceStartupAsDouble;

        switch (command)
        {
            case MoveCommand.Forward:
                if (held && !uiForward) forwardPressedAt = now;
                uiForward = held;
                break;

            case MoveCommand.Backward:
                if (held && !uiBackward) backwardPressedAt = now;
                uiBackward = held;
                break;

            case MoveCommand.TurnLeft:
                if (held && !uiLeft) leftPressedAt = now;
                uiLeft = held;
                break;

            case MoveCommand.TurnRight:
                if (held && !uiRight) rightPressedAt = now;
                uiRight = held;
                break;
        }

        MoveChanged?.Invoke(Move);
    }

    public void SetFireHeld(bool held)
    {
        if (uiFireHeld == held)
            return;

        uiFireHeld = held;
        FireChanged?.Invoke(IsFireHeld);
    }

    private void BuildActions()
    {
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            expectedControlType: "Vector2");

#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableKeyboardInEditor)
        {
            moveAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            moveAction
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
#endif

        if (enableGamepad)
            moveAction.AddBinding("<Gamepad>/leftStick");

        fireAction = new InputAction(
            name: "Fire",
            type: InputActionType.Button,
            expectedControlType: "Button");

#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableKeyboardInEditor)
        {
            fireAction.AddBinding("<Keyboard>/space");
            fireAction.AddBinding("<Keyboard>/e");
        }
#endif

        if (enableGamepad)
            fireAction.AddBinding("<Gamepad>/buttonSouth");

        moveAction.performed += OnMovePerformed;
        moveAction.canceled += OnMoveCanceled;
        fireAction.started += OnFireStarted;
        fireAction.canceled += OnFireCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        actionMove = context.ReadValue<Vector2>();
        MoveChanged?.Invoke(Move);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        actionMove = Vector2.zero;
        MoveChanged?.Invoke(Move);
    }

    private void OnFireStarted(InputAction.CallbackContext context)
    {
        actionFireHeld = true;
        FireChanged?.Invoke(IsFireHeld);
    }

    private void OnFireCanceled(InputAction.CallbackContext context)
    {
        actionFireHeld = false;
        FireChanged?.Invoke(IsFireHeld);
    }

    private Vector2 GetUiMove()
    {
        float x = 0f;
        float y = 0f;

        if (uiRight) x += 1f;
        if (uiLeft) x -= 1f;
        if (uiForward) y += 1f;
        if (uiBackward) y -= 1f;

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private void ClearUiState()
    {
        uiForward = false;
        uiBackward = false;
        uiLeft = false;
        uiRight = false;
        uiFireHeld = false;
    }

    private MoveCommand EvaluateMoveCommand()
    {
        MoveCommand best = MoveCommand.None;
        double bestTime = double.NegativeInfinity;

        Consider(uiForward || actionMove.y > 0.1f, forwardPressedAt, MoveCommand.Forward, ref best, ref bestTime);
        Consider(uiBackward || actionMove.y < -0.1f, backwardPressedAt, MoveCommand.Backward, ref best, ref bestTime);
        Consider(uiLeft || actionMove.x < -0.1f, leftPressedAt, MoveCommand.TurnLeft, ref best, ref bestTime);
        Consider(uiRight || actionMove.x > 0.1f, rightPressedAt, MoveCommand.TurnRight, ref best, ref bestTime);

        return best;
    }

    private static void Consider(bool held, double pressedAt, MoveCommand command, ref MoveCommand best, ref double bestTime)
    {
        if (!held || pressedAt < bestTime)
            return;

        bestTime = pressedAt;
        best = command;
    }
}
