using FlatBuffers;
using syncnet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 클라이언트 세션의 '조립과 창구' 역할만 한다. 실제 일은 역할별 객체가 나눠 맡는다:
//
//   ServerConnection  연결/자동 재접속/하트비트/송신/요청-응답
//   ActorSync         액터 생성·상태 갱신·위치 보간
//   SkillController   스킬 시전·거부 처리·연출·점프 애니메이션
//   MapTransition     게이트 이동과 씬 전환
//   LoginController   로그인/재접속 핸드오버/자동 스폰
//   DialogController  NPC 대화(상호작용 → 노드 표시 → 고른 번호 회신)
//
// Session 이 MonoBehaviour 로 남는 이유는 두 가지다. 씬에 배치된 컴포넌트 참조를 유지해야 하고,
// TcpConnection 이 Receiver 를 Session 타입으로 캐스팅하기 때문이다. 그래서 프레임 진행(Update)과
// 외부 공개 API(Gate/InputHandler/Actor 가 부르는 것들)는 여기 두고, 내용은 전부 위임한다.
public class Session : MonoBehaviour
{
    // 게이트(Gate) 등에서 접근할 수 있도록 노출하는 인스턴스.
    // 씬 전환(게이트 이동) 중에도 네트워크 연결을 유지하기 위해 DontDestroyOnLoad 로 유지된다.
    public static Session Instance { get; private set; }

    private ServerConnection connection;
    private ActorSync actors;
    private SkillController skills;
    private MapTransition mapTransition;
    private LoginController login;
    private DialogController dialogs;

    /// <summary>NPC 에게 말을 거는 키. 근처에 NPC 가 없으면 아무 일도 하지 않는다.</summary>
    public KeyCode interactKey = KeyCode.F;

    /// <summary>내 캐릭터의 actor id. 로그인/스폰/게이트 이동 응답으로 갱신된다.</summary>
    public int player_actor_id = 0;

    /// <summary>
    /// 내 레벨과 누적 경험치. 서버가 로그인 직후와 레벨이 오를 때 알려 준다(PlayerStatSync).
    /// 게이트의 required_level 을 밟기 전에 확인하는 데 쓴다.
    /// </summary>
    public int player_level { get; private set; } = 1;
    public long player_exp { get; private set; } = 0;

    /// <summary>
    /// 서버에게서 레벨을 받은 적이 있는가. 받기 전의 1 은 "레벨 1" 이 아니라 "모른다" 이므로,
    /// 그 상태로 게이트를 막으면 서버가 허락할 이동을 클라가 가로막는다.
    /// </summary>
    public bool player_level_known { get; private set; } = false;

    /// <summary>레벨이 바뀌었을 때(로그인 직후 첫 통보 포함). HUD 가 붙으면 여기에 건다.</summary>
    public event Action<int> PlayerLevelChanged;

    public long unixTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // ── 외부에 열어 둔 접근자(기존 호출부 유지) ──
    /// <summary>actorId → 씬 오브젝트(Actor 가 자기 참조를 지울 때 사용).</summary>
    public Dictionary<int, GameObject> game_objects => actors.Objects;

    /// <summary>HUD 등에서 남은 쿨다운을 표시하기 위한 접근자.</summary>
    public SkillCooldownTracker SkillCooldowns => skills.Cooldowns;

    /// <summary>게이트 재이동 억제 플래그(Gate.cs 가 참조/해제).</summary>
    public bool SuppressGateWarpUntilExit
    {
        get { return mapTransition.SuppressGateWarpUntilExit; }
        set { mapTransition.SuppressGateWarpUntilExit = value; }
    }

    /// <summary>서버가 알려준 로그인 맵/스폰 위치.</summary>
    public int loginMapId => login.MapId;
    public Vector3 loginSpawnPos => login.SpawnPos;

    void Awake()
    {
        // 씬 전환에도 연결을 유지하기 위한 영속 싱글톤. 새 씬에 중복 Session 이 있으면 자가 파괴한다.
        if (Instance != null && Instance != this)
        {
            enabled = false; // 파괴는 프레임 끝에 일어나므로 그때까지 Start/Update 가 돌지 않게 막는다
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildParts();
        login.RestoreToken();
    }

    // 역할별 객체를 만들고 서로 연결한다. 의존 방향은 한쪽으로만 흐른다:
    // 연결 → (액터/스킬/맵/로그인). 반대로 부를 일은 이벤트로 되돌려 받는다.
    private void BuildParts()
    {
        connection = new ServerConnection(this, () => GameManager.Instance.server_address);
        actors = new ActorSync();
        skills = new SkillController(this, connection, actors, () => player_actor_id);
        mapTransition = new MapTransition(connection, actors);
        dialogs = new DialogController(connection);
        login = new LoginController(connection, mapTransition,
            actorId => player_actor_id = actorId,
            pos => AddAgent(0, pos, GameObjectType.Character));

        // 연결되면(최초/재접속 공통) 자동 로그인한다.
        connection.Connected += login.Login;

        // 게이트 이동으로 캐릭터가 새로 만들어지면 id 를 갱신하고 스킬 예측을 버린다.
        mapTransition.GateEntered += actorId =>
        {
            player_actor_id = actorId;
            skills.Reset();

            // 맵이 바뀌면 열려 있던 대화는 서버 쪽에서도 의미가 없다.
            dialogs.Close();
        };

        // 씬이 준비되면 신규 로그인 대기 중이던 캐릭터를 스폰한다.
        mapTransition.SceneReady += login.TrySpawnCharacter;
    }

    void Start()
    {
        Application.runInBackground = true;
        // BT 디버그 뷰(Tools/BT Debug Viewer)가 트리 정의를 요청할 때 사용할 송신 경로 등록
        TreeDebugRepository.DefinitionRequestSender = RequestTreeDebug;
        startServer();
    }

    void OnDestroy()
    {
        if (TreeDebugRepository.DefinitionRequestSender == (System.Action<long>)RequestTreeDebug)
            TreeDebugRepository.DefinitionRequestSender = null;
    }

    void Update()
    {
        // 중복 Session 은 Awake 에서 Destroy 되지만 실제 파괴는 프레임 끝이라 Update 가 한 번 더 돈다.
        // 그때는 아직(또는 영영) 조립되지 않았으므로 아무 일도 하지 않는다.
        if (connection == null)
            return;

        connection.Tick();

        // 통합 큐 처리 (네트워크 데이터 + 연결 이벤트)
        // 씬 로드 대기 중에는 처리하지 않고 큐에 남겨, 씬 로드 완료 후 새 씬에서 처리한다.
        if (!mapTransition.IsLoadingScene)
        {
            object item;
            while (connection.TryDequeue(out item))
            {
                if (item is byte[])
                {
                    OnReceive((byte[])item);

                    // 게이트 응답 처리로 씬 로드가 시작되면, 남은 메시지(새 맵 동기화 등)는
                    // 씬 로드 완료 후 처리하도록 이번 프레임 처리를 중단한다.
                    if (mapTransition.IsLoadingScene)
                        break;
                }
                else if (item is string)
                {
                    connection.HandleQueuedEvent((string)item);
                }
            }
        }

        actors.Tick();

        // 대화 중에는 말 걸기 키를 받지 않는다(창 안의 선택지로 진행한다).
        if (Input.GetKeyDown(interactKey) && !dialogs.IsOpen)
            TryInteractNearestNpc();
    }

    // 수신 메시지를 종류별 담당자에게 넘긴다. 요청-응답 콜백은 그 뒤에 실행한다(기존 순서 유지).
    void OnReceive(byte[] bytes)
    {
        var recv_msg = GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
        switch (recv_msg.MsgType)
        {
            case GameMessages.UpdateActorNotify:
                actors.Apply(recv_msg.Msg<UpdateActorNotify>().Value);
                break;
            case GameMessages.UseSkill:
                skills.OnServerMessage(recv_msg, recv_msg.Msg<syncnet.UseSkill>().Value);
                break;
            case GameMessages.Ping:
                connection.OnPong();
                break;
            case GameMessages.TreeDebugSync:
                TreeDebugRepository.Apply(recv_msg.Msg<TreeDebugSync>().Value);
                break;
            case GameMessages.EnterGate:
                OnEnterGatePush(recv_msg);
                break;
            case GameMessages.DialogNode:
                dialogs.Apply(recv_msg, recv_msg.Msg<DialogNode>().Value);
                break;
            case GameMessages.PlayerStatSync:
                OnPlayerStatSync(recv_msg.Msg<PlayerStatSync>().Value);
                break;
        }

        connection.CompleteResponse(recv_msg);
    }

    // 서버가 먼저 보낸 EnterGate(= 강제 이동 통보)를 처리한다.
    // 내가 요청한 게이트 이동의 응답은 id 가 붙어 있고 CompleteResponse 가 처리하므로 여기서 건너뛴다.
    // (레이드 종료로 인스턴스가 파괴될 때 서버가 플레이어를 출구 맵으로 내보내는 경로다.)
    void OnEnterGatePush(GameMessage recv_msg)
    {
        if (recv_msg.Id != 0)
            return;

        if (recv_msg.Result != StatusCode.Success)
        {
            Debug.LogWarning("Forced move push with non-success result. ignored.");
            return;
        }

        EnterGate enterGate = recv_msg.Msg<EnterGate>().Value;

        var destMap = GameManager.Instance.resource.GetMapById(enterGate.MapId);
        if (destMap == null)
        {
            Debug.LogError($"Forced move: destination map id {enterGate.MapId} not found in loaded map data.");
            return;
        }

        string destScene = !string.IsNullOrEmpty(destMap.scene) ? destMap.scene : destMap.name;
        mapTransition.ForcedMove(enterGate.MapId, enterGate.GateId, enterGate.ActorId, destScene);
    }

    // ── 성장 상태 ──

    private void OnPlayerStatSync(PlayerStatSync stat)
    {
        player_exp = stat.Exp;

        bool first = !player_level_known;
        player_level_known = true;

        if (player_level == stat.Level && !first)
            return;

        player_level = stat.Level;
        Debug.Log($"[Level] now {player_level} (exp {player_exp})");

        if (PlayerLevelChanged != null)
            PlayerLevelChanged(player_level);
    }

    // ── 상호작용 / 대화 ──

    /// <summary>NPC 나 맵 오브젝트에 말을 건다. 서버가 맵과 거리를 다시 검증한다.</summary>
    public void Interact(int targetId) { dialogs.Interact(targetId); }

    /// <summary>
    /// 지금 맵에서 상호작용 거리 안에 있는 가장 가까운 NPC 에게 말을 건다.
    ///
    /// NPC 는 아직 씬에 오브젝트로 놓여 있지 않아 눌러서 고를 대상이 없다. 위치는
    /// npc.json 에 있으므로 그 좌표로 가장 가까운 NPC 를 찾아 같은 요청을 보낸다
    /// (서버도 데이터 좌표로 거리를 재므로 판정은 같다).
    /// </summary>
    public bool TryInteractNearestNpc()
    {
        var resource = GameManager.Instance != null ? GameManager.Instance.resource : null;
        if (resource == null)
            return false;

        GameObject playerObject;
        if (!game_objects.TryGetValue(player_actor_id, out playerObject) || playerObject == null)
            return false;

        // 지금 맵은 로드된 씬으로 안다(맵 이동이 씬 교체로 이뤄진다).
        var currentMap = resource.GetMapByName(SceneManager.GetActiveScene().name);
        if (currentMap == null)
            return false;

        Vector3 playerPos = playerObject.transform.position;

        int bestId = 0;
        float bestDistanceSq = float.MaxValue;

        foreach (var npc in resource.Npcs.Values)
        {
            if (npc == null || npc.map_id != currentMap.id || npc.position == null)
                continue;

            // 거리는 수평으로만 잰다(서버와 같은 규칙 — 높이는 지형이 정한다).
            float dx = playerPos.x - (float)npc.position.x;
            float dz = playerPos.z - (float)npc.position.z;
            float distanceSq = dx * dx + dz * dz;

            float range = npc.interact_range > 0.0 ? (float)npc.interact_range : 3.0f;
            if (distanceSq > range * range || distanceSq >= bestDistanceSq)
                continue;

            bestDistanceSq = distanceSq;
            bestId = npc.id;
        }

        if (bestId == 0)
            return false;

        Interact(bestId);
        return true;
    }

    // ── 접속 ──
    public void startServer() { connection.Connect(); }
    public void ManualReconnect() { connection.ManualReconnect(); }
    public void Login() { login.Login(); }

    // ── 서버로 보내는 게임 명령 ──
    public void AddAgent(int actor_id, Vector3 pos, GameObjectType type)
    {
        int messageId = connection.NextMessageId();
        connection.Send(PacketFactory.CreateAddAgentMessage(messageId, pos, type), response =>
        {
            if (response.MsgType != GameMessages.AddAgent)
            {
                Debug.Log("AddAgent Error");
                return;
            }

            if (response.Result != StatusCode.Success)
            {
                Debug.Log("AddAgent Fail");
                return;
            }

            AddAgent addAgent = response.Msg<AddAgent>().Value;
            Debug.Log("AddAgent Success");
            if (addAgent.GameObjectType == (int)GameObjectType.Character)
            {
                player_actor_id = addAgent.ActorId;
                Debug.Log($"Player Agent ID: {player_actor_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
            }
        });
    }

    public void RemoveAgent(int actorId)
    {
        connection.Send(PacketFactory.CreateRemoveAgentMessage(actorId));
    }

    public void SetMoveTarget(int actorId, Vector3 pos)
    {
        Debug.Log($"SetMoveTarget actor_id: {actorId}, pos({pos.x}, {pos.y}, {pos.z}) ");
        connection.Send(PacketFactory.CreateSetMoveTargetMessage(actorId, pos));
    }

    public void SetRaycast(Vector3 pos)
    {
        Debug.Log($"SetRaycast pos({pos.x}, {pos.y}, {pos.z}) ");
        connection.Send(PacketFactory.CreateSetRaycastMessage(pos));
    }

    public void UseSkill(int skillId, Vector3 pos, int type)
    {
        skills.Cast(skillId, pos, type);
    }

    /// <summary>밟은 게이트 id 를 서버에 알린다. 목적지는 서버가 정한다.</summary>
    public void EnterGate(int gateId)
    {
        mapTransition.EnterGate(gateId);
    }

    /// <summary>특정 몬스터의 BT 정의(+현재 상태)를 서버에 요청한다.</summary>
    public void RequestTreeDebug(long monsterId)
    {
        connection.Send(PacketFactory.CreateTreeDebugRequestMessage(monsterId));
    }

    /// <summary>임의 패킷 전송(도구/디버그용). 일반 흐름은 위의 명령 메서드를 쓴다.</summary>
    public void SendMessage(byte[] msg, Action<GameMessage> response = null)
    {
        connection.Send(msg, response);
    }

    public int nextMesssagetId()
    {
        return connection.NextMessageId();
    }
}
