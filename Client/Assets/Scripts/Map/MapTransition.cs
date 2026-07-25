using syncnet;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 맵(씬) 전환을 담당한다: 게이트 이동 요청 → 서버 응답 → 목적지 씬 로드 → 로드 완료 후 동기화 재개.
//
// 씬 로드 중에는 네트워크 처리를 멈춰야 한다(IsLoadingScene). 서버는 게이트 응답 직후 새 맵 상태를
// 보내는데, 씬 로드 전에 처리하면 새로 만든 액터가 씬 전환으로 파괴되기 때문이다.
// 그래서 Session 의 수신 큐 펌프는 이 플래그를 보고 메시지를 큐에 남겨 둔다.
public class MapTransition
{
    private readonly ServerConnection connection;
    private readonly ActorSync actors;

    private bool isChangingMap = false;  // 게이트 이동 요청 진행 중(중복 요청 방지)

    // 다음 씬 로드 완료 시 재이동 억제를 걸지 여부. 단방향(one_way) 게이트로 스폰 지점에
    // 도착하는 경우(gateId 0)는 게이트 위가 아니므로 억제하면 다음 게이트 진입이 한 번 무시된다.
    private bool suppressWarpOnNextSceneLoad = true;

    /// <summary>씬 로드 대기 중 — 이 동안 네트워크 동기화 처리를 멈춘다.</summary>
    public bool IsLoadingScene { get; private set; } = false;

    // 게이트 이동으로 막 도착하면 목적지 게이트 위치에 스폰되어 도착 즉시 트리거가 재발동한다.
    // "밖에서 안으로 들어온 경우"만 이동시키기 위해, 도착 직후에는 재이동을 억제하고
    // 플레이어가 게이트 밖으로 나가면(OnTriggerExit) 해제한다. Gate.cs 에서 참조/해제한다.
    public bool SuppressGateWarpUntilExit { get; set; } = false;

    /// <summary>게이트 이동 성공 — 서버가 목적지 맵에 캐릭터를 재생성하며 부여한 새 agent id.</summary>
    public event Action<int> GateEntered;

    /// <summary>씬이 준비됨(로드 완료, 또는 로드가 필요 없거나 불가능해 현재 씬 유지).</summary>
    public event Action SceneReady;

    public MapTransition(ServerConnection connection, ActorSync actors)
    {
        this.connection = connection;
        this.actors = actors;
    }

    /// <summary>
    /// 게이트 이동을 서버에 요청한다. 성공 응답 시 목적지 맵 씬을 로드한다.
    /// (씬 단위 이동. 프리팹 단위 로드는 추후 최적화로 진행 예정.)
    /// </summary>
    public void EnterGate(int mapId, int gateId, string sceneName)
    {
        if (isChangingMap)
        {
            Debug.Log("EnterGate ignored: already changing map.");
            return;
        }
        isChangingMap = true;

        int messageId = connection.NextMessageId();
        Debug.Log($"EnterGate request mapId:{mapId}, gateId:{gateId}, scene:{sceneName}");
        connection.Send(PacketFactory.CreateEnterGateMessage(messageId, mapId, gateId), response =>
        {
            if (response.MsgType == GameMessages.EnterGate && response.Result == StatusCode.Success)
            {
                EnterGate enterGate = response.Msg<EnterGate>().Value;

                Vector3 destPos = Vector3.zero;
                if (enterGate.Pos.HasValue)
                    destPos = new Vector3(enterGate.Pos.Value.X, enterGate.Pos.Value.Y, enterGate.Pos.Value.Z);

                Debug.Log($"EnterGate Success. new agentId:{enterGate.AgentId}, pos({destPos.x},{destPos.y},{destPos.z})");
                GateEntered?.Invoke(enterGate.AgentId);

                // 게이트 위 도착(gateId != 0)일 때만 재이동 억제가 필요하다.
                // 스폰 지점 도착(gateId 0, one_way 인스턴스 입구)은 게이트 위가 아니다.
                suppressWarpOnNextSceneLoad = enterGate.GateId != 0;

                LoadMapScene(sceneName);
            }
            else
            {
                Debug.Log("EnterGate Fail");
                isChangingMap = false;
            }
        });
    }

    /// <summary>
    /// 맵 씬을 로드한다. 로드가 끝날 때까지 네트워크 동기화 처리를 멈춰(OnSceneLoaded 에서 재개),
    /// 로드 전에 도착한 새 맵 상태가 씬 전환으로 파괴되지 않게 한다.
    /// </summary>
    public void LoadMapScene(string sceneName)
    {
        // Build Settings 에 등록되지 않은 씬은 로드할 수 없다. 강제로 LoadScene 하면 예외가 나므로
        // 방어적으로 확인하고, 불가능하면 현재 씬을 유지한 채 네트워크 처리를 계속한다.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings (File > Build Settings 에 추가 필요). 씬 로드를 건너뜁니다.");
            IsLoadingScene = false;
            isChangingMap = false;
            SceneReady?.Invoke(); // 로그인 자동 스폰 대기 중이었다면 현재 씬에서라도 스폰한다(서버 위치가 권위).
            return;
        }

        IsLoadingScene = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 씬 로드 완료 콜백. 씬 전환으로 파괴된 이전 액터 참조를 정리하고 네트워크 동기화 처리를 재개한다.
    /// 대기 중이던 새 맵 상태(UpdateActorNotify)가 새 씬에서 액터를 생성한다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 씬 로드로 파괴된 이전 맵의 액터 GameObject 참조 정리(딕셔너리에는 파괴된 참조가 남아 있다).
        actors.Clear();

        IsLoadingScene = false;
        isChangingMap = false;

        // 게이트 위 도착이면 도착 즉시 트리거가 재발동하므로, 밖으로 걸어 나가기 전까지
        // 재이동을 억제한다(무한 왕복 방지). 스폰 지점 도착(one_way, gateId 0)은 억제하지 않는다.
        SuppressGateWarpUntilExit = suppressWarpOnNextSceneLoad;
        suppressWarpOnNextSceneLoad = true;

        Debug.Log($"Gate scene loaded: {scene.name}");
        SceneReady?.Invoke();
    }
}
