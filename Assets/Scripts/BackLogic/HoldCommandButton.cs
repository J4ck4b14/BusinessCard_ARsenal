using UnityEngine;
using UnityEngine.EventSystems;

public class HoldCommandButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
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