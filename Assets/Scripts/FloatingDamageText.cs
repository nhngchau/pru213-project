using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private Vector3 randomizeOffset = new Vector3(0.5f, 0.5f, 0);

    public void Setup(int damageAmount)
    {
        if (textComponent == null)
            textComponent = GetComponentInChildren<TMP_Text>();

        if (textComponent != null)
        {
            textComponent.text = damageAmount.ToString();
        }

        // Randomize starting position slightly so overlapping texts are readable
        transform.position += new Vector3(
            Random.Range(-randomizeOffset.x, randomizeOffset.x),
            Random.Range(-randomizeOffset.y, randomizeOffset.y),
            0
        );

        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        float timer = 0;
        Color startColor = textComponent != null ? textComponent.color : Color.white;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            float percent = timer / lifeTime;

            // Float upwards
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;

            // Fade out
            if (textComponent != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, percent);
                textComponent.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
