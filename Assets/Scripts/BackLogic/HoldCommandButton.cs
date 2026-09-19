using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoldCommandButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ButtonMode
    {
        Forward,
        Backward,
        TurnLeft,
        TurnRight,
        Fire
    }

    [SerializeField] private PlayerCommandInput commandInput;
    [SerializeField] private ButtonMode mode;

    private void Awake()
    {
        ResolveCommandInput();
    }

    private void OnDisable()
    {
        // Never leave a command latched if the UI disappears while a finger is down.
        SetPressed(false);
    }

    public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
    public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
    public void OnPointerExit(PointerEventData eventData) => SetPressed(false);

    private void ResolveCommandInput()
    {
        if (commandInput != null)
            return;

        BackTrackedContentHandler backRoot = GetComponentInParent<BackTrackedContentHandler>(true);
        if (backRoot != null)
            commandInput = backRoot.GetComponentInChildren<PlayerCommandInput>(true);
    }

    private void SetPressed(bool pressed)
    {
        if (commandInput == null)
            ResolveCommandInput();

        if (commandInput == null)
            return;

        switch (mode)
        {
            case ButtonMode.Forward:
                commandInput.SetUiHeld(PlayerCommandInput.MoveCommand.Forward, pressed);
                break;
            case ButtonMode.Backward:
                commandInput.SetUiHeld(PlayerCommandInput.MoveCommand.Backward, pressed);
                break;
            case ButtonMode.TurnLeft:
                commandInput.SetUiHeld(PlayerCommandInput.MoveCommand.TurnLeft, pressed);
                break;
            case ButtonMode.TurnRight:
                commandInput.SetUiHeld(PlayerCommandInput.MoveCommand.TurnRight, pressed);
                break;
            case ButtonMode.Fire:
                commandInput.SetFireHeld(pressed);
                break;
        }
    }
}
