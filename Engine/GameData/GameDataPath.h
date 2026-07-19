#pragma once
#include <string>

// 게임 데이터 단일 소스(<repo>/Client/Assets/Resources/GameData/) 경로 해석.
// 모든 소비자(Game 서버, UnitTest, Benchmark)가 이 폴더 하나를 읽는다 — 사본 없음.
// 개발 환경에서는 작업 디렉터리/exe 위치에서 위로 올라가며 리포의 통합 폴더를 찾고,
// 리포 밖(실서비스 배포 레이아웃)에서는 실행 위치 옆 "GameData/" 로 폴백한다.
namespace GameDataPath
{
    // 항상 '/' 로 끝나는 경로를 반환한다 (예: "D:/.../Client/Assets/Resources/GameData/").
    // 통합 폴더를 찾지 못하면 "GameData/" (실행 위치 기준 상대 경로)를 반환한다.
    // 최초 호출 시 1회만 탐색하고 이후에는 캐시를 돌려준다.
    const std::string& Resolve();
}
