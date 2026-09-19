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

    [SerializeField] private WeaponBase primaryWeapon;
    [SerializeField] private GameObject player;    

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
        player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<TankController>().faction = Faction.Player;
        primaryWeapon = weapons[0];
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
            EnemyBehaviour();
        }

    }

    private void HandlePlayerInput()
    {
        // Switch weapons (once per key press)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            primaryWeapon = weapons[0];
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            primaryWeapon = weapons[1];
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            primaryWeapon = weapons[2];

        if (Keyboard.current.wKey.isPressed)
            motor.Thrust(motorSpeed);
        else if (Keyboard.current.sKey.isPressed)
            motor.Thrust(-motorSpeed);
        else if (Keyboard.current.aKey.isPressed)
            motor.Rotate(-motorTorque);
        else if (Keyboard.current.dKey.isPressed)
            motor.Rotate(motorTorque);

        if (Mouse.current.leftButton.isPressed)
            primaryWeapon.TryStartFire();
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
            primaryWeapon.StopFire();
    }

    private void EnemyBehaviour()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent.destination != player.transform.position)
        {
            agent.SetDestination(player.transform.position);
            Debug.Log($"Enemy tank destination: {agent.destination}");
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
