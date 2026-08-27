using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 대화 창. 서버가 보낸 노드를 그리고, 고른 번호를 되돌려 준다.
///
/// 화면은 코드로 만든다(프리팹 없음). 대화는 씬마다 있어야 하는데 게이트로 씬이 바뀌므로,
/// 씬에 배치해 두면 네 개 씬에 같은 것을 만들어 두고 계속 맞춰야 한다.
/// DamageTextManager 와 같은 방식이다 — 처음 필요할 때 만들고 씬 전환에도 유지한다.
///
/// 이 창은 조건을 판정하지 않는다. 서버가 조건에 맞는 선택지만 보내 주므로,
/// 클라는 <b>받은 순서대로 그리고 고른 번호를 돌려보내면 된다</b>.
/// </summary>
public class DialogWindow : MonoBehaviour
{
    private static DialogWindow instance;

    /// <summary>이미 만들어진 창(없으면 null). 종료 중에 새로 만들지 않으려고 둔다.</summary>
    public static DialogWindow Existing => instance;

    public static DialogWindow Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("DialogWindow");
                instance = go.AddComponent<DialogWindow>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // 고른 번호를 돌려줄 곳. 음수는 "창을 닫았다"는 뜻이다.
    private Action<int> onChoice;

    private Canvas canvas;
    private GameObject panel;
    private TextMeshProUGUI speakerText;
    private TextMeshProUGUI bodyText;
    private RectTransform choiceRoot;
    private readonly List<GameObject> choiceButtons = new List<GameObject>();

    private static TMP_FontAsset cachedFont;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Build();
        panel.SetActive(false);
    }

    void Update()
    {
        // 창이 떠 있는 동안 ESC 는 "대화 닫기"다. 서버에도 알려야 다음 상호작용이
        // 지난 노드로 돌아오지 않는다.
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Choose(-1);
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    /// <summary>
    /// 노드 하나를 그린다. choiceLabels 는 서버가 보낸 순서 그대로여야 한다.
    /// </summary>
    public void Show(string speaker, string body, IList<string> choiceLabels, Action<int> onChoiceSelected)
    {
        onChoice = onChoiceSelected;

        speakerText.text = speaker ?? string.Empty;
        bodyText.text = body ?? string.Empty;

        ClearChoices();
        for (int i = 0; i < choiceLabels.Count; ++i)
            AddChoice(i, choiceLabels[i]);

        panel.SetActive(true);
    }

    public void Hide()
    {
        onChoice = null;
        ClearChoices();
        if (panel != null)
            panel.SetActive(false);
    }

    private void Choose(int index)
    {
        // 콜백이 창을 다시 열 수 있으므로 먼저 꺼내 두고 부른다.
        var callback = onChoice;
        onChoice = null;
        if (callback != null)
            callback(index);
    }

    private void ClearChoices()
    {
        foreach (var button in choiceButtons)
            Destroy(button);
        choiceButtons.Clear();
    }

    private void AddChoice(int index, string label)
    {
        var buttonObj = new GameObject($"Choice{index}", typeof(RectTransform));
        buttonObj.transform.SetParent(choiceRoot, false);

        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.16f, 0.17f, 0.22f, 0.95f);

        var layout = buttonObj.AddComponent<LayoutElement>();
        layout.minHeight = 40f;

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;

        int captured = index;   // 람다가 루프 변수를 잡지 않게 복사한다
        button.onClick.AddListener(() => Choose(captured));

        var textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(buttonObj.transform, false);

        var text = textObj.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.margin = new Vector4(12f, 0f, 12f, 0f);

        var textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        choiceButtons.Add(buttonObj);
    }

    private void Build()
    {
        var canvasObj = new GameObject("DialogCanvas", typeof(RectTransform));
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;   // 데미지 텍스트 위에 뜬다

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.10f, 0.92f);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 40f);
        panelRect.sizeDelta = new Vector2(1000f, 380f);

        var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(24, 24, 20, 20);
        panelLayout.spacing = 10f;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandWidth = true;

        speakerText = CreateText(panel.transform, "Speaker", 26, FontStyles.Bold,
            new Color(0.98f, 0.85f, 0.45f));
        bodyText = CreateText(panel.transform, "Body", 24, FontStyles.Normal, Color.white);
        bodyText.gameObject.AddComponent<LayoutElement>().minHeight = 90f;

        var choiceObj = new GameObject("Choices", typeof(RectTransform));
        choiceObj.transform.SetParent(panel.transform, false);
        choiceRoot = choiceObj.GetComponent<RectTransform>();

        var choiceLayout = choiceObj.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 6f;
        choiceLayout.childControlHeight = true;
        choiceLayout.childForceExpandHeight = false;
        choiceLayout.childControlWidth = true;
        choiceLayout.childForceExpandWidth = true;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, float size,
        FontStyles style, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var text = obj.AddComponent<TextMeshProUGUI>();
        ApplyFont(text);
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    /// <summary>
    /// 폰트를 지정하지 않으면 TextMeshPro 가 기본 폰트를 못 찾아 예외를 던지는 환경이 있다
    /// (AdvancedDamageText 가 같은 이유로 같은 처리를 한다).
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
