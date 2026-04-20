using UnityEngine;

public class ElectricProjectile : ProjectileBase
{
    [SerializeField] private float maxScaleMult = 2.5f;

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
}