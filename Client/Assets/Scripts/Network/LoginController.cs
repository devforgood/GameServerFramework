using syncnet;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// 로그인과 재접속 핸드오버를 담당한다.
//
//   최초 로그인: 서버가 맵 id + 스폰 위치를 돌려주면 그 맵 씬을 로드하고, 씬이 준비되면
//                캐릭터를 1회 자동 생성한다(AddAgent).
//   재접속:      서버가 유예 중이던 기존 캐릭터의 actor id 를 돌려준다. 이때는 AddAgent 없이
//                그 id 를 채택하고 맵 씬만 (재)로드하면 서버 상태 동기화가 캐릭터를 되살린다.
//
// 재접속 토큰(플레이어 uuid)은 PlayerPrefs 에 영속 저장해 앱/플레이 재시작에도 유지한다.
public class LoginController
{
    private const string ReconnectTokenKey = "reconnectToken";

    private readonly ServerConnection connection;
    private readonly MapTransition mapTransition;
    private readonly Action<int> setPlayerActorId;
    private readonly Action<Vector3> spawnCharacter;

    private string reconnectToken = "";

    // 신규 로그인 후 자동 스폰 대기 플래그. 씬 로드가 끝나면(또는 이미 대상 씬이면 즉시)
    // 서버가 준 스폰 위치로 AddAgent(Character) 를 1회 보낸다.
    // 재접속 핸드오버는 서버가 기존 캐릭터를 넘겨주므로 제외.
    private bool pendingSpawn = false;

    /// <summary>서버가 알려준 현재 맵 id 와 스폰 위치(캐릭터 생성/배치에 사용).</summary>
    public int MapId { get; private set; } = 0;
    public Vector3 SpawnPos { get; private set; } = Vector3.zero;

    public LoginController(ServerConnection connection, MapTransition mapTransition,
        Action<int> setPlayerActorId, Action<Vector3> spawnCharacter)
    {
        this.connection = connection;
        this.mapTransition = mapTransition;
        this.setPlayerActorId = setPlayerActorId;
        this.spawnCharacter = spawnCharacter;
    }

    /// <summary>재접속 토큰을 영속 저장소에서 복원한다(Session.Awake 시점).</summary>
    public void RestoreToken()
    {
        // 앱/플레이 재시작으로 Session 이 새로 생성돼도, 유예 시간(서버 60초) 내라면
        // 이 토큰으로 기존 캐릭터를 넘겨받을 수 있다.
        reconnectToken = PlayerPrefs.GetString(ReconnectTokenKey, "");
    }

    public void Login()
    {
        int messageId = connection.NextMessageId();
        // 보유한 재접속 토큰(uuid)을 함께 보낸다. 최초 로그인이면 빈 문자열이라 신규 로그인으로
        // 처리되고, 재접속이면 서버가 이 토큰으로 유예 중이던 기존 캐릭터를 넘겨준다.
        connection.Send(PacketFactory.CreateLoginMessage(messageId, reconnectToken), response =>
        {
            if (response.MsgType != GameMessages.Login)
            {
                Debug.Log("Login Error");
                return;
            }

            Login login = response.Msg<Login>().Value;
            if (response.Result != StatusCode.Success)
            {
                Debug.Log("Login Fail");
                return;
            }

            MapId = login.MapId;
            if (login.Pos.HasValue)
                SpawnPos = new Vector3(login.Pos.Value.X, login.Pos.Value.Y, login.Pos.Value.Z);

            // 재접속 토큰(플레이어 uuid) 저장. 이후 재접속 로그인 요청에 되돌려 보낸다.
            if (!string.IsNullOrEmpty(login.Uuid))
            {
                reconnectToken = login.Uuid;
                PlayerPrefs.SetString(ReconnectTokenKey, reconnectToken);
                PlayerPrefs.Save();
            }

            if (login.ActorId != 0)
                HandleReconnectHandover(login.ActorId);
            else
                HandleNewLogin();
        });
    }

    /// <summary>
    /// 신규 로그인 자동 스폰: 서버가 준 위치(기본 스폰 마커 또는 이전 로그아웃 위치)로
    /// 캐릭터 생성을 1회 요청한다. 대기 플래그가 없으면 아무것도 하지 않는다.
    /// </summary>
    public void TrySpawnCharacter()
    {
        if (!pendingSpawn)
            return;
        pendingSpawn = false;

        Debug.Log($"Auto spawn character at login pos({SpawnPos.x},{SpawnPos.y},{SpawnPos.z})");
        spawnCharacter(SpawnPos);
    }

    // 재접속: AddAgent(신규 스폰)를 하지 않고 서버가 유지하던 actor id 를 채택한 뒤 맵 씬을
    // (재)로드한다. 로드 완료 후 서버가 보낸 상태 동기화(SendStateTo)가 큐에서 처리되며
    // 기존 캐릭터가 재생성된다. (게이트 이동과 동일한 씬 로드 파이프라인)
    private void HandleReconnectHandover(int actorId)
    {
        setPlayerActorId(actorId);
        Debug.Log($"Reconnect handover. actorId:{actorId}, mapId:{MapId}, pos({SpawnPos.x},{SpawnPos.y},{SpawnPos.z})");

        Gamedata.Map map;
        if (GameManager.Instance.resource.Maps.TryGetValue(MapId, out map) && !string.IsNullOrEmpty(map.scene))
        {
            // 같은 씬이어도 재로드해 이전(정지된) 액터/참조를 초기화한다.
            mapTransition.LoadMapScene(map.scene);
        }
    }

    private void HandleNewLogin()
    {
        Debug.Log($"Login Success. mapId:{MapId}, pos({SpawnPos.x},{SpawnPos.y},{SpawnPos.z})");

        // 씬 준비가 끝나면 서버가 준 스폰 위치로 캐릭터를 자동 생성한다.
        pendingSpawn = true;

        // 맵 데이터에 연동된 씬 정보로 해당 맵 씬을 로드한다(현재 씬과 다를 때만).
        Gamedata.Map map;
        if (GameManager.Instance.resource.Maps.TryGetValue(MapId, out map)
            && !string.IsNullOrEmpty(map.scene)
            && SceneManager.GetActiveScene().name != map.scene)
        {
            mapTransition.LoadMapScene(map.scene); // 스폰은 씬 로드 완료 후
        }
        else
        {
            // 이미 대상 씬에 있으면 즉시 스폰한다.
            TrySpawnCharacter();
        }
    }
}
