using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private FloatingDamageText damageTextPrefab;
    [SerializeField] private GameObject genericDeathParticlePrefab;

    private void OnEnable()
    {
        GameEvents.OnDamageTaken += SpawnDamageText;
        GameEvents.OnEnemyDied += SpawnDeathParticle;
    }

    private void OnDisable()
    {
        GameEvents.OnDamageTaken -= SpawnDamageText;
        GameEvents.OnEnemyDied -= SpawnDeathParticle;
    }

    private void SpawnDamageText(Vector3 position, int damage)
    {
        if (damageTextPrefab == null) return;

        FloatingDamageText textInstance = Instantiate(damageTextPrefab, position, Quaternion.identity);
        textInstance.Setup(damage);
    }

    private void SpawnDeathParticle(Vector3 position)
    {
        // Notice: EnemyBehavior might also spawn its own death effect. 
        // This is a generic/fallback death particle system.
        if (genericDeathParticlePrefab != null)
        {
            // If you use an object pool, you can use PooledEffectPool.Spawn instead
            Instantiate(genericDeathParticlePrefab, position, Quaternion.identity);
        }
    }
}
