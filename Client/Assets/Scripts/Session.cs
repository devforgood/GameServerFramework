using FlatBuffers;
using syncnet;
using System;
using System.Collections.Generic;
using UnityEngine;

// 클라이언트 세션의 '조립과 창구' 역할만 한다. 실제 일은 역할별 객체가 나눠 맡는다:
//
//   ServerConnection  연결/자동 재접속/하트비트/송신/요청-응답
//   ActorSync         액터 생성·상태 갱신·위치 보간
//   SkillController   스킬 시전·거부 처리·연출·점프 애니메이션
//   MapTransition     게이트 이동과 씬 전환
//   LoginController   로그인/재접속 핸드오버/자동 스폰
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

    /// <summary>내 캐릭터의 agent id. 로그인/스폰/게이트 이동 응답으로 갱신된다.</summary>
    public int player_agnet_id = 0;

    public long unixTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // ── 외부에 열어 둔 접근자(기존 호출부 유지) ──
    /// <summary>agentId → 씬 오브젝트(Actor 가 자기 참조를 지울 때 사용).</summary>
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
        skills = new SkillController(this, connection, actors, () => player_agnet_id);
        mapTransition = new MapTransition(connection, actors);
        login = new LoginController(connection, mapTransition,
            agentId => player_agnet_id = agentId,
            pos => AddAgent(0, pos, GameObjectType.Character));

        // 연결되면(최초/재접속 공통) 자동 로그인한다.
        connection.Connected += login.Login;

        // 게이트 이동으로 캐릭터가 새로 만들어지면 id 를 갱신하고 스킬 예측을 버린다.
        mapTransition.GateEntered += agentId =>
        {
            player_agnet_id = agentId;
            skills.Reset();
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
        }

        connection.CompleteResponse(recv_msg);
    }

    // ── 접속 ──
    public void startServer() { connection.Connect(); }
    public void ManualReconnect() { connection.ManualReconnect(); }
    public void Login() { login.Login(); }

    // ── 서버로 보내는 게임 명령 ──
    public void AddAgent(int agent_id, Vector3 pos, GameObjectType type)
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
                player_agnet_id = addAgent.AgentId;
                Debug.Log($"Player Agent ID: {player_agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
            }
        });
    }

    public void RemoveAgent(int agentId)
    {
        connection.Send(PacketFactory.CreateRemoveAgentMessage(agentId));
    }

    public void SetMoveTarget(int agentId, Vector3 pos)
    {
        Debug.Log($"SetMoveTarget agent_id: {agentId}, pos({pos.x}, {pos.y}, {pos.z}) ");
        connection.Send(PacketFactory.CreateSetMoveTargetMessage(agentId, pos));
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

    public void EnterGate(int mapId, int gateId, string sceneName)
    {
        mapTransition.EnterGate(mapId, gateId, sceneName);
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
