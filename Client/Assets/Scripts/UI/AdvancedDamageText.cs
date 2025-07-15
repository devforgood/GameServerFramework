using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class AdvancedDamageText : DamageText
{
    [Header("Advanced Animation Settings")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    
    [Header("Color Settings")]
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color criticalColor = Color.yellow;
    public Color poisonColor = new Color(0.5f, 0f, 0.5f); // Purple
    public Color fireColor = new Color(1f, 0.5f, 0f); // Orange
    
    [Header("Text Effects")]
    public bool enableTextShake = true;
    public bool enableColorPulse = true;
    public float pulseSpeed = 2f;
    
    private Color originalColor;
    private Vector3 originalPosition;
    

    
    public void ShowAdvancedDamage(int damage, bool isCritical = false, bool isHeal = false, DamageType damageType = DamageType.Normal)
    {
        // Set text and color
        damageText.text = damage.ToString();
        
        // Set color based on damage type
        switch (damageType)
        {
            case DamageType.Heal:
                damageText.color = healColor;
                damageText.text = "+" + damage.ToString();
                break;
            case DamageType.Critical:
                damageText.color = criticalColor;
                damageText.text = damage.ToString() + "!";
                break;
            case DamageType.Poison:
                damageText.color = poisonColor;
                damageText.text = damage.ToString() + "P";
                break;
            case DamageType.Fire:
                damageText.color = fireColor;
                damageText.text = damage.ToString() + "F";
                break;
            default:
                damageText.color = damageColor;
                break;
        }
        
        originalColor = damageText.color;
        originalPosition = transform.localPosition;
        
        // Call parent's ShowDamage method to set up basic animation
        ShowDamage(damage, isCritical, isHeal);
        
        // Start advanced animation
        StartCoroutine(AnimateAdvancedDamage());
    }
    
    private IEnumerator AnimateAdvancedDamage()
    {
        // Wait for parent animation to start
        yield return new WaitForSeconds(fadeInDuration * 0.5f);
        
        // Add special effects based on damage type
        if (damageText.text.Contains("!")) // Critical hit
        {
            yield return StartCoroutine(AnimateBounce());
            if (enableTextShake)
                yield return StartCoroutine(AnimateShake());
        }
        
        // Color pulse effect for critical hits
        if (enableColorPulse && damageText.text.Contains("!"))
        {
            StartCoroutine(AnimateColorPulse());
        }
        
        // Wait for parent animation to complete
        yield return new WaitForSeconds(animationDuration * 0.5f);
    }
    
    private IEnumerator AnimateBounce()
    {
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < bounceDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / bounceDuration;
            
            // Create bounce effect using sine wave
            float bounce = Mathf.Sin(progress * Mathf.PI * bounceCount) * bounceStrength;
            Vector3 bounceScale = originalScale * (1f + bounce);
            
            transform.localScale = bounceScale;
            yield return null;
        }
        
        // Return to original scale
        transform.localScale = originalScale;
    }
    
    private IEnumerator AnimateShake()
    {
        float elapsedTime = 0f;
        Vector3 originalPos = transform.localPosition;
        
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / shakeDuration;
            
            // Create shake effect
            Vector3 shakeOffset = new Vector3(
                Mathf.Sin(Time.time * 50f) * shakeIntensity * (1f - progress),
                Mathf.Cos(Time.time * 30f) * shakeIntensity * (1f - progress),
                0f
            );
            
            transform.localPosition = originalPos + shakeOffset;
            yield return null;
        }
        
        transform.localPosition = originalPos;
    }
    
    private IEnumerator AnimateColorPulse()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float pulse = Mathf.Sin(elapsedTime * pulseSpeed) * 0.5f + 0.5f;
            
            damageText.color = Color.Lerp(originalColor, Color.white, pulse * 0.3f);
            yield return null;
        }
        
        damageText.color = originalColor;
    }
    
    // Advanced easing function
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    public static AdvancedDamageText CreateDamageText(Vector3 worldPosition, Transform parent = null)
    {
        // Create canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DamageCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            
            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create damage text prefab
        GameObject damageTextObj = new GameObject("AdvancedDamageText");
        damageTextObj.transform.SetParent(canvas.transform);
        
        // Set world position
        damageTextObj.transform.position = worldPosition;
        
        // Add components
        AdvancedDamageText damageText = damageTextObj.AddComponent<AdvancedDamageText>();
        CanvasGroup canvasGroup = damageTextObj.AddComponent<CanvasGroup>();
        
        // Create text object
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(damageTextObj.transform);
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localScale = Vector3.one;
        
        // Add TextMeshPro component
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 24;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.text = "0";
        
        // Set references
        damageText.damageText = textComponent;
        damageText.canvasGroup = canvasGroup;
        
        return damageText;
    }
}

public enum DamageType
{
    Normal,
    Critical,
    Heal,
    Poison,
    Fire
} 