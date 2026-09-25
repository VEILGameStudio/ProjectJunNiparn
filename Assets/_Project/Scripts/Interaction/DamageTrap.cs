// DamageTrap
// A hazard (spikes, fire, thorns) that hurts anything that can be damaged the
// moment it touches the trap. Traps are not interactables: they react to the
// player's body touching them, not to clicks.
//
// Put this on: the trap GameObject. Add a Collider2D first (for example a
//   BoxCollider2D) and tick "Is Trigger" on it.
// Assign in Inspector: Damage Amount.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageTrap : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("How much damage to deal when something touches the trap.")]
    [SerializeField] private int damageAmount = 20;

    // Called when something walks into the trap.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    // Damages the object if it can be hurt.
    private void TryDamage(GameObject target)
    {
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
        }
    }
}
