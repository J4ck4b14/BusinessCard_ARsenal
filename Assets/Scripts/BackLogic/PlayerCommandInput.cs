using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerCommandInput : MonoBehaviour
{
    public enum MoveCommand
    {
        None,
        Forward,
        Backward,
        TurnLeft,
        TurnRight
    }

    [System.Serializable]
    private sealed class HeldState
    {
        public bool keyboardHeld;
        public bool uiHeld;
        public double lastPressedTime;

        public bool IsHeld => keyboardHeld || uiHeld;
    }

    [Header("Editor keyboard")]
    [SerializeField] private bool enableKeyboardInEditor = true;

    private readonly HeldState forward = new();
    private readonly HeldState backward = new();
    private readonly HeldState turnLeft = new();
    private readonly HeldState turnRight = new();

    private bool keyboardFireHeld;
    private bool uiFireHeld;

    // Kept for compatibility with existing code/UI.
    public MoveCommand CurrentMoveCommand => EvaluateMoveCommand();

    // New: tank-style axes (can be used simultaneously).
    // throttle: -1 (back) .. +1 (forward)
    // steer:    -1 (left) .. +1 (right)
    public float Throttle
    {
        get
        {
            float value = 0f;
            if (forward.IsHeld) value += 1f;
            if (backward.IsHeld) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }
    }

    public float Steer
    {
        get
        {
            float value = 0f;
            if (turnRight.IsHeld) value += 1f;
            if (turnLeft.IsHeld) value -= 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }
    }

    public bool IsFireHeld => keyboardFireHeld || uiFireHeld;

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!enableKeyboardInEditor || Keyboard.current == null)
            return;

        SyncKeyboardState(Keyboard.current.wKey, forward);
        SyncKeyboardState(Keyboard.current.sKey, backward);
        SyncKeyboardState(Keyboard.current.aKey, turnLeft);
        SyncKeyboardState(Keyboard.current.dKey, turnRight);

        keyboardFireHeld =
            Keyboard.current.spaceKey.isPressed ||
            Keyboard.current.eKey.isPressed;
#endif
    }

    public void SetUiHeld(MoveCommand command, bool held)
    {
        switch (command)
        {
            case MoveCommand.Forward:
                SetUiState(forward, held);
                break;
            case MoveCommand.Backward:
                SetUiState(backward, held);
                break;
            case MoveCommand.TurnLeft:
                SetUiState(turnLeft, held);
                break;
            case MoveCommand.TurnRight:
                SetUiState(turnRight, held);
                break;
        }
    }

    public void SetFireHeld(bool held)
    {
        uiFireHeld = held;
    }

    private void SyncKeyboardState(KeyControl key, HeldState state)
    {
        if (key.wasPressedThisFrame)
            state.lastPressedTime = Time.realtimeSinceStartupAsDouble;

        state.keyboardHeld = key.isPressed;
    }

    private static void SetUiState(HeldState state, bool held)
    {
        if (held && !state.uiHeld)
            state.lastPressedTime = Time.realtimeSinceStartupAsDouble;

        state.uiHeld = held;
    }

    private MoveCommand EvaluateMoveCommand()
    {
        MoveCommand best = MoveCommand.None;
        double bestTime = double.NegativeInfinity;

        Consider(forward, MoveCommand.Forward, ref best, ref bestTime);
        Consider(backward, MoveCommand.Backward, ref best, ref bestTime);
        Consider(turnLeft, MoveCommand.TurnLeft, ref best, ref bestTime);
        Consider(turnRight, MoveCommand.TurnRight, ref best, ref bestTime);

        return best;
    }

    private static void Consider(HeldState state, MoveCommand command, ref MoveCommand best, ref double bestTime)
    {
        if (!state.IsHeld)
            return;

        if (state.lastPressedTime >= bestTime)
        {
            bestTime = state.lastPressedTime;
            best = command;
        }
    }
}