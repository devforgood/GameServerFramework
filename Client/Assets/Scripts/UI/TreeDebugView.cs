using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TreeDebugView : MonoBehaviour
{
    private const float PanelWidth = 560f;
    private const float PanelHeight = 620f;

    private static TreeDebugView instance;

    public static TreeDebugView Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<TreeDebugView>();
                if (instance == null)
                {
                    var obj = new GameObject("TreeDebugView");
                    instance = obj.AddComponent<TreeDebugView>();
                }
            }
            return instance;
        }
    }

    private readonly Dictionary<long, TreeState> trees = new Dictionary<long, TreeState>();
    private readonly Dictionary<ushort, NodeRow> rows = new Dictionary<ushort, NodeRow>();
    private readonly List<long> monsterOrder = new List<long>();

    private Canvas canvas;
    private GameObject panel;
    private Dropdown monsterDropdown;
    private Text titleText;
    private Text metaText;
    private RectTransform contentRoot;
    private Font font;
    private long selectedMonsterId = -1;
    private bool suppressDropdownEvent;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUi();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8) && panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    public void Apply(syncnet.TreeDebugSync sync)
    {
        EnsureUi();

        for (int i = 0; i < sync.DefinitionsLength; ++i)
        {
            var definition = sync.Definitions(i);
            if (!definition.HasValue)
                continue;

            ApplyDefinition(definition.Value);
        }

        for (int i = 0; i < sync.FramesLength; ++i)
        {
            var frame = sync.Frames(i);
            if (!frame.HasValue)
                continue;

            ApplyFrame(frame.Value);
        }

        RefreshDropdown();
        RefreshTree();
    }

    private void ApplyDefinition(syncnet.TreeDebugDefinition definition)
    {
        TreeState tree;
        if (!trees.TryGetValue(definition.MonsterId, out tree))
        {
            tree = new TreeState();
            tree.MonsterId = definition.MonsterId;
            trees.Add(tree.MonsterId, tree);
            monsterOrder.Add(tree.MonsterId);
        }

        tree.TreeId = definition.TreeId ?? string.Empty;
        tree.Nodes.Clear();
        tree.Roots.Clear();

        for (int i = 0; i < definition.NodesLength; ++i)
        {
            var node = definition.Nodes(i);
            if (!node.HasValue)
                continue;

            var value = node.Value;
            var nodeState = new NodeState();
            nodeState.NodeId = value.NodeId;
            nodeState.ParentNodeId = value.ParentNodeId;
            nodeState.Name = value.Name ?? string.Empty;
            nodeState.NodeType = value.NodeType;
            tree.Nodes[nodeState.NodeId] = nodeState;
        }

        foreach (var pair in tree.Nodes)
        {
            var node = pair.Value;
            NodeState parent;
            if (node.ParentNodeId >= 0 && tree.Nodes.TryGetValue((ushort)node.ParentNodeId, out parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                tree.Roots.Add(node);
            }
        }

        SortNodes(tree.Roots);

        if (selectedMonsterId < 0)
            selectedMonsterId = tree.MonsterId;
    }

    private void ApplyFrame(syncnet.TreeDebugRuntimeFrame frame)
    {
        TreeState tree;
        if (!trees.TryGetValue(frame.MonsterId, out tree))
        {
            tree = new TreeState();
            tree.MonsterId = frame.MonsterId;
            tree.TreeId = frame.TreeId ?? string.Empty;
            trees.Add(tree.MonsterId, tree);
            monsterOrder.Add(tree.MonsterId);
        }

        tree.TreeId = frame.TreeId ?? tree.TreeId;
        tree.Tick = frame.Tick;
        tree.AiState = frame.AiState;
        tree.TargetAgentId = frame.TargetAgentId;
        tree.ExecutedPath.Clear();

        for (int i = 0; i < frame.ExecutedPathLength; ++i)
        {
            tree.ExecutedPath.Add(frame.ExecutedPath(i));
        }

        for (int i = 0; i < frame.ChangesLength; ++i)
        {
            var change = frame.Changes(i);
            if (!change.HasValue)
                continue;

            var value = change.Value;
            NodeState node;
            if (!tree.Nodes.TryGetValue(value.NodeId, out node))
            {
                node = new NodeState();
                node.NodeId = value.NodeId;
                node.Name = value.Name ?? string.Empty;
                tree.Nodes[node.NodeId] = node;
                tree.Roots.Add(node);
            }

            node.Name = string.IsNullOrEmpty(value.Name) ? node.Name : value.Name;
            node.Status = value.Status;
            node.Reason = value.Reason ?? string.Empty;
            node.SuccessCount = value.SuccessCount;
            node.FailureCount = value.FailureCount;
            node.RunningCount = value.RunningCount;
            node.LastSeenTick = frame.Tick;
        }

        if (selectedMonsterId < 0)
            selectedMonsterId = tree.MonsterId;
    }

    private void RefreshDropdown()
    {
        suppressDropdownEvent = true;
        monsterDropdown.ClearOptions();

        var options = new List<string>();
        int selectedIndex = 0;
        for (int i = 0; i < monsterOrder.Count; ++i)
        {
            long monsterId = monsterOrder[i];
            options.Add("Monster " + monsterId);
            if (monsterId == selectedMonsterId)
                selectedIndex = i;
        }

        monsterDropdown.AddOptions(options);
        monsterDropdown.value = selectedIndex;
        monsterDropdown.RefreshShownValue();
        suppressDropdownEvent = false;
    }

    private void RefreshTree()
    {
        TreeState tree;
        if (!trees.TryGetValue(selectedMonsterId, out tree))
            return;

        titleText.text = string.IsNullOrEmpty(tree.TreeId) ? "Behavior Tree" : tree.TreeId;
        metaText.text = "monster=" + tree.MonsterId +
                        "  tick=" + tree.Tick +
                        "  state=" + tree.AiState +
                        "  target=" + tree.TargetAgentId;

        ClearRows();
        foreach (var root in tree.Roots)
        {
            AddNodeRow(tree, root, 0);
        }
    }

    private void AddNodeRow(TreeState tree, NodeState node, int depth)
    {
        var row = CreateNodeRow(node, depth);
        rows[node.NodeId] = row;
        UpdateRow(row, tree, node);

        SortNodes(node.Children);
        foreach (var child in node.Children)
        {
            AddNodeRow(tree, child, depth + 1);
        }
    }

    private void UpdateRow(NodeRow row, TreeState tree, NodeState node)
    {
        bool executed = tree.ExecutedPath.Contains(node.NodeId);
        row.NameText.text = BuildNodeText(node, executed);
        row.StatusText.text = node.Status.ToString();
        row.ReasonText.text = node.Reason;
        row.Background.color = GetStatusColor(node.Status, executed);
    }

    private string BuildNodeText(NodeState node, bool executed)
    {
        var builder = new StringBuilder();
        builder.Append(executed ? "> " : "  ");
        builder.Append(node.Name);
        builder.Append(" #");
        builder.Append(node.NodeId);
        builder.Append(" ");
        builder.Append(node.NodeType);
        builder.Append("  S:");
        builder.Append(node.SuccessCount);
        builder.Append(" F:");
        builder.Append(node.FailureCount);
        builder.Append(" R:");
        builder.Append(node.RunningCount);
        return builder.ToString();
    }

    private Color GetStatusColor(syncnet.TreeNodeStatus status, bool executed)
    {
        Color color;
        switch (status)
        {
            case syncnet.TreeNodeStatus.Success:
                color = new Color(0.10f, 0.34f, 0.18f, 0.92f);
                break;
            case syncnet.TreeNodeStatus.Failure:
                color = new Color(0.42f, 0.12f, 0.12f, 0.92f);
                break;
            case syncnet.TreeNodeStatus.Running:
                color = new Color(0.14f, 0.28f, 0.48f, 0.92f);
                break;
            case syncnet.TreeNodeStatus.Skipped:
                color = new Color(0.28f, 0.28f, 0.30f, 0.92f);
                break;
            default:
                color = new Color(0.16f, 0.16f, 0.18f, 0.92f);
                break;
        }

        if (executed)
        {
            color.r = Mathf.Min(1f, color.r + 0.12f);
            color.g = Mathf.Min(1f, color.g + 0.12f);
            color.b = Mathf.Min(1f, color.b + 0.12f);
        }

        return color;
    }

    private void BuildUi()
    {
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        EnsureEventSystem();

        var canvasObj = new GameObject("TreeDebugCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        panel = CreateUiObject("Panel", canvasObj.transform);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.07f, 0.08f, 0.88f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 12);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var header = CreateUiObject("Header", panel.transform);
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 8;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandHeight = true;
        AddLayoutElement(header, -1, 34);

        titleText = CreateText("Title", header.transform, "Behavior Tree", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(titleText.gameObject, 260, 34);

        monsterDropdown = CreateDropdown(header.transform);
        AddLayoutElement(monsterDropdown.gameObject, 220, 34);
        monsterDropdown.onValueChanged.AddListener(OnMonsterSelected);

        metaText = CreateText("Meta", panel.transform, "waiting for tree debug frames", 13, FontStyle.Normal, TextAnchor.MiddleLeft);
        metaText.color = new Color(0.76f, 0.80f, 0.84f, 1f);
        AddLayoutElement(metaText.gameObject, -1, 24);

        var scrollObj = CreateUiObject("Scroll View", panel.transform);
        AddLayoutElement(scrollObj, -1, PanelHeight - 104);
        var scrollImage = scrollObj.AddComponent<Image>();
        scrollImage.color = new Color(0.03f, 0.035f, 0.04f, 0.95f);
        var scrollRect = scrollObj.AddComponent<ScrollRect>();

        var viewport = CreateUiObject("Viewport", scrollObj.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8, 8);
        viewportRect.offsetMax = new Vector2(-8, -8);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        var viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;

        var content = CreateUiObject("Content", viewport.transform);
        contentRoot = content.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0f, 0f);
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 4;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        panel.SetActive(true);
    }

    private NodeRow CreateNodeRow(NodeState node, int depth)
    {
        var rowObj = CreateUiObject("Node " + node.NodeId, contentRoot);
        AddLayoutElement(rowObj, -1, 30);
        var image = rowObj.AddComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.18f, 0.92f);

        var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 3, 3);
        layout.spacing = 8;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;

        var indent = CreateUiObject("Indent", rowObj.transform);
        AddLayoutElement(indent, depth * 18, 24);

        var nameText = CreateText("Name", rowObj.transform, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
        AddLayoutElement(nameText.gameObject, 270, 24);

        var statusText = CreateText("Status", rowObj.transform, string.Empty, 12, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddLayoutElement(statusText.gameObject, 78, 24);

        var reasonText = CreateText("Reason", rowObj.transform, string.Empty, 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        reasonText.color = new Color(0.82f, 0.85f, 0.88f, 1f);
        AddLayoutElement(reasonText.gameObject, 150, 24);

        var row = new NodeRow();
        row.Background = image;
        row.NameText = nameText;
        row.StatusText = statusText;
        row.ReasonText = reasonText;
        return row;
    }

    private Dropdown CreateDropdown(Transform parent)
    {
        var obj = CreateUiObject("MonsterDropdown", parent);
        var image = obj.AddComponent<Image>();
        image.color = new Color(0.12f, 0.13f, 0.15f, 1f);

        var dropdown = obj.AddComponent<Dropdown>();
        dropdown.targetGraphic = image;
        var label = CreateText("Label", obj.transform, string.Empty, 13, FontStyle.Normal, TextAnchor.MiddleLeft);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 0);
        labelRect.offsetMax = new Vector2(-24, 0);
        dropdown.captionText = label;

        var arrow = CreateText("Arrow", obj.transform, "v", 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        var arrowRect = arrow.rectTransform;
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.pivot = new Vector2(1, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 0);
        arrowRect.anchoredPosition = new Vector2(-4, 0);

        var template = CreateDropdownTemplate(obj.transform);
        dropdown.template = template;
        dropdown.itemText = template.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent)
    {
        var templateObj = CreateUiObject("Template", parent);
        templateObj.SetActive(false);
        var templateRect = templateObj.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, -2);
        templateRect.sizeDelta = new Vector2(0, 140);
        templateObj.AddComponent<Image>().color = new Color(0.09f, 0.10f, 0.12f, 1f);
        var scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        var viewport = CreateUiObject("Viewport", templateObj.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.clear;

        var content = CreateUiObject("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = Vector2.zero;
        var contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var item = CreateUiObject("Item", content.transform);
        AddLayoutElement(item, -1, 28);
        var toggle = item.AddComponent<Toggle>();
        var itemBackground = item.AddComponent<Image>();
        itemBackground.color = new Color(0.12f, 0.13f, 0.15f, 1f);
        toggle.targetGraphic = itemBackground;

        var checkmark = CreateText("Item Checkmark", item.transform, ">", 12, FontStyle.Bold, TextAnchor.MiddleCenter);
        var checkRect = checkmark.rectTransform;
        checkRect.anchorMin = new Vector2(0, 0);
        checkRect.anchorMax = new Vector2(0, 1);
        checkRect.sizeDelta = new Vector2(22, 0);
        checkRect.anchoredPosition = new Vector2(11, 0);
        toggle.graphic = checkmark;

        var itemLabel = CreateText("Item Label", item.transform, "Monster", 13, FontStyle.Normal, TextAnchor.MiddleLeft);
        var itemLabelRect = itemLabel.rectTransform;
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(28, 0);
        itemLabelRect.offsetMax = new Vector2(-8, 0);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        return templateRect;
    }

    private Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor alignment)
    {
        var obj = CreateUiObject(name, parent);
        var label = obj.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private GameObject CreateUiObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    private void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight)
    {
        var element = obj.GetComponent<LayoutElement>();
        if (element == null)
            element = obj.AddComponent<LayoutElement>();

        if (preferredWidth >= 0)
            element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
    }

    private void ClearRows()
    {
        rows.Clear();
        for (int i = contentRoot.childCount - 1; i >= 0; --i)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    private void SortNodes(List<NodeState> nodes)
    {
        nodes.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        for (int i = 0; i < nodes.Count; ++i)
        {
            SortNodes(nodes[i].Children);
        }
    }

    private void OnMonsterSelected(int index)
    {
        if (suppressDropdownEvent || index < 0 || index >= monsterOrder.Count)
            return;

        selectedMonsterId = monsterOrder[index];
        RefreshTree();
    }

    private void EnsureUi()
    {
        if (canvas == null)
            BuildUi();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        var obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(obj);
    }

    private class TreeState
    {
        public string TreeId = string.Empty;
        public long MonsterId = -1;
        public ulong Tick;
        public syncnet.AIState AiState = syncnet.AIState.Patrol;
        public long TargetAgentId = -1;
        public readonly Dictionary<ushort, NodeState> Nodes = new Dictionary<ushort, NodeState>();
        public readonly List<NodeState> Roots = new List<NodeState>();
        public readonly List<ushort> ExecutedPath = new List<ushort>();
    }

    private class NodeState
    {
        public ushort NodeId;
        public int ParentNodeId = -1;
        public string Name = string.Empty;
        public syncnet.TreeNodeType NodeType = syncnet.TreeNodeType.Action;
        public syncnet.TreeNodeStatus Status = syncnet.TreeNodeStatus.Idle;
        public string Reason = string.Empty;
        public uint SuccessCount;
        public uint FailureCount;
        public uint RunningCount;
        public ulong LastSeenTick;
        public readonly List<NodeState> Children = new List<NodeState>();
    }

    private class NodeRow
    {
        public Image Background;
        public Text NameText;
        public Text StatusText;
        public Text ReasonText;
    }
}
