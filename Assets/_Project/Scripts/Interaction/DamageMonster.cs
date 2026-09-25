// DamageMonster
// An enemy that keeps hurting anything that can be damaged while it stays in
// contact, once every Damage Interval.
//
// Put this on: the monster GameObject. Add a Collider2D first (for example a
//   CircleCollider2D) and tick "Is Trigger" on it.
// Assign in Inspector: Damage Amount, Damage Interval.

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageMonster : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("How much damage to deal each time.")]
    [SerializeField] private int damageAmount = 10;

    [Tooltip("How many seconds between hits while touching the target.")]
    [SerializeField] private float damageInterval = 1f;

    // Counts down to the next hit while a target is inside.
    private float timeUntilNextHit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        timeUntilNextHit = 0f; // Hit right away on first contact.
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Tick(other.gameObject);
    }

    // Counts down and deals damage when the timer runs out.
    private void Tick(GameObject target)
    {
        timeUntilNextHit -= Time.deltaTime;
        if (timeUntilNextHit > 0f)
        {
            return;
        }

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damageAmount);
        }
        timeUntilNextHit = damageInterval;
    }
}
