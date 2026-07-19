#include "GameDataPath.h"
#include <filesystem>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

namespace
{
    namespace fs = std::filesystem;

    // base 에서 루트까지 올라가며 통합 리소스 폴더를 찾는다.
    // Map.json 존재까지 확인해 빈 폴더/무관한 폴더 오탐을 막는다.
    std::string FindUnifiedDir(fs::path base)
    {
        std::error_code ec;
        base = fs::absolute(base, ec);
        if (ec)
            return {};

        while (!base.empty())
        {
            fs::path candidate = base / "Client" / "Assets" / "Resources" / "GameData";
            if (fs::exists(candidate / "Map.json", ec))
                return candidate.generic_string() + "/";
            if (base == base.parent_path())
                break;
            base = base.parent_path();
        }
        return {};
    }

    std::string ExeDir()
    {
#ifdef _WIN32
        wchar_t path[MAX_PATH];
        DWORD len = GetModuleFileNameW(nullptr, path, MAX_PATH);
        if (len == 0 || len == MAX_PATH)
            return {};
        return fs::path(path).parent_path().string();
#else
        return {};
#endif
    }
}

namespace GameDataPath
{
    const std::string& Resolve()
    {
        static const std::string cached = []() -> std::string
        {
            // 1) 작업 디렉터리 기준으로 리포의 통합 폴더 탐색
            std::error_code ec;
            fs::path cwd = fs::current_path(ec);
            if (!ec)
            {
                std::string found = FindUnifiedDir(cwd);
                if (!found.empty())
                    return found;
            }

            // 2) exe 위치 기준 탐색 (VS 테스트 러너 등 작업 디렉터리가 리포 밖인 경우)
            std::string exeDir = ExeDir();
            if (!exeDir.empty())
            {
                std::string found = FindUnifiedDir(exeDir);
                if (!found.empty())
                    return found;
            }

            // 3) 배포 레이아웃 폴백: 실행 위치 옆 GameData/
            return "GameData/";
        }();
        return cached;
    }
}
