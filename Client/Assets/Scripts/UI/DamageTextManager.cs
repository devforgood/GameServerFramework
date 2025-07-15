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
    private static bool isInitialized = false;
    
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
                    DontDestroyOnLoad(go);
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
            
            // Only initialize once
            if (!isInitialized)
            {
                InitializePools();
                isInitialized = true;
            }
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple DamageTextManager instances detected, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    private void InitializePools()
    {
        damageTextPools = new Dictionary<int, Queue<DamageText>>();
        activeTextsPerActor = new Dictionary<int, List<DamageText>>();
        
        // Pre-populate the pool with some damage texts
        int poolKey = 0;
        damageTextPools[poolKey] = new Queue<DamageText>();
        
        Debug.LogWarning($"=== DAMAGE EVENT === Initializing damage text pool with {initialPoolSize} items...");
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            // Create a dummy position for pool initialization
            Vector3 dummyPosition = Vector3.zero;
            DamageText damageText = DamageText.CreateDamageText(dummyPosition);
            if (damageText != null)
            {
                damageText.gameObject.SetActive(false);
                damageTextPools[poolKey].Enqueue(damageText);
            }
        }
        
        Debug.LogWarning($"=== DAMAGE EVENT === Initialized damage text pool with {damageTextPools[poolKey].Count} items");
    }
    
    public void ShowDamage(int actorId, Vector3 position, int damage, bool isCritical = false, bool isHeal = false)
    {
        // Clean up old texts for this actor
        CleanupOldTexts(actorId);
        
        // Get damage text from pool or create new one
        DamageText damageText = GetDamageTextFromPool();
        if (damageText == null)
        {
            Debug.LogWarning($"=== DAMAGE EVENT === Failed to get or create damage text for Actor {actorId}");
            return;
        }
        
        // Calculate display position
        Vector3 displayPosition = CalculateDisplayPosition(actorId, position);
        
        // Set position properly for World Space Canvas
        Canvas parentCanvas = damageText.GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            Vector3 localPosition = parentCanvas.transform.InverseTransformPoint(displayPosition);
            damageText.transform.localPosition = localPosition;
        }
        else
        {
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
            // Ensure the GameObject is active before starting coroutines
            damageText.gameObject.SetActive(true);
            
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
        // Start from the provided position (HealthBar position) and move up slightly
        Vector3 position = basePosition + Vector3.up * 0.3f;
        
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
            // Create new damage text if pool is empty or invalid
            Vector3 dummyPosition = Vector3.zero;
            damageText = DamageText.CreateDamageText(dummyPosition);
            if (damageText != null)
            {
                damageText.gameObject.SetActive(true);
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