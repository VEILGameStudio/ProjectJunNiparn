// PlayerHealth
// Keeps the player's health. Traps and monsters hurt the player by calling
// TakeDamage (through the IDamageable interface). Every change raises
// GameEvents.OnHealthChanged, so the health bar updates itself. When health
// reaches 0 it runs the On Death event and raises GameEvents.OnGameOver, which
// starts the shared game over sequence.
//
// Put this on: the Player GameObject.
// Assign in Inspector:
//   - Max Health: the starting and highest health.
//   - On Death (optional): extra things to do when health hits 0 (e.g. a death animation).

using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("The starting and maximum health.")]
    [SerializeField] private int maxHealth = 100;

    [Header("Events")]
    [Tooltip("Runs once when health reaches 0, just before the game over sequence starts.")]
    [SerializeField] private UnityEvent onDeath;

    private int currentHealth;
    private bool isDead;

    // The player's current health.
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }

    // Lowers health by the amount (from IDamageable). Ignores negative numbers.
    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    // Raises health by the amount, up to the maximum.
    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        GameEvents.RaiseHealthChanged(currentHealth, maxHealth);
    }

    // Runs the death event once, then starts the shared game over sequence.
    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();
        GameEvents.RaiseGameOver();
    }
}
