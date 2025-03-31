using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Moviment i Desaparició")]
    public float floatSpeed = 1f;
    public float fadeDuration = 1f;
    public Vector3 floatDirection = Vector3.up;

    [Header("Referència explícita al TextFeedback")]
    public TMP_Text textFeedback;  // Referència directa al TextMeshPro que vols controlar.

    private Color originalColor;
    private float elapsedTime = 0f;

    private void Awake()
    {
        if (textFeedback == null)
        {
            textFeedback = GetComponentInChildren<TMP_Text>();
            if (textFeedback == null)
            {
                Debug.LogError("❗ No s'ha pogut trobar cap TMP_Text dins de FloatingText!");
                enabled = false; // ✳️ Desactivem aquest script per evitar més errors
                return;
            }
        }

        originalColor = textFeedback.color;
    }

    private void Update()
    {
        // Mou el text cap amunt (usant Time.unscaledDeltaTime per funcionar en pausa)
        transform.position += floatDirection * floatSpeed * Time.unscaledDeltaTime;

        elapsedTime += Time.unscaledDeltaTime;
        float fadeAmount = Mathf.Clamp01(1f - (elapsedTime / fadeDuration));

        if (textFeedback != null)
        {
            Color fadedColor = originalColor;
            fadedColor.a = fadeAmount;

            // ✅ Assignació directa al vertex color
            textFeedback.color = fadedColor;
        }

        if (elapsedTime >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }

    public void SetupText(string newText, Color newVertexColor)
    {
        if (textFeedback != null)
        {
            textFeedback.text = newText;

            // ✅ Assegurem que el color s’aplica realment
            textFeedback.color = newVertexColor;

            // Si vols forçar el material per seguretat:
            if (textFeedback.fontMaterial.HasProperty(ShaderUtilities.ID_FaceColor))
            {
                textFeedback.fontMaterial.SetColor(ShaderUtilities.ID_FaceColor, newVertexColor);
            }

            originalColor = newVertexColor;
        }
    }

}
