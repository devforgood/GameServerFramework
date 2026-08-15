# -*- coding: utf-8 -*-
import os
import shutil  
import xml.etree.ElementTree as ET
from jinja2 import Environment, FileSystemLoader

MYSQL_TO_CPP = {
    "INT": "int",
    "BIGINT": "long long",
    "VARCHAR": "std::string",
    "TEXT": "std::string",
    "FLOAT": "float",
    "DOUBLE": "double",
    "DATETIME": "std::chrono::system_clock::time_point",
}

CPP_SET_FUNC = {
    "int": "Int",
    "long long": "Int64",
    "float": "Double",
    "double": "Double",
    "std::string": "String",
    "std::chrono::system_clock::time_point": "String"
}

def map_cpp_type(mysql_type):
    # 긴 이름부터 본다. "INT" 가 "BIGINT" 의 부분 문자열이라 짧은 것부터 맞추면
    # BIGINT 컬럼이 int 로 생성되어 64비트 값이 조용히 잘린다.
    for key in sorted(MYSQL_TO_CPP, key=len, reverse=True):
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
        class_name = table_node.get("class_name") or table_node.get("name").capitalize() + "DAO"
        vo_class_name = class_name.replace("DAO", "VO") if class_name.endswith("DAO") else table_node.get("name").capitalize() + "VO"
        table = {
            "name": table_node.get("name"),
            "class_name": class_name,
            "vo_class_name": vo_class_name,
            "file_name": table_node.get("file_name") or table_node.get("name").lower() + "_dao",
            "columns": [],
            "primary_key": None,
            "unique_keys": [],  
            "indexes": []  
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
            col["is_datetime"] = col["cpp_type"] == "std::chrono::system_clock::time_point"
            table["columns"].append(col)
        
        pk_node = table_node.find('primary_key')
        if pk_node is not None:
            pk_names = [name.strip() for name in pk_node.text.split(",")]
            table["primary_key"] = [c for c in table["columns"] if c["name"] in pk_names]
            for col in table["columns"]:
                col["is_pk"] = col["name"] in pk_names
        else:
            table["primary_key"] = []
            for col in table["columns"]:
                col["is_pk"] = False

        for unique_key_node in table_node.findall('unique_key'):
            unique_key_columns = unique_key_node.text.split(",") 
            table["unique_keys"].append([col.strip() for col in unique_key_columns]) 

        for index_node in table_node.findall('index'):
            index_columns = index_node.text.split(",") 
            table["indexes"].append([col.strip() for col in index_columns])  


        tables.append(table)
    return tables

def render_templates(tables):
    env = Environment(loader=FileSystemLoader("templates"), trim_blocks=True, lstrip_blocks=True)

    pathname = "../Engine/SQL/generated"

    if os.path.exists(pathname):
        shutil.rmtree(pathname)


    os.makedirs(pathname, exist_ok=True)

    create_sqls = []

    is_first_file = True

    for table in tables:

        with open(f"{pathname}/vo.h", "a", encoding="utf-8") as f:
            f.write(env.get_template("vo.h.j2").render(table=table, include_header=is_first_file))

        with open(f"{pathname}/{table['file_name']}.h", "a", encoding="utf-8") as f:
            f.write(env.get_template("dao.h.j2").render(class_name=table["class_name"], table=table, include_header=is_first_file))

        with open(f"{pathname}/{table['file_name']}.cpp", "a", encoding="utf-8") as f:
            f.write(env.get_template("dao.cpp.j2").render(class_name=table["class_name"], table=table, include_header=is_first_file))

        # 기동 시 실제 컬럼과 대조할 목록(빠진 컬럼만 ADD COLUMN 한다).
        with open(f"{pathname}/schema_columns.h", "a", encoding="utf-8") as f:
            f.write(env.get_template("schema_columns.h.j2").render(table=table, include_header=is_first_file))

        create_sqls.append(env.get_template("create_table.sql.j2").render(table=table))

        is_first_file = False

    with open(f"{pathname}/schema_columns.h", "a", encoding="utf-8") as f:
        f.write("};\n")

    create_sql_text = "\n\n".join(create_sqls)

    # create_tables.sql 은 런타임에 작업 디렉터리 기준 SQL/generated/ 에서 읽으므로
    # Engine(빌드 산출물)과 Game(실행 작업 디렉터리) 양쪽에 동일하게 써준다.
    sql_out_dirs = [pathname, "../Game/SQL/generated"]
    for out_dir in sql_out_dirs:
        os.makedirs(out_dir, exist_ok=True)
        with open(f"{out_dir}/create_tables.sql", "w", encoding="utf-8") as f:
            f.write(create_sql_text)

if __name__ == "__main__":
    tables = parse_schema("schema.xml")
    render_templates(tables)