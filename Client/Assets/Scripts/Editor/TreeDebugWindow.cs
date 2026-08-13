using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 서버 BTDebugManager 의 TreeDebugSync 를 언리얼 비헤이비어 트리 에디터 스타일로 보여주는 창.
// Tools > BT Debug Viewer 로 연다. 플레이 모드에서 서버에 접속하면 데이터가 들어온다.
// - 툴바 드롭다운 또는 씬/하이어라키에서 Monster 선택 시 해당 몬스터의 트리 표시
// - 마우스 휠: 줌, 드래그: 패닝, F 키/Fit 버튼: 전체 트리 맞춤
// - 노드 클릭: 선택 → 하단 상세 패널에 카운트/사유 표시
// - 실행 경로는 오렌지 와이어 + 흐름 점 애니메이션, Running 노드는 펄스 글로우로 강조
public class TreeDebugWindow : EditorWindow
{
    private const float NodeWidth = 190f;
    private const float NodeHeight = 78f;
    private const float NodeHeaderHeight = 24f;
    private const float StatusStripHeight = 4f;
    private const float HGap = 28f;
    private const float VGap = 56f;
    private const float GraphMargin = 40f;
    private const float DetailsPanelHeight = 56f;
    private const float MinorGridSpacing = 16f;
    private const float MajorGridSpacing = 128f;

    private static readonly Color BackgroundColor = new Color(0.115f, 0.12f, 0.13f, 1f);
    private static readonly Color MinorGridColor = new Color(0f, 0f, 0f, 0.18f);
    private static readonly Color MajorGridColor = new Color(0f, 0f, 0f, 0.38f);
    private static readonly Color NodeBodyColor = new Color(0.145f, 0.15f, 0.165f, 1f);
    private static readonly Color NodeBodyExecutedColor = new Color(0.185f, 0.19f, 0.21f, 1f);
    private static readonly Color NodeOutlineColor = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color NodeShadowColor = new Color(0f, 0f, 0f, 0.35f);
    private static readonly Color ExecutedColor = new Color(1f, 0.58f, 0.10f, 1f);
    private static readonly Color ExecutedGlowColor = new Color(1f, 0.58f, 0.10f, 0.28f);
    private static readonly Color SelectedColor = new Color(1f, 0.8f, 0.25f, 1f);
    private static readonly Color HoverColor = new Color(1f, 1f, 1f, 0.30f);
    private static readonly Color IdleWireColor = new Color(0.46f, 0.48f, 0.53f, 0.85f);
    private static readonly Color FlowDotColor = new Color(1f, 0.78f, 0.35f, 1f);
    private static readonly Color PanelColor = new Color(0.155f, 0.16f, 0.175f, 1f);

    private long selectedMonsterId = -1;
    private bool followSceneSelection = true;
    private int selectedNodeId = -1;
    private int hoveredNodeId = -1;

    // 레이아웃을 마지막으로 계산했을 때의 대상/구조 버전. 달라졌을 때만 다시 계산한다.
    private long builtMonsterId = -1;
    private int builtStructureVersion = -1;
    private readonly Dictionary<ushort, Vector2> nodePositions = new Dictionary<ushort, Vector2>();
    private Vector2 graphSize;

    private float zoom = 1f;
    private Vector2 pan = Vector2.zero;
    private bool fitRequested = true;
    private double lastAnimationRepaintTime;

    private struct WireInfo
    {
        public Vector2 From;
        public Vector2 To;
    }

    private readonly List<WireInfo> executedWires = new List<WireInfo>();

    private GUIStyle nameStyle;
    private GUIStyle infoStyle;
    private GUIStyle statusStyle;
    private GUIStyle badgeStyle;
    private GUIStyle glyphStyle;
    private GUIStyle nodeIdStyle;
    private GUIStyle watermarkStyle;
    private GUIStyle watermarkSubStyle;
    private GUIStyle panelTitleStyle;
    private GUIStyle panelReasonStyle;

    [MenuItem("Tools/BT Debug Viewer")]
    public static void Open()
    {
        var window = GetWindow<TreeDebugWindow>("BT Debug");
        window.minSize = new Vector2(480f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        wantsMouseMove = true;
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

    // 실행 경로 흐름 점/Running 펄스 애니메이션용. 플레이 중에만 약 20fps 로 다시 그린다.
    private void Update()
    {
        if (!Application.isPlaying || selectedMonsterId < 0)
            return;

        if (EditorApplication.timeSinceStartup - lastAnimationRepaintTime < 0.05)
            return;

        lastAnimationRepaintTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    private void OnSelectionChanged()
    {
        if (!followSceneSelection || Selection.activeGameObject == null)
            return;

        var monster = Selection.activeGameObject.GetComponentInParent<Monster>();
        if (monster == null || monster.actor_id == selectedMonsterId)
            return;

        selectedMonsterId = monster.actor_id;
        // 아직 정의를 받지 못한 몬스터라면 서버에 트리 정의를 요청한다
        TreeDebugRepository.RequestDefinition(selectedMonsterId);
        Repaint();
    }

    private void OnGUI()
    {
        EnsureStyles();

        var monsterIds = TreeDebugRepository.MonsterIds;
        if (selectedMonsterId < 0 && monsterIds.Count > 0)
            selectedMonsterId = monsterIds[0];

        var tree = TreeDebugRepository.GetTree(selectedMonsterId);

        DrawToolbar(tree);

        var graphArea = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        var panelRect = GUILayoutUtility.GetRect(0f, DetailsPanelHeight, GUILayout.ExpandWidth(true));

        EditorGUI.DrawRect(graphArea, BackgroundColor);

        if (tree == null)
        {
            GUI.BeginClip(graphArea);
            DrawGrid(new Rect(0f, 0f, graphArea.width, graphArea.height));
            GUI.EndClip();

            var message = Application.isPlaying
                ? "트리 데이터를 기다리는 중입니다. 서버에 접속하면 몬스터의 BT가 표시됩니다."
                : "플레이 모드에서 서버에 접속하면 몬스터의 BT가 표시됩니다.";
            var messageRect = new Rect(graphArea.x + 12f, graphArea.y + 12f, graphArea.width - 24f, 40f);
            EditorGUI.HelpBox(messageRect, message, MessageType.Info);
            DrawDetailsPanel(panelRect, null);
            return;
        }

        EnsureLayout(tree);
        HandleGraphInput(graphArea, tree);

        if (fitRequested)
        {
            FitToArea(graphArea);
            fitRequested = false;
        }

        GUI.BeginClip(graphArea);
        var clipRect = new Rect(0f, 0f, graphArea.width, graphArea.height);
        DrawGrid(clipRect);
        DrawWatermark(clipRect, tree);
        DrawEdges(tree);
        DrawNodes(tree, clipRect);
        DrawLegend(clipRect);
        GUI.EndClip();

        DrawDetailsPanel(panelRect, tree);
    }

    #region 툴바/헤더

    private void DrawToolbar(TreeDebugRepository.TreeState tree)
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
                selectedNodeId = -1;
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

        if (tree != null)
        {
            var meta = new StringBuilder();
            meta.Append("tick ").Append(tree.Tick);
            meta.Append("  ·  ").Append(tree.AiState);
            if (tree.TargetActorId >= 0)
                meta.Append("  ·  target ").Append(tree.TargetActorId);
            GUILayout.Label(meta.ToString(), EditorStyles.miniLabel);
            GUILayout.Space(10f);
        }

        GUILayout.Label("zoom " + zoom.ToString("0.00") + "x", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
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
        {
            selectedNodeId = -1;
            fitRequested = true;
        }
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

    private void DrawGrid(Rect clipRect)
    {
        DrawGridLines(clipRect, MinorGridSpacing * zoom, MinorGridColor);
        DrawGridLines(clipRect, MajorGridSpacing * zoom, MajorGridColor);
    }

    private void DrawGridLines(Rect clipRect, float spacing, Color color)
    {
        // 너무 촘촘해지면(줌 아웃) 소격자는 생략한다
        if (spacing < 7f)
            return;

        for (float x = Mathf.Repeat(pan.x, spacing); x < clipRect.width; x += spacing)
            EditorGUI.DrawRect(new Rect(x, 0f, 1f, clipRect.height), color);
        for (float y = Mathf.Repeat(pan.y, spacing); y < clipRect.height; y += spacing)
            EditorGUI.DrawRect(new Rect(0f, y, clipRect.width, 1f), color);
    }

    // 언리얼 그래프 에디터처럼 우상단에 큰 반투명 타이틀 워터마크를 깔아준다
    private void DrawWatermark(Rect clipRect, TreeDebugRepository.TreeState tree)
    {
        var titleRect = new Rect(clipRect.width - 420f, 6f, 408f, 34f);
        GUI.Label(titleRect, "BEHAVIOR TREE", watermarkStyle);

        var sub = string.IsNullOrEmpty(tree.TreeId) ? ("monster " + tree.MonsterId) : tree.TreeId;
        var subRect = new Rect(clipRect.width - 420f, 38f, 408f, 18f);
        GUI.Label(subRect, sub, watermarkSubStyle);
    }

    private void DrawEdges(TreeDebugRepository.TreeState tree)
    {
        float idleWidth = Mathf.Max(1.75f, 2.2f * zoom);
        float executedWidth = Mathf.Max(2.5f, 4f * zoom);
        executedWires.Clear();

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
                var wire = new WireInfo
                {
                    From = new Vector2(parentRect.x + parentRect.width * 0.5f, parentRect.yMax),
                    To = new Vector2(childRect.x + childRect.width * 0.5f, childRect.y),
                };

                if (tree.ExecutedPath.Contains(child.NodeId))
                {
                    // 실행 와이어는 유휴 와이어를 모두 그린 뒤 위에 얹는다
                    executedWires.Add(wire);
                }
                else
                {
                    DrawWire(wire, IdleWireColor, idleWidth);
                    DrawPortDots(wire, IdleWireColor);
                }
            }
        }

        for (int i = 0; i < executedWires.Count; ++i)
        {
            DrawWire(executedWires[i], ExecutedColor, executedWidth);
            DrawPortDots(executedWires[i], ExecutedColor);
        }

        if (Application.isPlaying)
        {
            for (int i = 0; i < executedWires.Count; ++i)
                DrawFlowDot(executedWires[i], i);
        }
    }

    private void DrawWire(WireInfo wire, Color color, float width)
    {
        float tangent = Mathf.Clamp((wire.To.y - wire.From.y) * 0.5f, 12f * zoom, 80f * zoom);
        Handles.DrawBezier(
            wire.From, wire.To,
            wire.From + new Vector2(0f, tangent),
            wire.To - new Vector2(0f, tangent),
            color, null, width);
    }

    private void DrawPortDots(WireInfo wire, Color color)
    {
        float radius = Mathf.Max(2f, 3f * zoom);
        DrawCircle(wire.From, radius, color);
        DrawCircle(wire.To, radius, color);
    }

    // 실행 와이어를 따라 흐르는 점: 부모→자식 방향으로 틱 실행 흐름을 보여준다
    private void DrawFlowDot(WireInfo wire, int index)
    {
        float tangent = Mathf.Clamp((wire.To.y - wire.From.y) * 0.5f, 12f * zoom, 80f * zoom);
        var p1 = wire.From + new Vector2(0f, tangent);
        var p2 = wire.To - new Vector2(0f, tangent);
        float t = Mathf.Repeat((float)EditorApplication.timeSinceStartup * 0.6f + index * 0.17f, 1f);
        var point = CubicBezier(wire.From, p1, p2, wire.To, t);
        DrawCircle(point, Mathf.Max(2.5f, 3.5f * zoom), FlowDotColor);
    }

    private static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        return u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
    }

    private void DrawNodes(TreeDebugRepository.TreeState tree, Rect clipRect)
    {
        float radius = Mathf.Max(3f, 6f * zoom);
        float pulse = 0.5f + 0.5f * Mathf.Sin((float)EditorApplication.timeSinceStartup * 5f);

        nameStyle.fontSize = Mathf.RoundToInt(12f * zoom);
        infoStyle.fontSize = Mathf.RoundToInt(10f * zoom);
        statusStyle.fontSize = Mathf.RoundToInt(10f * zoom);
        glyphStyle.fontSize = Mathf.RoundToInt(10f * zoom);
        nodeIdStyle.fontSize = Mathf.RoundToInt(9f * zoom);
        badgeStyle.fontSize = Mathf.RoundToInt(10f * zoom);

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
            bool running = executed && node.Status == syncnet.TreeNodeStatus.Running;

            // 그림자 → 본체 → 헤더 → 상태 스트립 → 외곽선 순으로 겹쳐 그린다
            var shadowRect = new Rect(rect.x + 2f * zoom, rect.y + 3f * zoom, rect.width, rect.height);
            DrawRoundedRect(shadowRect, NodeShadowColor, radius + 1f);
            DrawRoundedRect(rect, executed ? NodeBodyExecutedColor : NodeBodyColor, radius);

            var headerRect = new Rect(rect.x, rect.y, rect.width, NodeHeaderHeight * zoom);
            var headerColor = GetTypeColor(node.NodeType);
            if (!executed)
                headerColor = Dim(headerColor, 0.55f);
            DrawRoundedRectPerCorner(headerRect, headerColor, new Vector4(radius, radius, 0f, 0f));

            float stripHeight = Mathf.Max(2f, StatusStripHeight * zoom);
            var stripRect = new Rect(rect.x, rect.yMax - stripHeight, rect.width, stripHeight);
            var stripColor = GetStatusColor(node.Status);
            if (!executed)
                stripColor = Dim(stripColor, 0.45f);
            DrawRoundedRectPerCorner(stripRect, stripColor, new Vector4(0f, 0f, radius, radius));

            if (executed)
            {
                // 부드러운 글로우 + 본 외곽선. Running 이면 펄스로 숨쉬게 한다
                var glow = ExecutedGlowColor;
                var outline = ExecutedColor;
                if (running)
                {
                    glow.a = Mathf.Lerp(0.15f, 0.45f, pulse);
                    outline.a = Mathf.Lerp(0.6f, 1f, pulse);
                }
                DrawRoundedOutline(Expand(rect, 3f * zoom), glow, Mathf.Max(2f, 3f * zoom), radius + 3f * zoom);
                DrawRoundedOutline(rect, outline, Mathf.Max(1.5f, 2f * zoom), radius);
            }
            else
            {
                DrawRoundedOutline(rect, NodeOutlineColor, 1f, radius);
            }

            if (node.NodeId == hoveredNodeId && node.NodeId != selectedNodeId)
                DrawRoundedOutline(Expand(rect, 1.5f * zoom), HoverColor, 1.5f, radius + 1.5f * zoom);

            if (node.NodeId == selectedNodeId)
                DrawRoundedOutline(Expand(rect, 2.5f * zoom), SelectedColor, Mathf.Max(1.5f, 2f * zoom), radius + 2.5f * zoom);

            // 너무 축소되면 글자는 생략하고 색 블록만 보여준다
            if (zoom > 0.45f)
            {
                float chipSize = 15f * zoom;
                var chipRect = new Rect(headerRect.x + 5f * zoom, headerRect.y + (headerRect.height - chipSize) * 0.5f, chipSize, chipSize);
                DrawRoundedRect(chipRect, new Color(0f, 0f, 0f, 0.35f), 3f * zoom);
                GUI.Label(chipRect, GetTypeGlyph(node.NodeType), glyphStyle);

                var idRect = new Rect(headerRect.xMax - 42f * zoom, headerRect.y, 38f * zoom, headerRect.height);
                GUI.Label(idRect, "#" + node.NodeId, nodeIdStyle);

                var nameRect = new Rect(chipRect.xMax + 4f * zoom, headerRect.y,
                    idRect.x - chipRect.xMax - 8f * zoom, headerRect.height);
                GUI.Label(nameRect, node.Name, nameStyle);

                float lineHeight = 15f * zoom;
                var lineRect = new Rect(rect.x + 7f * zoom, headerRect.yMax + 2f * zoom,
                    rect.width - 14f * zoom, lineHeight);

                statusStyle.normal.textColor = executed ? GetStatusColor(node.Status) : Dim(GetStatusColor(node.Status), 0.6f);
                GUI.Label(lineRect, node.Status.ToString(), statusStyle);

                lineRect.y += lineHeight;
                GUI.Label(lineRect, "S " + node.SuccessCount + "   F " + node.FailureCount + "   R " + node.RunningCount, infoStyle);

                if (!string.IsNullOrEmpty(node.Reason))
                {
                    lineRect.y += lineHeight;
                    GUI.Label(lineRect, node.Reason, infoStyle);
                }
            }

            // 언리얼처럼 이번 틱의 실행 순서를 우상단 원형 배지로 표시
            if (executed)
            {
                float badgeSize = 19f * zoom;
                var badgeRect = new Rect(rect.xMax - badgeSize * 0.55f, rect.y - badgeSize * 0.45f, badgeSize, badgeSize);
                DrawCircle(badgeRect.center, badgeSize * 0.5f, new Color(0.10f, 0.10f, 0.11f, 1f));
                DrawRoundedOutline(badgeRect, ExecutedColor, Mathf.Max(1f, 1.5f * zoom), badgeSize * 0.5f);
                if (zoom > 0.45f)
                    GUI.Label(badgeRect, (executedIndex + 1).ToString(), badgeStyle);
            }
        }
    }

    private void DrawLegend(Rect clipRect)
    {
        float y = clipRect.height - 26f;

        // 항목 폭을 먼저 재서 반투명 필(pill) 배경을 깔아준다
        float width = 10f;
        width += LegendItemWidth("Running") + LegendItemWidth("Success") + LegendItemWidth("Failure") + LegendItemWidth("Executed path");
        DrawRoundedRect(new Rect(6f, y - 3f, width, 22f), new Color(0f, 0f, 0f, 0.45f), 11f);

        float x = 14f;
        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Running), "Running");
        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Success), "Success");
        DrawLegendItem(ref x, y, GetStatusColor(syncnet.TreeNodeStatus.Failure), "Failure");
        DrawLegendItem(ref x, y, ExecutedColor, "Executed path");

        var hint = new GUIContent("wheel: zoom   drag: pan   F: fit");
        var hintSize = EditorStyles.miniLabel.CalcSize(hint);
        GUI.Label(new Rect(clipRect.width - hintSize.x - 10f, y, hintSize.x, 16f), hint, EditorStyles.miniLabel);
    }

    private float LegendItemWidth(string label)
    {
        return 16f + EditorStyles.miniLabel.CalcSize(new GUIContent(label)).x + 12f;
    }

    private void DrawLegendItem(ref float x, float y, Color color, string label)
    {
        DrawCircle(new Vector2(x + 5f, y + 8f), 5f, color);
        x += 16f;
        var content = new GUIContent(label);
        var size = EditorStyles.miniLabel.CalcSize(content);
        GUI.Label(new Rect(x, y, size.x, 16f), content, EditorStyles.miniLabel);
        x += size.x + 12f;
    }

    private void DrawDetailsPanel(Rect rect, TreeDebugRepository.TreeState tree)
    {
        EditorGUI.DrawRect(rect, PanelColor);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0f, 0f, 0f, 0.6f));

        TreeDebugRepository.NodeState node = null;
        if (tree != null && selectedNodeId >= 0)
            tree.Nodes.TryGetValue((ushort)selectedNodeId, out node);

        if (node == null)
        {
            var hintRect = new Rect(rect.x + 10f, rect.y, rect.width - 20f, rect.height);
            GUI.Label(hintRect, "노드를 클릭하면 상세 정보가 표시됩니다", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        var titleRect = new Rect(rect.x + 12f, rect.y + 7f, 300f, 18f);
        GUI.Label(titleRect, node.Name, panelTitleStyle);

        var info = new StringBuilder();
        info.Append(node.NodeType).Append("  ·  #").Append(node.NodeId);
        info.Append("  ·  ").Append(node.Status);
        info.Append("  ·  S ").Append(node.SuccessCount)
            .Append("  F ").Append(node.FailureCount)
            .Append("  R ").Append(node.RunningCount);
        info.Append("  ·  last tick ").Append(node.LastSeenTick);
        var infoRect = new Rect(rect.x + 12f, rect.y + 28f, 460f, 16f);
        GUI.Label(infoRect, info.ToString(), EditorStyles.miniLabel);

        var reasonRect = new Rect(rect.x + 480f, rect.y + 6f, rect.width - 492f, rect.height - 12f);
        if (reasonRect.width > 60f && !string.IsNullOrEmpty(node.Reason))
            GUI.Label(reasonRect, node.Reason, panelReasonStyle);
    }

    #endregion

    #region 입력(줌/패닝/선택)

    private void HandleGraphInput(Rect area, TreeDebugRepository.TreeState tree)
    {
        var e = Event.current;

        if (!area.Contains(e.mousePosition))
        {
            if (hoveredNodeId != -1 && e.type == EventType.MouseMove)
            {
                hoveredNodeId = -1;
                Repaint();
            }
            return;
        }

        if (e.type == EventType.ScrollWheel)
        {
            // 마우스 위치를 기준으로 줌 (그래프 좌표 고정점 유지)
            var local = e.mousePosition - area.position;
            var graphPoint = (local - pan) / zoom;
            zoom = Mathf.Clamp(zoom * (1f - e.delta.y * 0.04f), 0.25f, 2.5f);
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
        else if (e.type == EventType.MouseDown && e.button == 0)
        {
            int hit = HitTest(tree, e.mousePosition - area.position);
            if (hit != selectedNodeId)
            {
                selectedNodeId = hit;
                Repaint();
            }
        }
        else if (e.type == EventType.MouseMove)
        {
            int hit = HitTest(tree, e.mousePosition - area.position);
            if (hit != hoveredNodeId)
            {
                hoveredNodeId = hit;
                Repaint();
            }
        }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
        {
            fitRequested = true;
            e.Use();
            Repaint();
        }
    }

    private int HitTest(TreeDebugRepository.TreeState tree, Vector2 localPosition)
    {
        foreach (var pair in tree.Nodes)
        {
            if (nodePositions.ContainsKey(pair.Key) && NodeRect(pair.Key).Contains(localPosition))
                return pair.Key;
        }
        return -1;
    }

    private void FitToArea(Rect area)
    {
        if (graphSize.x <= 0f || graphSize.y <= 0f)
            return;

        zoom = Mathf.Clamp(Mathf.Min(area.width / graphSize.x, (area.height - 26f) / graphSize.y), 0.25f, 1f);
        pan = new Vector2((area.width - graphSize.x * zoom) * 0.5f, 4f);
    }

    #endregion

    #region 스타일/색상/도형

    private void EnsureStyles()
    {
        if (nameStyle != null)
            return;

        nameStyle = new GUIStyle(EditorStyles.boldLabel);
        nameStyle.normal.textColor = Color.white;
        nameStyle.alignment = TextAnchor.MiddleLeft;
        nameStyle.clipping = TextClipping.Clip;

        infoStyle = new GUIStyle(EditorStyles.label);
        infoStyle.normal.textColor = new Color(0.72f, 0.75f, 0.79f, 1f);
        infoStyle.alignment = TextAnchor.MiddleLeft;
        infoStyle.clipping = TextClipping.Clip;

        statusStyle = new GUIStyle(EditorStyles.boldLabel);
        statusStyle.alignment = TextAnchor.MiddleLeft;
        statusStyle.clipping = TextClipping.Clip;

        badgeStyle = new GUIStyle(EditorStyles.boldLabel);
        badgeStyle.normal.textColor = Color.white;
        badgeStyle.alignment = TextAnchor.MiddleCenter;
        badgeStyle.clipping = TextClipping.Clip;

        glyphStyle = new GUIStyle(EditorStyles.boldLabel);
        glyphStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
        glyphStyle.alignment = TextAnchor.MiddleCenter;
        glyphStyle.clipping = TextClipping.Clip;

        nodeIdStyle = new GUIStyle(EditorStyles.miniLabel);
        nodeIdStyle.normal.textColor = new Color(1f, 1f, 1f, 0.45f);
        nodeIdStyle.alignment = TextAnchor.MiddleRight;
        nodeIdStyle.clipping = TextClipping.Clip;

        watermarkStyle = new GUIStyle(EditorStyles.boldLabel);
        watermarkStyle.fontSize = 26;
        watermarkStyle.normal.textColor = new Color(1f, 1f, 1f, 0.05f);
        watermarkStyle.alignment = TextAnchor.UpperRight;

        watermarkSubStyle = new GUIStyle(EditorStyles.boldLabel);
        watermarkSubStyle.fontSize = 12;
        watermarkSubStyle.normal.textColor = new Color(1f, 1f, 1f, 0.16f);
        watermarkSubStyle.alignment = TextAnchor.UpperRight;

        panelTitleStyle = new GUIStyle(EditorStyles.boldLabel);
        panelTitleStyle.fontSize = 13;
        panelTitleStyle.normal.textColor = Color.white;
        panelTitleStyle.clipping = TextClipping.Clip;

        panelReasonStyle = new GUIStyle(EditorStyles.miniLabel);
        panelReasonStyle.wordWrap = true;
        panelReasonStyle.normal.textColor = new Color(0.80f, 0.82f, 0.85f, 1f);
        panelReasonStyle.alignment = TextAnchor.MiddleLeft;
    }

    private static void DrawRoundedRect(Rect rect, Color color, float radius)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
    }

    private static void DrawRoundedRectPerCorner(Rect rect, Color color, Vector4 radiuses)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, Vector4.zero, radiuses);
    }

    private static void DrawRoundedOutline(Rect rect, Color color, float width, float radius)
    {
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color,
            new Vector4(width, width, width, width), radius);
    }

    private static void DrawCircle(Vector2 center, float radius, Color color)
    {
        var rect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
    }

    private static Rect Expand(Rect rect, float amount)
    {
        return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }

    private static Color Dim(Color color, float factor)
    {
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }

    private Color GetStatusColor(syncnet.TreeNodeStatus status)
    {
        switch (status)
        {
            case syncnet.TreeNodeStatus.Success:
                return new Color(0.24f, 0.72f, 0.36f, 1f);
            case syncnet.TreeNodeStatus.Failure:
                return new Color(0.90f, 0.30f, 0.28f, 1f);
            case syncnet.TreeNodeStatus.Running:
                return new Color(0.26f, 0.60f, 1.0f, 1f);
            case syncnet.TreeNodeStatus.Skipped:
                return new Color(0.55f, 0.55f, 0.58f, 1f);
            default:
                return new Color(0.35f, 0.36f, 0.40f, 1f);
        }
    }

    private Color GetTypeColor(syncnet.TreeNodeType type)
    {
        switch (type)
        {
            case syncnet.TreeNodeType.Control:
                return new Color(0.34f, 0.38f, 0.46f, 1f);
            case syncnet.TreeNodeType.Condition:
                return new Color(0.10f, 0.46f, 0.55f, 1f);
            default:
                return new Color(0.46f, 0.30f, 0.64f, 1f);
        }
    }

    private string GetTypeGlyph(syncnet.TreeNodeType type)
    {
        switch (type)
        {
            case syncnet.TreeNodeType.Control:
                return "≡";
            case syncnet.TreeNodeType.Condition:
                return "?";
            default:
                return "▶";
        }
    }

    #endregion
}
