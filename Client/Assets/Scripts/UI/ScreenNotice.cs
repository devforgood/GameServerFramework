using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 가운데 위쪽에 잠깐 떴다 사라지는 한 줄 안내.
///
/// 서버가 요청을 거절하는 경우(레벨 미달로 게이트를 못 지나감 등)에 왜 안 되는지 보여 줄
/// 자리가 없었다. 로그에만 남으면 플레이어에게는 그냥 고장 난 것으로 보인다.
///
/// 창처럼 코드로 만든다 — 씬마다 두면 게이트로 씬이 바뀔 때마다 같은 것을 맞춰 둬야 한다.
/// </summary>
public class ScreenNotice : MonoBehaviour
{
    // 한 번에 화면에 남기는 줄 수. 넘치면 오래된 것부터 지운다.
    private const int MaxLines = 3;
    private const float DefaultSeconds = 3f;

    private static ScreenNotice instance;

    private RectTransform root;
    private readonly List<GameObject> lines = new List<GameObject>();
    private static TMP_FontAsset cachedFont;

    /// <summary>한 줄 띄운다. 어디서든 부를 수 있다(필요할 때 스스로 만들어진다).</summary>
    public static void Show(string message, float seconds = DefaultSeconds)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (instance == null)
        {
            var go = new GameObject("ScreenNotice");
            instance = go.AddComponent<ScreenNotice>();
            DontDestroyOnLoad(go);
        }

        instance.Push(message, seconds);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Build();
    }

    private void Push(string message, float seconds)
    {
        if (lines.Count >= MaxLines)
        {
            Destroy(lines[0]);
            lines.RemoveAt(0);
        }

        var obj = new GameObject("Line", typeof(RectTransform));
        obj.transform.SetParent(root, false);

        var background = obj.AddComponent<Image>();
        background.color = new Color(0.06f, 0.07f, 0.10f, 0.85f);

        obj.AddComponent<LayoutElement>().minHeight = 40f;

        var text = new GameObject("Text", typeof(RectTransform));
        text.transform.SetParent(obj.transform, false);

        var label = text.AddComponent<TextMeshProUGUI>();
        ApplyFont(label);
        label.text = message;
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.86f, 0.5f);

        var rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = new Vector2(-16f, 0f);

        lines.Add(obj);
        StartCoroutine(RemoveAfter(obj, seconds));
    }

    private System.Collections.IEnumerator RemoveAfter(GameObject line, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (line == null)
            yield break;

        lines.Remove(line);
        Destroy(line);
    }

    private void Build()
    {
        var canvasObj = new GameObject("NoticeCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // 대화 창(100)보다 위에 둔다. 대화 중에도 안내는 보여야 한다.
        canvas.sortingOrder = 200;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 안내는 누를 것이 없으므로 레이캐스터를 붙이지 않는다(뒤쪽 입력을 가리지 않는다).

        var holder = new GameObject("Lines", typeof(RectTransform));
        holder.transform.SetParent(canvasObj.transform, false);
        root = holder.GetComponent<RectTransform>();

        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -80f);
        root.sizeDelta = new Vector2(900f, 0f);

        var layout = holder.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        var fitter = holder.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>
    /// 폰트를 지정하지 않으면 TextMeshPro 가 기본 폰트를 못 찾아 예외를 던지는 환경이 있다
    /// (DialogWindow / AdvancedDamageText 가 같은 이유로 같은 처리를 한다).
    /// </summary>
    private static void ApplyFont(TextMeshProUGUI text)
    {
        if (text.font != null)
            return;

        if (cachedFont == null)
        {
            cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (cachedFont == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts.Length > 0)
                    cachedFont = fonts[0];
            }
        }

        if (cachedFont != null)
        {
            text.font = cachedFont;
            text.fontSharedMaterial = cachedFont.material;
        }
    }
}
