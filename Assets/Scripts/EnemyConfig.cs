using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "The Senior Defender/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Stats")]
    [Min(1)] public int maxHP = 20;
    [Min(0f)] public float moveSpeed = 3f;
    [Min(0)] public int damageToServer = 10;
    [Min(0)] public int damageToPlayer = 10;
    [Min(0.05f)] public float damageInterval = 1f;
    [Min(0)] public int dataPackValue = 5;
    [Min(0)] public int expReward = 10;

    [Header("Death")]
    [Min(0f)] public float deathDelay = 0.6f;
    public GameObject onDeathEffectPrefab;
}
