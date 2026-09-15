using UnityEngine;

/// <summary>
/// 보기 전용 배경(지형·나무·건물·물)을 묶는 루트 표식.
///
/// 이동은 서버 navmesh 가 정하고, navmesh 는 씬의 게임 지오메트리(바닥·장애물·경사로)로 굽는다.
/// 배경은 그 위에 덧입힌 껍데기라서 두 가지를 지켜야 한다.
///   - NavMesh 굽기와 맵 크기 계산에서 빠진다(MapPipeline 이 이 컴포넌트 밑을 건너뛴다).
///     같이 구우면 울타리 바깥 언덕까지 걸을 수 있는 땅이 된다.
///   - 콜라이더를 끈다. 클릭 이동은 마우스 레이가 처음 맞은 점을 목적지로 쓰므로
///     지붕이나 나무에 맞으면 엉뚱한 곳으로 간다.
///
/// 생성은 EnvironmentDressingTool 이 한다. 손으로 고치면 다시 생성할 때 덮어써진다.
/// </summary>
[DisallowMultipleComponent]
public class EnvironmentDressing : MonoBehaviour
{
}
