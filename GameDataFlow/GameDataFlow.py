import json
import os
import sys

from schema_infer import build_structs
from validate_data import validate_table
from generate_factory import generate_factory
from generate_resource_loader import (
    generate_gamedata_header,
    generate_resource_loader,
    generate_csharp_model,
)

# ANSI escape codes
RED = '\033[91m'
GREEN = '\033[92m'
RESET = '\033[0m'

# Source data — 단일 리소스 위치.
# 모든 소비자(Unity 클라이언트, Game 서버, UnitTest, Benchmark)가 이 폴더 하나를 읽는다.
# (C++ 쪽은 GameDataPath::Resolve() 가 리포 안에서 이 경로를 찾아낸다. 복사본 없음)
GAMEDATA_DIR = "../Client/Assets/Resources/GameData"

# Generated C++ data model + loader (built into the Engine static lib)
ENGINE_GAMEDATA_DIR = "../Engine/GameData/"
# Hand-written-by-default C++ classes + generated factories
SERVER_SRC_DIR = "../Engine/"
# Generated C# factories (Unity client)
CLIENT_SRC_DIR = "../Client/Assets/Scripts/"
# Generated C# data model (Unity client)
CLIENT_MODEL_PATH = "../Client/Assets/Scripts/GameData/Gamedata.cs"


def load_table_meta():
    try:
        with open(os.path.join(GAMEDATA_DIR, "table_meta.json"), encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"{RED}[ERROR] Failed to load table_meta.json: {e}{RESET}")
        return None


def load_table_data(json_path):
    try:
        with open(os.path.join(GAMEDATA_DIR, json_path), encoding="utf-8") as f:
            return json.load(f)
    except Exception as e:
        print(f"{RED}[ERROR] {json_path} read/parse error: {e}{RESET}")
        return None


def main():
    meta = load_table_meta()
    if not meta:
        return

    tables = [t for t in meta.get("tables", []) if t.get("enabled", True)]

    all_structs = []          # combined C++/C# model structs (across all tables)
    all_indexes = []          # id 를 가진 중첩 오브젝트의 인덱스 서술자
    seen_struct_names = set()
    table_names = []          # top-level table class names (for C# list wrappers)

    had_error = False

    for table in tables:
        table_name = table["name"]
        data = load_table_data(table["json_path"])
        if data is None:
            had_error = True
            continue

        # 코드 생성 전에 데이터를 정적 검증한다(중복 id, 필드명 오타 등).
        # 오타 필드는 스키마 추론을 그대로 통과해 모델까지 전파되므로 여기서 차단한다.
        validation_errors = validate_table(table_name, data)
        if validation_errors:
            for err in validation_errors:
                print(f"{RED}[ERROR] {err}{RESET}")
            print(f"{RED}[ERROR] {table_name}: 데이터 검증 실패로 코드 생성을 건너뜁니다{RESET}")
            had_error = True
            continue

        # Infer the data model from the JSON itself.
        try:
            structs, indexes = build_structs(data, table_name)
        except Exception as e:
            print(f"{RED}[ERROR] schema inference failed for {table_name}: {e}{RESET}")
            continue

        for s in structs:
            if s['name'] not in seen_struct_names:
                seen_struct_names.add(s['name'])
                all_structs.append(s)
        all_indexes.extend(indexes)
        table_names.append(table_name)

        # Factory / base-class code generation (C++ + C#).
        try:
            generate_factory(SERVER_SRC_DIR, CLIENT_SRC_DIR, table_name, data)
            print(f"{GREEN}[OK] Generated factory for {table_name}{RESET}")
        except Exception as e:
            print(f"{RED}[ERROR] Failed to generate factory for {table_name}: {e}{RESET}")

    # C++ data model + loader
    try:
        generate_gamedata_header(ENGINE_GAMEDATA_DIR, all_structs)
        generate_resource_loader(ENGINE_GAMEDATA_DIR, tables, all_indexes)
        print(f"{GREEN}[OK] Generated C++ gamedata.h + ResourceLoader{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] Failed to generate C++ model/loader: {e}{RESET}")

    # C# data model (Unity client)
    try:
        generate_csharp_model(CLIENT_MODEL_PATH, all_structs, table_names)
        print(f"{GREEN}[OK] Generated C# Gamedata model{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] Failed to generate C# model: {e}{RESET}")

    # 검증/로드 실패가 있으면 빌드 파이프라인이 실패하도록 종료 코드로 알린다.
    if had_error:
        sys.exit(1)


if __name__ == "__main__":
    main()
