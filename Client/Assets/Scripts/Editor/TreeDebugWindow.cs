using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 서버 BTDebugManager 의 TreeDebugSync 를 언리얼 비헤이비어 트리 에디터 스타일로 보여주는 창.
// Tools > BT Debug Viewer 로 연다. 플레이 모드에서 서버에 접속하면 데이터가 들어온다.
// - 툴바 드롭다운 또는 씬/하이어라키에서 Monster 선택 시 해당 몬스터의 트리 표시
// - 마우스 휠: 줌, 드래그: 패닝, Fit: 전체 트리 맞춤
// - 상태 변경 프레임이 도착할 때만 다시 그려진다 (TreeDebugRepository.Changed)
public class TreeDebugWindow : EditorWindow
{
    private const float NodeWidth = 180f;
    private const float NodeHeight = 74f;
    private const float NodeHeaderHeight = 20f;
    private const float HGap = 26f;
    private const float VGap = 48f;
    private const float GraphMargin = 30f;

    private static readonly Color BackgroundColor = new Color(0.13f, 0.14f, 0.15f, 1f);
    private static readonly Color ExecutedColor = new Color(1f, 0.55f, 0.10f, 1f);
    private static readonly Color IdleBorderColor = new Color(0.05f, 0.06f, 0.07f, 1f);
    private static readonly Color IdleEdgeColor = new Color(0.40f, 0.42f, 0.46f, 1f);

    private long selectedMonsterId = -1;
    private bool followSceneSelection = true;

    // 레이아웃을 마지막으로 계산했을 때의 대상/구조 버전. 달라졌을 때만 다시 계산한다.
    private long builtMonsterId = -1;
    private int builtStructureVersion = -1;
    private readonly Dictionary<ushort, Vector2> nodePositions = new Dictionary<ushort, Vector2>();
    private Vector2 graphSize;

    private float zoom = 1f;
    private Vector2 pan = Vector2.zero;
    private bool fitRequested = true;

    private GUIStyle nameStyle;
    private GUIStyle infoStyle;
    private GUIStyle badgeStyle;

    [MenuItem("Tools/BT Debug Viewer")]
    public static void Open()
    {
        var window = GetWindow<TreeDebugWindow>("BT Debug");
        window.minSize = new Vector2(480f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        TreeDebugRepository.Changed += OnRepositoryChanged;
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        TreeDebugRepository.Changed -= OnRepositoryChanged;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    private void OnRepositoryChanged()
    {
        Repaint();
    }

    private void OnSelectionChanged()
    {
        if (!followSceneSelection || Selection.activeGameObject == null)
            return;

        var monster = Selection.activeGameObject.GetComponentInParent<Monster>();
        if (monster == null || monster.agnet_id == selectedMonsterId)
            return;

        selectedMonsterId = monster.agnet_id;
        // 아직 정의를 받지 못한 몬스터라면 서버에 트리 정의를 요청한다
        TreeDebugRepository.RequestDefinition(selectedMonsterId);
        Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawToolbar();

        var monsterIds = TreeDebugRepository.MonsterIds;
        if (selectedMonsterId < 0 && monsterIds.Count > 0)
            selectedMonsterId = monsterIds[0];

        var tree = TreeDebugRepository.GetTree(selectedMonsterId);

        DrawMetaLine(tree);

        var graphArea = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(graphArea, BackgroundColor);

        if (tree == null)
        {
            var message = Application.isPlaying
                ? "트리 데이터를 기다리는 중입니다. 서버에 접속하면 몬스터의 BT가 표시됩니다."
                : "플레이 모드에서 서버에 접속하면 몬스터의 BT가 표시됩니다.";
            var messageRect = new Rect(graphArea.x + 12f, graphArea.y + 12f, graphArea.width - 24f, 40f);
            EditorGUI.HelpBox(messageRect, message, MessageType.Info);
            return;
        }

        EnsureLayout(tree);
        HandleGraphInput(graphArea);

        if (fitRequested)
        {
            FitToArea(graphArea);
            fitRequested = false;
        }

        GUI.BeginClip(graphArea);
        var clipRect = new Rect(0f, 0f, graphArea.width, graphArea.height);
        DrawEdges(tree);
        DrawNodes(tree, clipRect);
        GUI.EndClip();

        DrawLegend(graphArea);
    }

    #region 툴바/헤더

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        var monsterIds = TreeDebugRepository.MonsterIds;
        if (monsterIds.Count > 0)
        {
            var labels = new string[monsterIds.Count];
            int selectedIndex = 0;
            for (int i = 0; i < monsterIds.Count; ++i)
            {
                labels[i] = "Monster " + monsterIds[i];
                if (monsterIds[i] == selectedMonsterId)
                    selectedIndex = i;
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, labels, EditorStyles.toolbarPopup, GUILayout.Width(150f));
            if (newIndex != selectedIndex || selectedMonsterId < 0)
            {
                selectedMonsterId = monsterIds[newIndex];
                TreeDebugRepository.RequestDefinition(selectedMonsterId);
            }
        }
        else
        {
            GUILayout.Label("(no monsters)", EditorStyles.miniLabel, GUILayout.Width(150f));
        }

        followSceneSelection = GUILayout.Toggle(followSceneSelection,
            new GUIContent("Follow Selection", "씬/하이어라키에서 Monster 를 선택하면 그 몬스터의 트리로 전환"),
            EditorStyles.toolbarButton, GUILayout.Width(110f));

        if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(40f)))
            fitRequested = true;

        GUILayout.FlexibleSpace();
        GUILayout.Label("zoom " + zoom.ToString("0.00") + "x", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
    }

    private void DrawMetaLine(TreeDebugRepository.TreeState tree)
    {
        var meta = new StringBuilder();
        if (tree != null)
        {
            meta.Append(string.IsNullOrEmpty(tree.TreeId) ? "behavior tree" : tree.TreeId);
            meta.Append("   monster=").Append(tree.MonsterId);
            meta.Append("   tick=").Append(tree.Tick);
            meta.Append("   state=").Append(tree.AiState);
            meta.Append("   target=").Append(tree.TargetAgentId);
        }
        else
        {
            meta.Append("no tree selected");
        }

        GUILayout.Label(meta.ToString(), EditorStyles.miniLabel);
    }

    #endregion

    #region 레이아웃

    private void EnsureLayout(TreeDebugRepository.TreeState tree)
    {
        if (builtMonsterId == tree.MonsterId && builtStructureVersion == tree.StructureVersion)
            return;

        nodePositions.Clear();
        float cursorX = 0f;
        int maxDepth = 0;

        foreach (var root in tree.Roots)
        {
            PlaceSubtree(root, ref cursorX, 0, ref maxDepth);
        }

        graphSize = new Vector2(
            Mathf.Max(NodeWidth, cursorX - HGap) + GraphMargin * 2f,
            (maxDepth + 1) * NodeHeight + maxDepth * VGap + GraphMargin * 2f);

        bool monsterChanged = builtMonsterId != tree.MonsterId;
        builtMonsterId = tree.MonsterId;
        builtStructureVersion = tree.StructureVersion;

        if (monsterChanged)
            fitRequested = true;
    }

    /// <summary>서브트리 폭 기반으로 노드 좌표(그래프 공간: x→오른쪽, y→아래)를 계산한다.</summary>
    private float PlaceSubtree(TreeDebugRepository.NodeState node, ref float cursorX, int depth, ref int maxDepth)
    {
        if (depth > maxDepth)
            maxDepth = depth;

        float y = depth * (NodeHeight + VGap);

        if (node.Children.Count == 0)
        {
            float leafX = cursorX;
            cursorX += NodeWidth + HGap;
            nodePositions[node.NodeId] = new Vector2(leafX, y);
            return leafX;
        }

        float firstX = 0f;
        float lastX = 0f;
        for (int i = 0; i < node.Children.Count; ++i)
        {
            float childX = PlaceSubtree(node.Children[i], ref cursorX, depth + 1, ref maxDepth);
            if (i == 0)
                firstX = childX;
            lastX = childX;
        }

        float x = (firstX + lastX) * 0.5f;
        nodePositions[node.NodeId] = new Vector2(x, y);
        return x;
    }

    #endregion

    #region 그래프 그리기

    private Rect NodeRect(ushort nodeId)
    {
        var pos = nodePositions[nodeId];
        return new Rect(
            pan.x + (pos.x + GraphMargin) * zoom,
            pan.y + (pos.y + GraphMargin) * zoom,
            NodeWidth * zoom,
            NodeHeight * zoom);
    }

    private void DrawEdges(TreeDebugRepository.TreeState tree)
    {
        float thickness = Mathf.Max(1.5f, 2f * zoom);

        foreach (var pair in tree.Nodes)
        {
            var parent = pair.Value;
            if (!nodePositions.ContainsKey(parent.NodeId))
                continue;

            var parentRect = NodeRect(parent.NodeId);
            foreach (var child in parent.Children)
            {
                if (!nodePositions.ContainsKey(child.NodeId))
                    continue;

                var childRect = NodeRect(child.NodeId);
                bool executed = tree.ExecutedPath.Contains(child.NodeId);
                var color = executed ? ExecutedColor : IdleEdgeColor;

                // 부모 하단 중앙 → 자식 상단 중앙을 ㄱ자(엘보) 선 3개로 잇는다
                float px = parentRect.x + parentRect.width * 0.5f;
                float py = parentRect.yMax;
                float cx = childRect.x + childRect.width * 0.5f;
                float cy = childRect.y;
                float midY = (py + cy) * 0.5f;

                EditorGUI.DrawRect(new Rect(px - thickness * 0.5f, py, thickness, midY - py), color);
                EditorGUI.DrawRect(new Rect(Mathf.Min(px, cx) - thickness * 0.5f, midY - thickness * 0.5f,
                    Mathf.Abs(cx - px) + thickness, thickness), color);
                EditorGUI.DrawRect(new Rect(cx - thickness * 0.5f, midY, thickness, cy - midY), color);
            }
        }
    }

    private void DrawNodes(TreeDebugRepository.TreeState tree, Rect clipRect)
    {
        foreach (var pair in tree.Nodes)
        {
            var node = pair.Value;
            if (!nodePositions.ContainsKey(node.NodeId))
                continue;

            var rect = NodeRect(node.NodeId);
            if (!rect.Overlaps(clipRect))
                continue;

            int executedIndex = tree.ExecutedPath.IndexOf(node.NodeId);
            bool executed = executedIndex >= 0;
            float border = Mathf.Max(1f, 2f * zoom);

            EditorGUI.DrawRect(rect, executed ? ExecutedColor : IdleBorderColor);

            var bodyRect = new Rect(rect.x + border, rect.y + border,
                rect.width - border * 2f, rect.height - border * 2f);
            EditorGUI.DrawRect(bodyRect, GetStatusColor(node.Status, executed));

            var headerRect = new Rect(bodyRect.x, bodyRect.y, bodyRect.width, NodeHeaderHeight * zoom);
            EditorGUI.DrawRect(headerRect, GetTypeColor(node.NodeType));

            // 너무 축소되면 글자는 생략하고 색 블록만 보여준다
            if (zoom > 0.45f)
            {
                nameStyle.fontSize = Mathf.RoundToInt(12f * zoom);
                infoStyle.fontSize = Mathf.RoundToInt(10f * zoom);

                GUI.Label(Inset(headerRect, 5f * zoom, 0f), node.Name, nameStyle);

                float lineHeight = 15f * zoom;
                var lineRect = new Rect(bodyRect.x + 5f * zoom, headerRect.yMax + 1f * zoom,
                    bodyRect.width - 10f * zoom, lineHeight);
                GUI.Label(lineRect, "#" + node.NodeId + "  " + node.NodeType + "  ·  " + node.Status, infoStyle);

                lineRect.y += lineHeight;
                GUI.Label(lineRect, "S:" + node.SuccessCount + "  F:" + node.FailureCount + "  R:" + node.RunningCount, infoStyle);

                if (!string.IsNullOrEmpty(node.Reason))
                {
                    lineRect.y += lineHeight;
                    GUI.Label(lineRect, node.Reason, infoStyle);
                }
            }

            // 언리얼처럼 이번 틱의 실행 순서를 우상단 배지로 표시
            if (executed)
            {
                float badgeSize = 18f * zoom;
                var badgeRect = new Rect(rect.xMax - badgeSize * 0.6f, rect.y - badgeSize * 0.4f, badgeSize, badgeSize);
                EditorGUI.DrawRect(badgeRect, ExecutedColor);
                if (zoom > 0.45f)
                {
                    badgeStyle.fontSize = Mathf.RoundToInt(11f * zoom);
                    GUI.Label(badgeRect, (executedIndex + 1).ToString(), badgeStyle);
                }
            }
        }
    }

    private void DrawLegend(Rect graphArea)
    {
        float y = graphArea.yMax - 22f;
        float x = graphArea.x + 8f;

        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Running, true), "Running");
        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Success, true), "Success");
        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Failure, true), "Failure");
        DrawLegendItem(ref x, y, ExecutedColor, "Executed path");

        var hint = new GUIContent("wheel: zoom   drag: pan");
        var hintSize = EditorStyles.miniLabel.CalcSize(hint);
        GUI.Label(new Rect(graphArea.xMax - hintSize.x - 8f, y, hintSize.x, 16f), hint, EditorStyles.miniLabel);
    }

    private void DrawLegendItem(ref float x, float y, Color color, string label)
    {
        EditorGUI.DrawRect(new Rect(x, y + 2f, 12f, 12f), color);
        x += 16f;
        var content = new GUIContent(label);
        var size = EditorStyles.miniLabel.CalcSize(content);
        GUI.Label(new Rect(x, y, size.x, 16f), content, EditorStyles.miniLabel);
        x += size.x + 12f;
    }

    #endregion

    #region 입력(줌/패닝)

    private void HandleGraphInput(Rect area)
    {
        var e = Event.current;
        if (!area.Contains(e.mousePosition))
            return;

        if (e.type == EventType.ScrollWheel)
        {
            // 마우스 위치를 기준으로 줌 (그래프 좌표 고정점 유지)
            var local = e.mousePosition - area.position;
            var graphPoint = (local - pan) / zoom;
            zoom = Mathf.Clamp(zoom * (1f - e.delta.y * 0.04f), 0.25f, 2f);
            pan = local - graphPoint * zoom;
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2))
        {
            pan += e.delta;
            e.Use();
            Repaint();
        }
    }

    private void FitToArea(Rect area)
    {
        if (graphSize.x <= 0f || graphSize.y <= 0f)
            return;

        zoom = Mathf.Clamp(Mathf.Min(area.width / graphSize.x, (area.height - 26f) / graphSize.y), 0.25f, 1f);
        pan = new Vector2((area.width - graphSize.x * zoom) * 0.5f, 4f);
    }

    #endregion

    #region 스타일/색상

    private void EnsureStyles()
    {
        if (nameStyle != null)
            return;

        nameStyle = new GUIStyle(EditorStyles.boldLabel);
        nameStyle.normal.textColor = Color.white;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.clipping = TextClipping.Clip;

        infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.normal.textColor = new Color(0.85f, 0.88f, 0.91f, 1f);
        infoStyle.alignment = TextAnchor.MiddleLeft;
        infoStyle.clipping = TextClipping.Clip;

        badgeStyle = new GUIStyle(EditorStyles.boldLabel);
        badgeStyle.normal.textColor = Color.black;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.clipping = TextClipping.Clip;
    }

    private Rect Inset(Rect rect, float horizontal, float vertical)
    {
        return new Rect(rect.x + horizontal, rect.y + vertical,
            rect.width - horizontal * 2f, rect.height - vertical * 2f);
    }

    private Color GetStatusColor(syncnet.TreeNodeStatus status, bool executed)
    {
        Color color;
        switch (status)
        {
            case syncnet.TreeNodeStatus.Success:
                color = new Color(0.12f, 0.38f, 0.20f, 1f);
                break;
            case syncnet.TreeNodeStatus.Failure:
                color = new Color(0.45f, 0.13f, 0.13f, 1f);
                break;
            case syncnet.TreeNodeStatus.Running:
                color = new Color(0.15f, 0.32f, 0.55f, 1f);
                break;
            case syncnet.TreeNodeStatus.Skipped:
                color = new Color(0.28f, 0.28f, 0.30f, 1f);
                break;
            default:
                color = new Color(0.17f, 0.18f, 0.20f, 1f);
                break;
        }

        if (!executed)
        {
            color.r *= 0.62f;
            color.g *= 0.62f;
            color.b *= 0.62f;
        }

        return color;
    }

    private Color GetTypeColor(syncnet.TreeNodeType type)
    {
        switch (type)
        {
            case syncnet.TreeNodeType.Control:
                return new Color(0.30f, 0.33f, 0.38f, 1f);
            case syncnet.TreeNodeType.Condition:
                return new Color(0.13f, 0.40f, 0.50f, 1f);
            default:
                return new Color(0.42f, 0.26f, 0.55f, 1f);
        }
    }

    #endregion
}
