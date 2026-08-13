using System;
using System.Collections.Generic;

/// <summary>
/// 서버 BTDebugManager 가 보내주는 TreeDebugSync 를 몬스터별로 보관하는 런타임 저장소.
/// Session 이 수신한 데이터를 넣어주면 에디터 창(Tools/BT Debug Viewer)이 Changed 이벤트를
/// 구독해서 그래프를 다시 그린다. 인게임 UI 는 만들지 않는다.
/// </summary>
public static class TreeDebugRepository
{
    public class TreeState
    {
        public string TreeId = string.Empty;
        public long MonsterId = -1;
        // 서버에서 전체 트리 정의를 받았는지. false 면 프레임에 등장한 노드만 아는 상태다.
        public bool HasDefinition;
        public ulong Tick;
        public syncnet.AIState AiState = syncnet.AIState.Patrol;
        public long TargetActorId = -1;
        // 노드 추가/부모 변경 등 구조가 바뀔 때마다 증가. 뷰는 이 값이 달라졌을 때만 레이아웃을 다시 계산한다.
        public int StructureVersion;
        public readonly Dictionary<ushort, NodeState> Nodes = new Dictionary<ushort, NodeState>();
        public readonly List<NodeState> Roots = new List<NodeState>();
        public readonly List<ushort> ExecutedPath = new List<ushort>();
    }

    public class NodeState
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

    /// <summary>정의/프레임이 반영될 때마다 발생. 메인 스레드(Session.Update)에서만 호출된다.</summary>
    public static event Action Changed;

    /// <summary>정의 요청을 서버로 보내는 송신자. 플레이 모드에서 Session 이 등록한다.</summary>
    public static Action<long> DefinitionRequestSender;

    private const float RequestCooldownSeconds = 2f;

    private static readonly Dictionary<long, TreeState> trees = new Dictionary<long, TreeState>();
    private static readonly List<long> monsterOrder = new List<long>();
    private static readonly Dictionary<long, float> lastRequestTimes = new Dictionary<long, float>();

    public static IReadOnlyList<long> MonsterIds => monsterOrder;

    public static TreeState GetTree(long monsterId)
    {
        TreeState tree;
        return trees.TryGetValue(monsterId, out tree) ? tree : null;
    }

    public static void Apply(syncnet.TreeDebugSync sync)
    {
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

        Changed?.Invoke();
    }

    /// <summary>
    /// 정의가 없는 트리의 정의를 서버에 요청한다. 이미 정의가 있거나 쿨타임 중이면 무시.
    /// 몬스터는 플레이어 접속 전에 스폰되어 최초 정의 브로드캐스트를 받지 못하므로,
    /// 프레임만 도착한 몬스터나 디버그 뷰에서 새로 선택한 몬스터는 이 경로로 정의를 받아온다.
    /// </summary>
    public static void RequestDefinition(long monsterId)
    {
        if (monsterId < 0 || DefinitionRequestSender == null)
            return;

        var tree = GetTree(monsterId);
        if (tree != null && tree.HasDefinition)
            return;

        float now = UnityEngine.Time.realtimeSinceStartup;
        float lastTime;
        if (lastRequestTimes.TryGetValue(monsterId, out lastTime) && now - lastTime < RequestCooldownSeconds)
            return;

        lastRequestTimes[monsterId] = now;
        DefinitionRequestSender(monsterId);
    }

    public static void Clear()
    {
        trees.Clear();
        monsterOrder.Clear();
        lastRequestTimes.Clear();
        Changed?.Invoke();
    }

    private static void ApplyDefinition(syncnet.TreeDebugDefinition definition)
    {
        var tree = GetOrCreateTree(definition.MonsterId);
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

        RebuildHierarchy(tree);
        tree.StructureVersion++;
        tree.HasDefinition = true;
    }

    private static void ApplyFrame(syncnet.TreeDebugRuntimeFrame frame)
    {
        var tree = GetOrCreateTree(frame.MonsterId);
        if (!string.IsNullOrEmpty(frame.TreeId))
            tree.TreeId = frame.TreeId;

        tree.Tick = frame.Tick;
        tree.AiState = frame.AiState;
        tree.TargetActorId = frame.TargetActorId;
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
                // 정의보다 프레임이 먼저 도착한 노드: 일단 루트로 붙이고 구조 버전을 올린다
                node = new NodeState();
                node.NodeId = value.NodeId;
                node.Name = value.Name ?? string.Empty;
                tree.Nodes[node.NodeId] = node;
                RebuildHierarchy(tree);
                tree.StructureVersion++;
            }

            node.Name = string.IsNullOrEmpty(value.Name) ? node.Name : value.Name;
            node.Status = value.Status;
            node.Reason = value.Reason ?? string.Empty;
            node.SuccessCount = value.SuccessCount;
            node.FailureCount = value.FailureCount;
            node.RunningCount = value.RunningCount;
            node.LastSeenTick = frame.Tick;
        }

        // 정의 없이 프레임만 도착한 몬스터: 전체 구조를 모르는 상태이므로 정의를 요청한다
        if (!tree.HasDefinition)
            RequestDefinition(tree.MonsterId);
    }

    private static TreeState GetOrCreateTree(long monsterId)
    {
        TreeState tree;
        if (!trees.TryGetValue(monsterId, out tree))
        {
            tree = new TreeState();
            tree.MonsterId = monsterId;
            trees.Add(monsterId, tree);
            monsterOrder.Add(monsterId);
        }
        return tree;
    }

    private static void RebuildHierarchy(TreeState tree)
    {
        tree.Roots.Clear();
        foreach (var pair in tree.Nodes)
        {
            pair.Value.Children.Clear();
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
    }

    private static void SortNodes(List<NodeState> nodes)
    {
        nodes.Sort((a, b) => a.NodeId.CompareTo(b.NodeId));
        for (int i = 0; i < nodes.Count; ++i)
        {
            SortNodes(nodes[i].Children);
        }
    }
}
