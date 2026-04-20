using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class TankController : MonoBehaviour
{
    public Faction faction;

    private TankMotor motor;
    private float motorSpeed;
    private float motorTorque;

    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private NavMeshAgent agent;

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
        else
        {
            AIBehaviour();
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

    private void AIBehaviour()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent.destination == null)
        {
            agent.SetDestination(GameObject.FindGameObjectWithTag("Player").transform.position);
            Debug.Log($"AI Tank setting destination to player position: {agent.destination}");
        }

        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 80f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                weapons[0].TryStartFire();
            }
            else
            {
                weapons[0].StopFire();
            }
        }
    }
}
