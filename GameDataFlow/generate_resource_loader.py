from jinja2 import Environment, FileSystemLoader
import os


def _env():
    return Environment(loader=FileSystemLoader('templates'))


def generate_gamedata_header(out_dir, structs):
    """Write the plain C++ data model (gamedata.h) from the inferred structs."""
    os.makedirs(out_dir, exist_ok=True)
    template = _env().get_template('gamedata.h.j2')
    path = os.path.join(out_dir, 'gamedata.h')
    with open(path, 'w', encoding='utf-8') as f:
        f.write(template.render(structs=structs))


def generate_resource_loader(out_dir, tables, indexes):
    """Write the C++ ResourceLoader (.h/.cpp) that loads the *.json tables.

    indexes: id 를 가진 중첩 오브젝트의 인덱스 서술자(schema_infer.build_structs 가 만든다).
    """
    os.makedirs(out_dir, exist_ok=True)
    env = _env()

    render_tables = [{
        'name': t['name'],
        'map_name': t['repeated_field'],
        'output_file': t['output_file'],
    } for t in tables if t.get('enabled', True)]

    storage_of = {t['name']: t['map_name'] for t in render_tables}
    render_indexes = []
    for index in indexes:
        entry = dict(index)
        entry['table_map_name'] = storage_of[index['table']]
        render_indexes.append(entry)

    template_h = env.get_template('ResourceLoader.h.j2')
    with open(os.path.join(out_dir, 'ResourceLoader.h'), 'w', encoding='utf-8') as f:
        f.write(template_h.render(tables=render_tables, indexes=render_indexes))

    template_cpp = env.get_template('ResourceLoader.cpp.j2')
    with open(os.path.join(out_dir, 'ResourceLoader.cpp'), 'w', encoding='utf-8') as f:
        f.write(template_cpp.render(tables=render_tables, indexes=render_indexes))


def generate_csharp_model(out_path, structs, table_names):
    """Write the plain C# data model (Gamedata.cs) for the Unity client."""
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    template = _env().get_template('GameData.cs.j2')
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(template.render(structs=structs, tables=table_names))


def generate_csharp_resource_loader(out_path, tables):
    """Write the generated half of the Unity ResourceLoader (partial class).

    클라이언트 팩토리는 ResourceLoader.{Table}s 사전을 참조한다. 그 사전이 손으로
    관리되던 동안에는 table_meta.json 에 테이블을 추가할 때마다 팩토리만 생성되고
    사전은 빠져서 클라이언트가 컴파일되지 않았다(CS1061). 서버(C++) 로더와 마찬가지로
    테이블 목록에서 직접 만들어 둘이 어긋날 수 없게 한다.
    """
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    render_tables = []
    for t in tables:
        if not t.get('enabled', True):
            continue
        name = t['name']
        stem = os.path.splitext(t['output_file'])[0]
        render_tables.append({
            'name': name,
            # Resources.LoadAsync 는 확장자 없는 경로를 받는다. 파일 이름의 대소문자를
            # 그대로 써야 한다 — Map.json 은 "GameData/Map", skill.json 은 "GameData/skill".
            'resource_path': 'GameData/' + stem,
            'var': name[0].lower() + name[1:],
        })

    template = _env().get_template('ResourceLoader.Tables.cs.j2')
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(template.render(tables=render_tables))
