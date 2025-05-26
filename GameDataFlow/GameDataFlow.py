import json
import os
import shutil
import jinja2
from jsonschema import validate, ValidationError

# 콘텐츠 종류 (스키마 & 데이터쌍)
CONTENT_TYPES = [
    ("skill", "skill.schema.json"),
    ("item", "item.schema.json")
]

# 폴더 경로
SCHEMA_DIR = "../GameData/schema"
DATA_DIR = "../GameData"
OUTPUT_CLIENT_DIR = "../Client/Assets/Resources/GameData"
OUTPUT_SERVER_DIR = "../Game"

def load_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)

def ensure_directory(path):
    os.makedirs(path, exist_ok=True)

def validate_content(data, schema, content_name):
    # 스키마의 최상위 타입이 'array'인지 확인
    if schema.get("type") == "array":
        if not isinstance(data, list):
            raise ValueError(f"{content_name}.json은 배열(JSON array)이어야 합니다.")
        for i, entry in enumerate(data):
            try:
                validate(instance=entry, schema=schema.get("items", {}))
            except ValidationError as e:
                raise ValidationError(f"[{content_name}의 {i}번째 항목 오류] {e.message}")
    else:
        # 최상위 타입이 array가 아니면 전체 데이터로 검증
        try:
            validate(instance=data, schema=schema)
        except ValidationError as e:
            raise ValidationError(f"[{content_name} 오류] {e.message}")

def render_template(content_name, schema):
    # 최상위 타입이 array면 items에서, object면 바로 properties 사용
    if schema.get("type") == "array":
        properties = schema["items"]["properties"]
        struct_name = "R" + schema["items"].get("title", content_name.capitalize())
    else:
        properties = schema["properties"]
        struct_name = "R" + schema.get("title", content_name.capitalize())

    fields = []
    enums = []

    for name, prop in properties.items():
        # 타입 매핑
        if prop.get("type") == "integer":
            field_type = "int"
        elif prop.get("type") == "number":
            field_type = "double"
        elif prop.get("type") == "string":
            field_type = "std::string"
        elif "enum" in prop:
            field_type = name.capitalize()
        else:
            field_type = "std::string"  # fallback

        field = {"name": name, "type": field_type}

        # enum 필드 처리
        if "enum" in prop:
            enum_name = name.capitalize()
            enum_values = prop["enum"]
            # enum 중복 방지
            if not any(e["name"] == enum_name for e in enums):
                enums.append({"name": enum_name, "values": enum_values})
            field["enum"] = {"name": enum_name, "values": enum_values}

        fields.append(field)

    env = jinja2.Environment(loader=jinja2.FileSystemLoader("template"))
    tmpl = env.get_template("class.hpp.jinja2")

    output = tmpl.render(
        struct_name=struct_name,
        fields=fields,
        enums=enums
    )

    filename = os.path.join(OUTPUT_SERVER_DIR, f"{struct_name}.hpp")
    with open(filename, "w") as f:
        f.write(output)

def process_content(content_name, schema_file):
    print(f"\n🔍 {content_name} 처리 중...")

    schema_path = os.path.join(SCHEMA_DIR, schema_file)
    data_path = os.path.join(DATA_DIR, f"{content_name}.json")
    output_client = os.path.join(OUTPUT_CLIENT_DIR, f"{content_name}.json")
    output_server = os.path.join(OUTPUT_SERVER_DIR, f"{content_name}.json")

    try:
        schema = load_json(schema_path)
        data = load_json(data_path)

        validate_content(data, schema, content_name)
        print(f"✅ {content_name}.json: 유효성 검사 통과")

        ensure_directory(OUTPUT_CLIENT_DIR)
        ensure_directory(OUTPUT_SERVER_DIR)

        shutil.copy(data_path, output_client)
        shutil.copy(data_path, output_server)
        print(f"📁 {content_name}.json 클라이언트/서버 폴더에 복사 완료")

        render_template(content_name, schema)
        print(f"📝 {content_name}.hpp 서버 폴더에 생성 완료")

    except FileNotFoundError as e:
        print(f"❌ 파일 누락: {e.filename}")
    except ValidationError as e:
        print(f"❌ 유효성 오류: {e}")
    except Exception as e:
        print(f"❌ 기타 오류: {e}")

def main():
    print("📦 MMORPG 콘텐츠 유효성 검사 및 배포 스크립트 시작")
    for content_name, schema_file in CONTENT_TYPES:
        process_content(content_name, schema_file)
    print("\n🏁 완료!")

if __name__ == "__main__":
    main()