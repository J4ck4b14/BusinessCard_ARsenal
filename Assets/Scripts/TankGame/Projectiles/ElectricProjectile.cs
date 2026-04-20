using UnityEngine;

public class ElectricProjectile : ProjectileBase
{
    public float maxScaleMult = 2.5f;
    [SerializeField] private bool isCharging = false;

    public void Initialize(float chargedDamage, float chargeRatio)
    {
        damage = chargedDamage;

        float scale = Mathf.Lerp(1f, maxScaleMult, chargeRatio);
        transform.localScale = Vector3.one * scale;
    }

    protected override bool HandleHealthHit(Health health)
    {
        float healthBefore = health.currentHealth;
        health.TakeDamage(damage);

        float remainingDamage = damage - healthBefore;

        if (remainingDamage > 0f)
        {
            damage = remainingDamage;
            return false;
        }

        return true;
    }
    
    private void Update()
    {
        if (isCharging)
            return;

        transform.position += transform.forward * speed * Time.deltaTime;
    }
}