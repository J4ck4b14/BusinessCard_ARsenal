using UnityEngine;
using UnityEngine.Events;

public class ArsenalInteractable : MonoBehaviour
{
    [SerializeField] private UnityEvent onPressed;

    public void Press()
    {
        onPressed?.Invoke();
    }
}
