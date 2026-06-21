/// <summary>
/// Standard contract for anything that can receive damage (GDD v3.0 - Combat System).
/// Implemented by ServerCore, PlayerHealth and EnemyBehavior so that damage dealers
/// (Code Bullets, enemy contact) never depend on concrete entity types - they just call
/// TakeDamage on an IDamageable. Keeps the combat layer fully decoupled and modular.
/// </summary>
public interface IDamageable
{
    void TakeDamage(int amount);
}
