using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ProjectileBase : MonoBehaviour
{
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float speed = 16f;
    [SerializeField] protected float lifetime = 5f;
    [SerializeField] protected bool destroyOnImpact = true;
    public Faction faction;

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    protected virtual void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<TankController>().faction == faction)
        {
            // Ignore collisions with the same faction
            return;
        }
        if (other.TryGetComponent<Health>(out Health health))
        {
            bool shouldDestroy = HandleHealthHit(health);

            if(shouldDestroy)
                Destroy(gameObject);

            return;
        }

        if(destroyOnImpact)
        {
            HandleEnvironmentHit(other);
            Destroy(gameObject);
        }
    }

    protected virtual bool HandleHealthHit(Health health)
    {
        health.TakeDamage(damage);
        return true;
    }

    protected virtual void HandleEnvironmentHit(Collider other)
    {
        // TODO: Add effects or sounds when hitting the environment
    }
}
