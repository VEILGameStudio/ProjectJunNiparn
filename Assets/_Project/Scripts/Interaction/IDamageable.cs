// IDamageable
// The shared contract for anything that can be hurt. The Player implements this,
// and traps or monsters call TakeDamage() on whatever they touch without needing
// to know it is the player.
//
// Put this on: nothing directly. Scripts implement this interface, e.g.:
//   public class PlayerHealth : MonoBehaviour, IDamageable { ... }

public interface IDamageable
{
    // Reduces health by the given amount. Should never receive a negative number.
    void TakeDamage(int amount);
}
