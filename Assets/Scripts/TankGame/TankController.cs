using UnityEngine;
using UnityEngine.InputSystem;

public class TankController : MonoBehaviour
{
    public Faction faction;

    private TankMotor motor;
    private float motorSpeed;
    private float motorTorque;

    [SerializeField] private WeaponBase[] weapons;

    private void OnEnable()
    {
        motor = GetComponent<TankMotor>();
        weapons = GetComponents<WeaponBase>();

        motorSpeed = 5f;
        motorTorque = 10f;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (faction == Faction.Player)
        {
            HandlePlayerInput();
        }
        
    }

    private void HandlePlayerInput()
    {
        if (Keyboard.current.wKey.isPressed)
        {
            motor.Thrust(motorSpeed);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            motor.Thrust(-motorSpeed);
        }
        else if (Keyboard.current.aKey.isPressed)
        {
            motor.Rotate(-motorTorque);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            motor.Rotate(motorTorque);
        }

        if (Mouse.current.leftButton.isPressed)
        {
            weapons[0].TryStartFire();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            weapons[0].StopFire();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (weapons.Length > 1)
                weapons[1].TryStartFire();
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            if (weapons.Length > 1)
                weapons[1].StopFire();
        }
    }
}
