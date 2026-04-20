using UnityEngine;

public class ShellWeapon : WeaponBase
{
    public override void TryStartFire()
    {
        if (!CanFire())
            return;

        Fire();
    }

    private void Fire()
    {
        SpawnProjectile();
        StartCooldown();
    }
}
