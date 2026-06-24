using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISlowable
{
    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;

    private float baseMoveSpeed;
    private readonly Dictionary<object, float> speedModifiers = new Dictionary<object, float>();

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        anim.SetFloat("Speed", movement.sqrMagnitude);

        if (movement != Vector2.zero)
        {
            anim.SetFloat("Horizontal", movement.x);
            anim.SetFloat("Vertical", movement.y);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    public void AddSpeedModifier(object source, float multiplier)
    {
        if (source == null)
        {
            return;
        }

        speedModifiers[source] = Mathf.Clamp01(multiplier);
        RecalculateSpeed();
    }

    public void RemoveSpeedModifier(object source)
    {
        if (source != null && speedModifiers.Remove(source))
        {
            RecalculateSpeed();
        }
    }

    private void RecalculateSpeed()
    {
        float multiplier = 1f;
        foreach (float m in speedModifiers.Values)
        {
            multiplier = Mathf.Min(multiplier, m);
        }

        moveSpeed = baseMoveSpeed * multiplier;
    }
}
