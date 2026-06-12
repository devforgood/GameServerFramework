import json
import os
import shutil

from schema_infer import build_structs
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

# Source data
GAMEDATA_DIR = "../GameData"

# Generated C++ data model + loader (built into the Engine static lib)
ENGINE_GAMEDATA_DIR = "../Engine/GameData/"
# Hand-written-by-default C++ classes + generated factories
SERVER_SRC_DIR = "../Engine/"
# Generated C# factories (Unity client)
CLIENT_SRC_DIR = "../Client/Assets/Scripts/"
# Generated C# data model (Unity client)
CLIENT_MODEL_PATH = "../Client/Assets/Scripts/GameData/Gamedata.cs"

# JSON data is copied verbatim into each consumer's runtime GameData folder
DATA_COPY_DIRS = [
    "../Client/Assets/Resources/GameData/",
    "../Game/GameData/",
    "../UnitTest/GameData/",
]

# GameData에서 관리되는 lua 스크립트
LUA_SCRIPTS = [
    "../GameData/behavior_tree.lua",
    "../GameData/mob.lua",
]
LUA_COPY_DIRS = ["../Game/GameData/", "../UnitTest/GameData/"]


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
    seen_struct_names = set()
    table_names = []          # top-level table class names (for C# list wrappers)

    for table in tables:
        table_name = table["name"]
        data = load_table_data(table["json_path"])
        if data is None:
            continue

        # Infer the data model from the JSON itself.
        try:
            structs = build_structs(data, table_name)
        except Exception as e:
            print(f"{RED}[ERROR] schema inference failed for {table_name}: {e}{RESET}")
            continue

        for s in structs:
            if s['name'] not in seen_struct_names:
                seen_struct_names.add(s['name'])
                all_structs.append(s)
        table_names.append(table_name)

        # Factory / base-class code generation (C++ + C#).
        try:
            generate_factory(SERVER_SRC_DIR, CLIENT_SRC_DIR, table_name, data)
            print(f"{GREEN}[OK] Generated factory for {table_name}{RESET}")
        except Exception as e:
            print(f"{RED}[ERROR] Failed to generate factory for {table_name}: {e}{RESET}")

        # Copy the JSON verbatim into each runtime data folder.
        for target_dir in DATA_COPY_DIRS:
            try:
                os.makedirs(target_dir, exist_ok=True)
                shutil.copy2(os.path.join(GAMEDATA_DIR, table["output_file"]),
                             os.path.join(target_dir, table["output_file"]))
                print(f"{GREEN}[OK] {table['output_file']} copied to {target_dir}{RESET}")
            except Exception as e:
                print(f"{RED}[ERROR] {table['output_file']} copy to {target_dir} failed: {e}{RESET}")

    # C++ data model + loader
    try:
        generate_gamedata_header(ENGINE_GAMEDATA_DIR, all_structs)
        generate_resource_loader(ENGINE_GAMEDATA_DIR, tables)
        print(f"{GREEN}[OK] Generated C++ gamedata.h + ResourceLoader{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] Failed to generate C++ model/loader: {e}{RESET}")

    # C# data model (Unity client)
    try:
        generate_csharp_model(CLIENT_MODEL_PATH, all_structs, table_names)
        print(f"{GREEN}[OK] Generated C# Gamedata model{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] Failed to generate C# model: {e}{RESET}")

    # lua 스크립트 복사
    for target_dir in LUA_COPY_DIRS:
        os.makedirs(target_dir, exist_ok=True)
        for lua_path in LUA_SCRIPTS:
            try:
                shutil.copy2(lua_path, os.path.join(target_dir, os.path.basename(lua_path)))
                print(f"{GREEN}[OK] {os.path.basename(lua_path)} copied to {target_dir}{RESET}")
            except Exception as e:
                print(f"{RED}[ERROR] {lua_path} copy to {target_dir} failed: {e}{RESET}")


if __name__ == "__main__":
    main()
