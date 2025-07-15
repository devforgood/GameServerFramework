using UnityEngine;
using System.Collections.Generic;

public class DamageTextManager : MonoBehaviour
{
    [Header("Pool Settings")]
    public int initialPoolSize = 20;
    public int maxPoolSize = 100;
    
    [Header("Display Settings")]
    public float textSpacing = 0.5f;
    public float maxTextsPerActor = 3;
    
    private Dictionary<int, Queue<DamageText>> damageTextPools;
    private Dictionary<int, List<DamageText>> activeTextsPerActor;
    
    private static DamageTextManager instance;
    public static DamageTextManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DamageTextManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DamageTextManager");
                    instance = go.AddComponent<DamageTextManager>();
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePools()
    {
        damageTextPools = new Dictionary<int, Queue<DamageText>>();
        activeTextsPerActor = new Dictionary<int, List<DamageText>>();
    }
    
    public void ShowDamage(int actorId, Vector3 position, int damage, bool isCritical = false, bool isHeal = false)
    {
        // Clean up old texts for this actor
        CleanupOldTexts(actorId);
        
        // Get damage text from pool or create new one
        DamageText damageText = GetDamageTextFromPool();
        if (damageText != null)
        {
            // For screen space canvas, convert world position to screen position
            Canvas parentCanvas = damageText.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(position);
                RectTransform rectTransform = damageText.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
                }
            }
            else
            {
                // For world space canvas, use world position
                Vector3 displayPosition = CalculateDisplayPosition(actorId, position);
                damageText.transform.position = displayPosition;
            }
            
            try
            {
                damageText.ShowDamage(damage, isCritical, isHeal);
                
                // Track active text for this actor
                if (!activeTextsPerActor.ContainsKey(actorId))
                    activeTextsPerActor[actorId] = new List<DamageText>();
                
                activeTextsPerActor[actorId].Add(damageText);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error showing damage text: {e.Message}");
                // Return to pool if there was an error
                ReturnDamageTextToPool(damageText);
            }
        }
        else
        {
            Debug.LogWarning("Failed to create damage text from pool");
        }
    }
    
    public void ShowAdvancedDamage(int actorId, Vector3 position, int damage, bool isCritical = false, bool isHeal = false, DamageType damageType = DamageType.Normal)
    {
        // Clean up old texts for this actor
        CleanupOldTexts(actorId);
        
        // Calculate position with offset to avoid overlapping
        Vector3 displayPosition = CalculateDisplayPosition(actorId, position);
        
        // Create advanced damage text
        AdvancedDamageText damageText = AdvancedDamageText.CreateDamageText(displayPosition);
        if (damageText != null)
        {
            damageText.ShowAdvancedDamage(damage, isCritical, isHeal, damageType);
            
            // Track active text for this actor (convert to base type for compatibility)
            if (!activeTextsPerActor.ContainsKey(actorId))
                activeTextsPerActor[actorId] = new List<DamageText>();
            
            // Note: AdvancedDamageText inherits from DamageText, so this should work
            // If not, you might need to create a separate tracking system
        }
    }
    
    private Vector3 CalculateDisplayPosition(int actorId, Vector3 basePosition)
    {
        Vector3 position = basePosition + Vector3.up * 2.5f;
        
        // Add horizontal offset based on number of active texts
        if (activeTextsPerActor.ContainsKey(actorId))
        {
            int textCount = activeTextsPerActor[actorId].Count;
            float offset = textCount * textSpacing;
            position += Vector3.right * offset;
        }
        
        return position;
    }
    
    private void CleanupOldTexts(int actorId)
    {
        if (activeTextsPerActor.ContainsKey(actorId))
        {
            // Remove texts that are no longer active
            activeTextsPerActor[actorId].RemoveAll(text => text == null);
            
            // Limit number of texts per actor
            while (activeTextsPerActor[actorId].Count >= maxTextsPerActor)
            {
                var oldText = activeTextsPerActor[actorId][0];
                activeTextsPerActor[actorId].RemoveAt(0);
                if (oldText != null)
                {
                    ReturnDamageTextToPool(oldText);
                }
            }
        }
    }
    
    private DamageText GetDamageTextFromPool()
    {
        int poolKey = 0; // You can use different keys for different text types
        
        if (!damageTextPools.ContainsKey(poolKey))
            damageTextPools[poolKey] = new Queue<DamageText>();
        
        DamageText damageText = null;
        
        if (damageTextPools[poolKey].Count > 0)
        {
            damageText = damageTextPools[poolKey].Dequeue();
            if (damageText != null && damageText.gameObject != null)
            {
                damageText.gameObject.SetActive(true);
            }
            else
            {
                damageText = null; // Invalid object, create new one
            }
        }
        
        if (damageText == null)
        {
            // Create new damage text using screen space for better visibility
            try
            {
                Vector3 tempPosition = Vector3.zero;
                damageText = DamageText.CreateScreenSpaceDamageText(tempPosition);
                if (damageText != null)
                {
                    damageText.gameObject.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating damage text: {e.Message}");
                return null;
            }
        }
        
        return damageText;
    }
    
    public void ReturnDamageTextToPool(DamageText damageText)
    {
        if (damageText == null) return;
        
        int poolKey = 0;
        
        if (!damageTextPools.ContainsKey(poolKey))
            damageTextPools[poolKey] = new Queue<DamageText>();
        
        // Limit pool size
        if (damageTextPools[poolKey].Count < maxPoolSize)
        {
            damageText.gameObject.SetActive(false);
            damageTextPools[poolKey].Enqueue(damageText);
        }
        else
        {
            Destroy(damageText.gameObject);
        }
    }
    
    public void ClearAllTexts()
    {
        foreach (var actorTexts in activeTextsPerActor.Values)
        {
            foreach (var text in actorTexts)
            {
                if (text != null)
                {
                    ReturnDamageTextToPool(text);
                }
            }
        }
        activeTextsPerActor.Clear();
    }
} 