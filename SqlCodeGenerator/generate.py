import os
import shutil  # shutil 모듈 import 추가
import xml.etree.ElementTree as ET
from jinja2 import Environment, FileSystemLoader

MYSQL_TO_CPP = {
    "INT": "int",
    "BIGINT": "long long",
    "VARCHAR": "std::string",
    "TEXT": "std::string",
    "FLOAT": "float",
    "DOUBLE": "double",
    "DATETIME": "std::string",
}

CPP_SET_FUNC = {
    "int": "Int",
    "long long": "Int64",
    "float": "Double",
    "double": "Double",
    "std::string": "String"
}

def map_cpp_type(mysql_type):
    for key in MYSQL_TO_CPP:
        if key in mysql_type:
            return MYSQL_TO_CPP[key]
    return "std::string"

def map_cpp_set_func(cpp_type):
    return CPP_SET_FUNC.get(cpp_type, "String")

def parse_schema(xml_file):
    tree = ET.parse(xml_file)
    root = tree.getroot()
    tables = []
    for table_node in root.findall('table'):
        table = {
            "name": table_node.get("name"),
            "class_name": table_node.get("class_name") or table_node.get("name").capitalize() + "DAO",
            "file_name": table_node.get("file_name") or table_node.get("name").lower() + "_dao",
            "columns": [],
            "primary_key": None
        }
        for col_node in table_node.findall('column'):
            col = {
                "name": col_node.get("name"),
                "type": col_node.get("type"),
                "auto_increment": col_node.get("auto_increment") == "true",
                "nullable": col_node.get("nullable", "true") == "true",
                "default": col_node.get("default"),
            }
            col["cpp_type"] = map_cpp_type(col["type"])
            col["cpp_set_func"] = map_cpp_set_func(col["cpp_type"])
            table["columns"].append(col)
        pk_name = table_node.find('primary_key').text
        table["primary_key"] = next((c for c in table["columns"] if c["name"] == pk_name), None)
        tables.append(table)
    return tables

def render_templates(tables):
    env = Environment(loader=FileSystemLoader("templates"), trim_blocks=True, lstrip_blocks=True)

        # 파일을 생성하기전에 삭제한다.
    if os.path.exists("generated"):
        shutil.rmtree("generated")


    os.makedirs("generated", exist_ok=True)

    create_sqls = []

    # 첫 번째 파일인지 확인하기 위한 플래그
    is_first_file = True

    for table in tables:

        # Header
        with open(f"generated/{table['file_name']}.h", "a") as f:
            f.write(env.get_template("dao.h.j2").render(class_name=table["class_name"], table=table, include_header=is_first_file))

        # CPP
        with open(f"generated/{table['file_name']}.cpp", "a") as f:
            f.write(env.get_template("dao.cpp.j2").render(class_name=table["class_name"], table=table, include_header=is_first_file))

        # CREATE TABLE SQL
        create_sqls.append(env.get_template("create_table.sql.j2").render(table=table))

        is_first_file = False  # 첫 번째 파일 생성 후 플래그 변경


    with open("generated/create_tables.sql", "w") as f:
        f.write("\n\n".join(create_sqls))

if __name__ == "__main__":
    tables = parse_schema("schema.xml")
    render_templates(tables)