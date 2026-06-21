using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats (GDD v3.0)")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private int damage = 10;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.right * speed;

        Destroy(gameObject, 2f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        if (collision.TryGetComponent(out IDamageable target))
        {
            target.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
