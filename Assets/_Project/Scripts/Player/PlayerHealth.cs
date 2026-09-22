// PlayerHealth
// Keeps the player's health and shows it on a UI bar. Traps and monsters hurt the
// player by calling TakeDamage (through the IDamageable interface). When health
// reaches 0, it runs the On Death event. Later the GameOverManager will listen to
// this to show the game over screen; for now wire whatever you like in On Death.
//
// Put this on: the Player GameObject.
// Assign in Inspector:
//   - Max Health: the starting and highest health.
//   - Health Bar (optional): the UIStatBar that shows health.
//   - On Death: what happens when health hits 0.

using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("The starting and maximum health.")]
    [SerializeField] private int maxHealth = 100;

    [Header("UI")]
    [Tooltip("The bar that shows current health. Optional.")]
    [SerializeField] private UIStatBar healthBar;

    [Header("Events")]
    [Tooltip("Runs once when health reaches 0. The GameOverManager will use this later.")]
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
        UpdateBar();
    }

    // Lowers health by the amount (from IDamageable). Ignores negative numbers.
    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateBar();

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
        UpdateBar();
    }

    // Runs the death event once, then starts the shared game over sequence.
    private void Die()
    {
        isDead = true;
        onDeath?.Invoke();

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver();
        }
    }

    // Updates the health bar to match the current health.
    private void UpdateBar()
    {
        if (healthBar != null)
        {
            healthBar.SetFill((float)currentHealth / maxHealth);
        }
    }
}
