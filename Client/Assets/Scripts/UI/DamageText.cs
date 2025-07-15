using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI damageText;
    public CanvasGroup canvasGroup;
    
    [Header("Animation Settings")]
    public float animationDuration = 1.5f;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.5f;
    public float moveDistance = 2f;
    public float scaleMultiplier = 1.5f;
    public float bounceStrength = 0.3f;
    public int bounceCount = 2;
    public float bounceDuration = 0.3f;
    
    [Header("Color Settings")]
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color criticalColor = Color.yellow;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 startScale;
    private Vector3 targetScale;
    private bool isCritical;
    private Camera mainCamera;
    
    void Awake()
    {
        // Initialize components if not assigned
        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>();
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();
    }
    
    void Update()
    {
        // Make text always face the camera (only for world space canvas)
        if (mainCamera != null && GetComponentInParent<Canvas>()?.renderMode == RenderMode.WorldSpace)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0); // Flip the text to face the camera properly
        }
    }
    
    public void ShowDamage(int damage, bool isCritical = false, bool isHeal = false)
    {
        // Null check and initialization
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TextMeshProUGUI>();
            if (damageText == null)
            {
                Debug.LogError("DamageText: TextMeshProUGUI component not found!");
                return;
            }
        }
        
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Set text and color
        damageText.text = damage.ToString();
        this.isCritical = isCritical;
        
        if (isHeal)
        {
            damageText.color = healColor;
            damageText.text = "+" + damage.ToString();
        }
        else if (isCritical)
        {
            damageText.color = criticalColor;
            damageText.text = damage.ToString() + "!";
        }
        else
        {
            damageText.color = damageColor;
        }
        
        // Set initial position and scale
        startPosition = transform.localPosition;
        targetPosition = startPosition + Vector3.up * moveDistance;
        
        startScale = Vector3.one;
        targetScale = Vector3.one * scaleMultiplier;
        
        // Reset alpha and scale
        canvasGroup.alpha = 0f;
        transform.localScale = startScale;
        
        // Start animation
        StartCoroutine(AnimateDamage());
    }
    
    private IEnumerator AnimateDamage()
    {
        float elapsedTime = 0f;
        
        // Fade in with ease out
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;
            float easeProgress = EaseOutQuad(progress);
            
            canvasGroup.alpha = easeProgress;
            transform.localScale = Vector3.Lerp(startScale, targetScale, easeProgress);
            yield return null;
        }
        
        // Hold at peak
        canvasGroup.alpha = 1f;
        transform.localScale = targetScale;
        
        // Add bounce effect for critical hits
        if (isCritical)
        {
            yield return StartCoroutine(AnimateBounce());
        }
        
        yield return new WaitForSeconds(animationDuration * 0.3f);
        
        // Fade out and move up with ease in
        elapsedTime = 0f;
        Vector3 currentPosition = transform.localPosition;
        Vector3 currentScale = transform.localScale;
        
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;
            float easeProgress = EaseInQuad(progress);
            
            canvasGroup.alpha = 1f - easeProgress;
            transform.localPosition = Vector3.Lerp(currentPosition, targetPosition, easeProgress);
            transform.localScale = Vector3.Lerp(currentScale, startScale, easeProgress);
            
            yield return null;
        }
        
        // Return to pool or destroy
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.ReturnDamageTextToPool(this);
        }
        else
        {
            Destroy(gameObject);
        }
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
    
    // Easing functions for smooth animations
    private float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }
    
    private float EaseInQuad(float t)
    {
        return t * t;
    }
    
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
    }
    
    public static DamageText CreateDamageText(Vector3 worldPosition, Transform parent = null)
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
            scaler.dynamicPixelsPerUnit = 10f; // Adjust for better text rendering
            
            // Add GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Set canvas size
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100, 100);
        }
        
        // Create damage text prefab
        GameObject damageTextObj = new GameObject("DamageText");
        damageTextObj.transform.SetParent(canvas.transform);
        
        // Set world position
        damageTextObj.transform.position = worldPosition;
        
        // Create text object first
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
        textComponent.color = Color.white;
        textComponent.enableAutoSizing = false;
        textComponent.fontSharedMaterial = null; // Use default material
        
        // Set RectTransform for proper sizing
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 50);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add components
        DamageText damageText = damageTextObj.AddComponent<DamageText>();
        CanvasGroup canvasGroup = damageTextObj.AddComponent<CanvasGroup>();
        
        // Set references immediately
        damageText.damageText = textComponent;
        damageText.canvasGroup = canvasGroup;
        
        // Force Awake to be called to ensure proper initialization
        damageText.Awake();
        
        return damageText;
    }
    
    public static DamageText CreateScreenSpaceDamageText(Vector3 worldPosition)
    {
        // Create screen space canvas if it doesn't exist
        Canvas screenCanvas = FindObjectOfType<Canvas>();
        if (screenCanvas == null || screenCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            GameObject canvasObj = new GameObject("ScreenDamageCanvas");
            screenCanvas = canvasObj.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            screenCanvas.sortingOrder = 100; // Ensure it renders on top
            
            // Add CanvasScaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Convert world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        // Create damage text prefab
        GameObject damageTextObj = new GameObject("ScreenDamageText");
        damageTextObj.transform.SetParent(screenCanvas.transform);
        
        // Set screen position
        RectTransform damageTextRect = damageTextObj.AddComponent<RectTransform>();
        damageTextRect.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
        damageTextRect.sizeDelta = new Vector2(200, 50);
        damageTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        damageTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        damageTextRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Create text object
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(damageTextObj.transform);
        
        // Add TextMeshPro component
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 24;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.text = "0";
        textComponent.color = Color.white;
        textComponent.enableAutoSizing = false;
        
        // Set RectTransform for proper sizing
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(200, 50);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add components
        DamageText damageText = damageTextObj.AddComponent<DamageText>();
        CanvasGroup canvasGroup = damageTextObj.AddComponent<CanvasGroup>();
        
        // Set references immediately
        damageText.damageText = textComponent;
        damageText.canvasGroup = canvasGroup;
        
        // Force Awake to be called to ensure proper initialization
        damageText.Awake();
        
        return damageText;
    }
} 