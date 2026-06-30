using UnityEngine;

/// <summary>
/// Moves a custom target/crosshair sprite to the mouse position.
/// Attach this script to a GameObject that has a SpriteRenderer.
/// </summary>
public class TargetCursor : MonoBehaviour
{
    [Header("Cursor Visual")]
    [Tooltip("The SpriteRenderer that draws the target/crosshair cursor.")]
    [SerializeField] private SpriteRenderer cursorSprite;

    [Tooltip("Color used when the cursor is not over an enemy.")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("Color used when the cursor is over an object tagged Enemy.")]
    [SerializeField] private Color enemyHoverColor = Color.red;

    [Header("Cursor Scale")]
    [Tooltip("If true, this script controls the cursor size. If false, use the Transform Scale in the Inspector.")]
    [SerializeField] private bool useScriptScale = true;

    [Tooltip("Cursor size when not hovering over an enemy.")]
    [SerializeField] private float normalScale = 0.03f;

    [Tooltip("Cursor size while hovering over an enemy.")]
    [SerializeField] private float enemyScale = 0.05f;

    private Camera mainCamera;

    private void Awake()
    {
        // Cache the main camera so we can convert mouse screen position to world position.
        mainCamera = Camera.main;

        // Beginner-friendly fallback: if nothing was assigned in the Inspector,
        // try to use the SpriteRenderer on this same GameObject.
        if (cursorSprite == null)
        {
            cursorSprite = GetComponent<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        // Hide the default operating-system cursor while this custom cursor is active.
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        // Show the default cursor again when this component is disabled.
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        // Safety fallback in case the cursor object is destroyed while the scene changes.
        Cursor.visible = true;
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        FollowMouse();
        UpdateHoverVisual();
    }

    private void FollowMouse()
    {
        // Input.mousePosition is in screen pixels, so convert it to a 2D world position.
        Vector3 mouseScreenPosition = Input.mousePosition;
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // Keep the cursor object's original Z position so it stays on the same 2D plane.
        transform.position = new Vector3(mouseWorldPosition.x, mouseWorldPosition.y, transform.position.z);
    }

    private void UpdateHoverVisual()
    {
        bool isHoveringEnemy = IsMouseOverEnemy();

        if (cursorSprite != null)
        {
            cursorSprite.color = isHoveringEnemy ? enemyHoverColor : normalColor;
        }

        // Only change Transform Scale when this option is enabled.
        // Turn it off if you want to control the cursor size manually in the Inspector.
        if (useScriptScale)
        {
            float targetScale = isHoveringEnemy ? enemyScale : normalScale;
            transform.localScale = Vector3.one * targetScale;
        }
    }

    private bool IsMouseOverEnemy()
    {
        // Check the 2D collider directly under the cursor's world position.
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);

        // Enemy objects in this project should use the "Enemy" tag.
        return hitCollider != null && hitCollider.CompareTag("Enemy");
    }
}
