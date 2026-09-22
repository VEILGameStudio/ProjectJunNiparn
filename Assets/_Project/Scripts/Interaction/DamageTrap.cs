// DamageTrap
// A hazard (spikes, fire, thorns) that hurts anything that can be damaged the
// moment it touches the trap. It works in both game modes: it reacts to 3D
// triggers (2.5D scenes) and 2D triggers (2D scenes), so use whichever collider
// matches your scene.
//
// Put this on: the trap GameObject. It needs a Collider (3D) OR Collider2D (2D)
//   with "Is Trigger" ticked.
// Assign in Inspector: Damage Amount.

using UnityEngine;

public class DamageTrap : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("How much damage to deal when something touches the trap.")]
    [SerializeField] private int damageAmount = 20;

    // Called when a 3D collider enters the trap (2.5D scenes).
    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other.gameObject);
    }

    // Called when a 2D collider enters the trap (2D scenes).
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
