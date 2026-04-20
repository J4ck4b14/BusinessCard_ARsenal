using UnityEngine;

public class ChargeWeapon : WeaponBase
{
    [Header("Charge")]
    [SerializeField] private float maxChargeTime = 3f;
    [SerializeField] private float minDamage = 5f;
    [SerializeField] private float maxDamage = 40f;

    private bool isCharging;
    private float chargeStartTime;

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

        ProjectileBase projectile = SpawnProjectile();
        if (projectile == null) return;

        // TODO: Pass charge data into the projectile
    }
}
