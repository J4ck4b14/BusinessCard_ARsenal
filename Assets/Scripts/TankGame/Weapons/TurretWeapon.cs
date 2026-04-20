using UnityEngine;

public class TurretWeapon : WeaponBase
{
    [Header("Heat")]
    [SerializeField] private float shotInterval = 0.1f;
    [SerializeField] private float heatPerShot = 10f;
    [SerializeField] private float maxHeat = 100f;
    [SerializeField] private float coolRate = 30f;

    [SerializeField] private float currentHeat;
    private bool isFiring;
    [SerializeField] private bool isOverheated;

    private void Update()
    {
        // If overheated, dissipate heat until it's cool. Otherwise, cool down when not firing.
        if (isOverheated)
            DissipateHeat();
        else
            CoolDown();

        if (isFiring)
            TryAutoFire();
    }

    public override void TryStartFire()
    {
        isFiring = true;
    }

    public override void StopFire()
    {
        isFiring = false;
    }

    private void TryAutoFire()
    {
        if (isOverheated) return;
        if (!CanFire()) return;

        Fire();
    }

    private void Fire()
    {
        SpawnProjectile();
        currentHeat += heatPerShot;

        if (currentHeat >= maxHeat)
        {
            isOverheated = true;
        }

        nextFireTime = Time.time + shotInterval;
    }

    private void CoolDown()
    {
        if (isFiring) return;

        currentHeat = Mathf.Max(0f, currentHeat - coolRate * Time.deltaTime);

    }

    private void DissipateHeat()
    {
        currentHeat = Mathf.Max(0f, currentHeat - coolRate * (2f / 3f) * Time.deltaTime);
        if (currentHeat <= 0f)
            isOverheated = false;
    }
}
