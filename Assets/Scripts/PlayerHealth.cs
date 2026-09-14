using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;

    [SerializeField] private float startingHealth = 100f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    public bool IsDead => CurrentHealth <= 0f;

    private void Awake()
    {
        startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);

        CurrentHealth = startingHealth;

        Debug.Log($"Player HP: {CurrentHealth} / {MaxHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (IsDead)
            return;

        damage = Mathf.Max(0f, damage);

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        Debug.Log($"Player HP: {CurrentHealth} / {MaxHealth}");

        if (IsDead)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead)
            return;

        if (CurrentHealth >= MaxHealth)
        {
            Debug.Log(
                $"Player HP เต็มแล้ว: {CurrentHealth} / {MaxHealth}"
            );

            return;
        }

        amount = Mathf.Max(0f, amount);

        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);

        Debug.Log($"Player HP: {CurrentHealth} / {MaxHealth}");
    }

    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        CurrentHealth = Mathf.Clamp(
            CurrentHealth,
            0f,
            MaxHealth
        );

        Debug.Log($"Max HP: {MaxHealth}");
        Debug.Log($"Player HP: {CurrentHealth} / {MaxHealth}");
    }

    private void Die()
    {
        Debug.Log("Player Dead");
    }
}