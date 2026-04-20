using UnityEngine;

public class ChargeWeapon : WeaponBase
{
    [Header("Charge")]
    [SerializeField] private float maxChargeTime = 3f;
    [SerializeField] private float minDamage = 5f;
    [SerializeField] private float maxDamage = 40f;

    private bool isCharging;
    private float chargeStartTime;
    private ElectricProjectile currentProjectile;

    private void Update()
    {
        if (isCharging)
        {
            
            float heldTime = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
            float chargePercent = heldTime / maxChargeTime;

            // Keeping the projectile attached to the cannon
            currentProjectile.transform.position = cannonEnd.position;
            currentProjectile.transform.rotation = cannonEnd.rotation;
    
            // Update the projectile's damage and scale based on charge
            currentProjectile.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * currentProjectile.maxScaleMult, chargePercent);
        }
    }

    public override void TryStartFire()
    {
        if (!CanFire()) return;
        if (isCharging) return;

        isCharging = true;
        chargeStartTime = Time.time;
    }

    public override void StopFire()
    {
        if (!isCharging) return;

        FireChargedShot();
        isCharging = false;
        StartCooldown();
    }

    private void FireChargedShot()
    {
        float heldTime = Mathf.Min(Time.time - chargeStartTime, maxChargeTime);
        float chargePercent = heldTime / maxChargeTime;

        ElectricProjectile projectile = (ElectricProjectile)SpawnProjectile();
        if (projectile == null) return;

        projectile.Initialize(Mathf.Lerp(minDamage, maxDamage, chargePercent), chargePercent);
    }
}
