using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 20f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
        {
            Debug.Log(
                $"เก็บไม่ได้ - HP เต็มแล้ว: " +
                $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}"
            );

            return;
        }

        playerHealth.Heal(healAmount);

        Destroy(gameObject);
    }
}