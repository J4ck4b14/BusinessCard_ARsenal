using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected GameObject owner;
    [SerializeField] protected Faction faction;
    [SerializeField] protected Transform cannonEnd;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float cooldown = 0.5f;

    protected float nextFireTime;

    private void OnEnable()
    {
        this.owner = GetComponentInParent<TankController>().gameObject;
        this.faction = GetComponentInParent<TankController>().faction;
    }

    public abstract void TryStartFire();
    public virtual void StopFire() {}

    protected virtual bool CanFire()
    {
        return Time.time >= nextFireTime;
    }

    protected virtual void StartCooldown()
    {
        nextFireTime = Time.time + cooldown;
    }

    protected virtual ProjectileBase SpawnProjectile()
    {
        GameObject instance = Instantiate(projectilePrefab, cannonEnd.position, cannonEnd.rotation);
        
        if(instance.TryGetComponent<ProjectileBase>(out ProjectileBase projectile))
        {
            projectile.faction = faction;
            return projectile;
        }
        
        return null;
    }
}
