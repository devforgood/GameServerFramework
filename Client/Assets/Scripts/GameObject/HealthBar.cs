using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private RectTransform fillRectTransform;
    private Image fillImage;

    public void Initialize()
    {
        // 캔버스 설정
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 캔버스 크기 설정
        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(150f, 20f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 1f);
        canvasRect.localPosition = new Vector3(0f, 2f, 0f);

        // 배경 생성
        GameObject background = new GameObject("Background");
        background.transform.SetParent(transform, false);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundRect.localPosition = Vector3.zero;

        // 체력바 생성
        GameObject healthBarFill = new GameObject("HealthBarFill");
        healthBarFill.transform.SetParent(background.transform, false);
        fillImage = healthBarFill.AddComponent<Image>();
        fillImage.color = Color.green;
        fillRectTransform = healthBarFill.GetComponent<RectTransform>();
        fillRectTransform.anchorMin = Vector2.zero;
        fillRectTransform.anchorMax = Vector2.one;
        fillRectTransform.offsetMin = Vector2.zero;
        fillRectTransform.offsetMax = Vector2.zero;
        fillRectTransform.localPosition = Vector3.zero;

        // Billboard 효과 추가
        gameObject.AddComponent<Billboard>();
    }

    public void UpdateHealth(float healthPercent)
    {
        if (fillRectTransform != null)
        {
            fillRectTransform.anchorMax = new Vector2(healthPercent, 1);
            
            if (healthPercent > 0.7f)
                fillImage.color = Color.green;
            else if (healthPercent > 0.3f)
                fillImage.color = Color.yellow;
            else
                fillImage.color = Color.red;
        }
    }
} 