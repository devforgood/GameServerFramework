import json
from google.protobuf import json_format
from generate_skill_factory import generate_skill_factory
import gamedata_pb2
import shutil
import os

# 변환할 JSON 파일 목록 및 정보
JSON_PROTO_MAP = [
    {
        "json_path": "../GameData/skill.json",
        "pb_cls": gamedata_pb2.SkillList,
        "repeated_field": "skills",
        "out_path": "skill.bytes"
    },
    {
        "json_path": "../GameData/item.json",
        "pb_cls": gamedata_pb2.ItemList,
        "repeated_field": "items",
        "out_path": "item.bytes"
    }
]

# 복사 대상 폴더
CLIENT_DIR = "../Client/Assets/Resources/GameData/"
SERVER_DIR = "../Game/GameData/"
SERVER_SRC_DIR = "../Game/"
CLIENT_SRC_DIR = "../Client/Assets/Scripts/"

def convert_json_to_protobuf(json_path, pb_cls, repeated_field, out_path):
    try:
        with open(json_path, encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        print(f"[ERROR] {json_path} file read/parse error: {e}")
        return

    pb_list = pb_cls()
    try:
        for entry in data:
            json_format.ParseDict(entry, getattr(pb_list, repeated_field).add())
        with open(out_path, "wb") as f:
            f.write(pb_list.SerializeToString())
        print(f"[OK] {json_path} -> {out_path} conversion complete")
    except Exception as e:
        print(f"[ERROR] {json_path} -> {out_path} conversion error: {e}")
        return

    # 스킬 테이블이라면 목록을 넘겨 코드를 생성
    if os.path.basename(json_path) == "skill.json":
        generate_skill_factory(SERVER_SRC_DIR, CLIENT_SRC_DIR, data)

    # 바이너리 파일 복사
    for target_dir in [CLIENT_DIR, SERVER_DIR]:
        try:
            os.makedirs(target_dir, exist_ok=True)
            shutil.copy2(out_path, os.path.join(target_dir, out_path))
            print(f"[OK] {out_path} copied to {target_dir}")
        except Exception as e:
            print(f"[ERROR] {out_path} copy to {target_dir} failed: {e}")

if __name__ == "__main__":
    for info in JSON_PROTO_MAP:
        convert_json_to_protobuf(
            json_path=info["json_path"],
            pb_cls=info["pb_cls"],
            repeated_field=info["repeated_field"],
            out_path=info["out_path"]
        )

