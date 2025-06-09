import json
from google.protobuf import json_format
from generate_factory import generate_factory
import gamedata_pb2
import shutil
import os

# ANSI 이스케이프 코드
RED = '\033[91m'
GREEN = '\033[92m'
RESET = '\033[0m'

# 변환할 JSON 파일 목록 및 정보
JSON_PROTO_MAP = [
    {
        "json_path": "../GameData/skill.json",
        "pb_cls": gamedata_pb2.SkillList,
        "repeated_field": "skills",
        "out_path": "skill.bytes",
        "table_name": "Skill"
    },
    {
        "json_path": "../GameData/item.json",
        "pb_cls": gamedata_pb2.ItemList,
        "repeated_field": "items",
        "out_path": "item.bytes",
        "table_name": "Item"
    }
]

# 복사 대상 폴더
CLIENT_DIR = "../Client/Assets/Resources/GameData/"
SERVER_DIR = "../Game/GameData/"
SERVER_SRC_DIR = "../Game/"
CLIENT_SRC_DIR = "../Client/Assets/Scripts/"

def convert_json_to_protobuf(json_path, pb_cls, repeated_field, out_path, table_name):
    try:
        with open(json_path, encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"{RED}[ERROR] {json_path} file read/parse error: {e}{RESET}")
        return

    pb_list = pb_cls()
    try:
        for entry in data:
            json_format.ParseDict(entry, getattr(pb_list, repeated_field).add())
        with open(out_path, "wb") as f:
            f.write(pb_list.SerializeToString())
        print(f"{GREEN}[OK] {json_path} -> {out_path} conversion complete{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] {json_path} -> {out_path} conversion error: {e}{RESET}")
        return

    # Factory 코드 생성
    try:
        generate_factory(SERVER_SRC_DIR, CLIENT_SRC_DIR, table_name, data)
        print(f"{GREEN}[OK] Generated factory for {table_name}{RESET}")
    except Exception as e:
        print(f"{RED}[ERROR] Failed to generate factory for {table_name}: {e}{RESET}")
        return

    # 바이너리 파일 복사
    for target_dir in [CLIENT_DIR, SERVER_DIR]:
        try:
            os.makedirs(target_dir, exist_ok=True)
            shutil.copy2(out_path, os.path.join(target_dir, out_path))
            print(f"{GREEN}[OK] {out_path} copied to {target_dir}{RESET}")
        except Exception as e:
            print(f"{RED}[ERROR] {out_path} copy to {target_dir} failed: {e}{RESET}")

if __name__ == "__main__":
    for info in JSON_PROTO_MAP:
        convert_json_to_protobuf(
            json_path=info["json_path"],
            pb_cls=info["pb_cls"],
            repeated_field=info["repeated_field"],
            out_path=info["out_path"],
            table_name=info["table_name"]
        )

