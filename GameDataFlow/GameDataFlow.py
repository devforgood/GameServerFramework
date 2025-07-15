import json
from google.protobuf import json_format
from generate_factory import generate_factory
import gamedata_pb2
import shutil
import os

# ANSI escape codes
RED = '\033[91m'
GREEN = '\033[92m'
RESET = '\033[0m'

# Protobuf class mapping
PB_CLASS_MAP = {
    "SkillList": gamedata_pb2.SkillList,
    "ItemList": gamedata_pb2.ItemList,
    "QuestList": gamedata_pb2.QuestList  # Future use
}

def load_table_meta():
    """Load table meta information from JSON file"""
    try:
        with open("../GameData/table_meta.json", encoding="utf-8") as f:
            meta_data = json.load(f)
        return meta_data
    except Exception as e:
        print(f"{RED}[ERROR] Failed to load table_meta.json: {e}{RESET}")
        return None

def initialize_json_proto_map():
    """Initialize JSON_PROTO_MAP from meta table"""
    meta_data = load_table_meta()
    if not meta_data:
        return []
    
    json_proto_map = []
    for table in meta_data.get("tables", []):
        # Skip disabled tables
        if table.get("enabled", True) == False:
            continue
            
        pb_class_name = table.get("pb_class")
        if pb_class_name not in PB_CLASS_MAP:
            print(f"{RED}[WARNING] Unknown protobuf class: {pb_class_name}{RESET}")
            continue
            
        json_proto_map.append({
            "json_path": f"../GameData/{table['json_path']}",
            "pb_cls": PB_CLASS_MAP[pb_class_name],
            "repeated_field": table["repeated_field"],
            "out_path": table["output_file"],
            "table_name": table["name"]
        })
    
    return json_proto_map

# Initialize JSON_PROTO_MAP from meta table
JSON_PROTO_MAP = initialize_json_proto_map()

# 복사 대상 폴더
CLIENT_DIR = "../Client/Assets/Resources/GameData/"
SERVER_DIR = "../Game/GameData/"
SERVER_SRC_DIR = "../Game/"
CLIENT_SRC_DIR = "../Client/Assets/Scripts/"

class GameDataProcessor:

    def convert_json_to_protobuf(self, json_path, pb_cls, repeated_field, out_path, table_name):
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

def main():
    processor = GameDataProcessor()
    for info in JSON_PROTO_MAP:
        processor.convert_json_to_protobuf(
            json_path=info["json_path"],
            pb_cls=info["pb_cls"],
            repeated_field=info["repeated_field"],
            out_path=info["out_path"],
            table_name=info["table_name"]
        )

if __name__ == "__main__":
    main()

