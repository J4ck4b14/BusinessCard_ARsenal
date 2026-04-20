using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
// This class handles the health of the tank. It will be used to determine when the tank is destroyed and to update the health bar UI.
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    [SerializeField] private Slider healthBar;
    [SerializeField] private Image fillImage;
    [SerializeField] private Gradient healthGradient;

    [SerializeField] private float lastDamageReceived;

    [SerializeField] private bool hasRegen = false;
    [SerializeField] private float regenRate = 5f;

    public void StartRun()
    {
        healthBar.minValue = 0;
        healthBar.maxValue = maxHealth;

        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        healthBar.value = currentHealth;
        fillImage.color = healthGradient.Evaluate(healthBar.normalizedValue);
    }

    public float TakeDamage(float damage)
    {
        currentHealth -= damage;
        UpdateHealthBar();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        lastDamageReceived = Time.time;

        return currentHealth;
    }

    void Die()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        // For testing purposes, take damage when the space key is pressed
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TakeDamage(10f);
        }

        if (hasRegen && currentHealth < maxHealth && Time.time - lastDamageReceived > 5f)
        {
            currentHealth += regenRate * Time.deltaTime;
            if(currentHealth > maxHealth)
                currentHealth = maxHealth;
            UpdateHealthBar();
        }
    }
}
