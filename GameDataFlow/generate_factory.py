import json
from jinja2 import Environment, FileSystemLoader
import os

def _resolve_path(server_src_dir, filename):
    """Return the existing path of `filename` anywhere under server_src_dir
    (recursive), or server_src_dir/filename if it does not exist yet.

    The C++ sources were reorganised into per-table subfolders (Engine/Map/,
    Engine/GameMode/, ...). Without this lookup the generator would re-create
    flat copies at the Engine/ root, which shadow the real headers via the
    include path. By resolving to the existing location we overwrite the real
    generated files in place and never spawn shadowing stubs."""
    for root, _, files in os.walk(server_src_dir):
        if filename in files:
            return os.path.join(root, filename)
    return os.path.join(server_src_dir, filename)

def _ensure_default_class(server_src_dir, table_name):
    h_path = _resolve_path(server_src_dir, f'{table_name}.h')
    cpp_path = _resolve_path(server_src_dir, f'{table_name}.cpp')

    if not os.path.exists(h_path):
        with open(h_path, 'w', encoding='utf-8') as f:
            f.write(
                '#pragma once\n'
                '#include <vector>\n'
                '#include "syncnet_generated.h"\n'
                '\n'
                '\n'
                'namespace gamedata\n'
                '{\n'
                f'\tstruct {table_name}; // Forward declaration of gamedata::{table_name}\n'
                '}\n'
                '\n'
                f'class {table_name}\n'
                '{\n'
                'public:\n'
                f'\tconst gamedata::{table_name}* gamedata; // Pointer to gamedata for {table_name} information\n'
                '};\n'
            )

    if not os.path.exists(cpp_path):
        with open(cpp_path, 'w', encoding='utf-8') as f:
            f.write(f'#include "{table_name}.h"\n')

def _new_file_dir(server_src_dir, table_name):
    """이 테이블의 C++ 소스를 새로 만들 디렉터리.

    기존 클래스들이 테이블별 하위 폴더(Engine/Quest/, Engine/Map/ ...)에 모여 있으므로
    베이스 클래스가 있는 곳에 새 파생 클래스도 같이 둔다. 루트에 흩어지면 include 경로에서
    진짜 헤더를 가리는 사고가 난다(_resolve_path 주석 참고).

    베이스 클래스가 아직 없는 새 테이블이면 테이블 이름의 폴더를 만들어 거기 둔다 —
    루트에 두면 다음 번 생성부터는 '루트에 있다'는 이유로 계속 루트에 쌓인다."""
    base_h = _resolve_path(server_src_dir, f'{table_name}.h')
    if os.path.exists(base_h):
        return os.path.dirname(base_h)

    table_dir = os.path.join(server_src_dir, table_name)
    os.makedirs(table_dir, exist_ok=True)
    return table_dir

def _ensure_default_derived_class(server_src_dir, table_name, class_name):
    h_path = _resolve_path(server_src_dir, f'{class_name}.h')
    cpp_path = _resolve_path(server_src_dir, f'{class_name}.cpp')

    if not os.path.exists(h_path):
        h_path = os.path.join(_new_file_dir(server_src_dir, table_name), f'{class_name}.h')
    if not os.path.exists(cpp_path):
        cpp_path = os.path.join(_new_file_dir(server_src_dir, table_name), f'{class_name}.cpp')

    if not os.path.exists(h_path):
        with open(h_path, 'w', encoding='utf-8') as f:
            f.write(
                '#pragma once\n'
                f'#include "{table_name}.h"\n'
                '\n'
                f'class {class_name} : public {table_name}\n'
                '{\n'
                '};\n'
            )

    if not os.path.exists(cpp_path):
        with open(cpp_path, 'w', encoding='utf-8') as f:
            f.write(f'#include "{class_name}.h"\n')

def _has_client_class(client_cs_dir, table_name):
    target_file = f'{table_name}.cs'
    for root, _, files in os.walk(client_cs_dir):
        if target_file in files:
            return True
    return False

def _ensure_default_client_class(client_cs_dir, table_name):
    if _has_client_class(client_cs_dir, table_name):
        return

    class_dir = os.path.join(client_cs_dir, table_name)
    os.makedirs(class_dir, exist_ok=True)

    cs_path = os.path.join(class_dir, f'{table_name}.cs')
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(
            f'public class {table_name}\n'
            '{\n'
            f'    public Gamedata.{table_name} gamedata {{ get; set; }}\n'
            '}\n'
        )

def _ensure_default_derived_client_class(client_cs_dir, table_name, class_name):
    if _has_client_class(client_cs_dir, class_name):
        return

    class_dir = os.path.join(client_cs_dir, table_name)
    os.makedirs(class_dir, exist_ok=True)

    cs_path = os.path.join(class_dir, f'{class_name}.cs')
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(
            f'public class {class_name} : {table_name}\n'
            '{\n'
            '}\n'
        )

def generate_factory(server_src_dir, client_cs_dir, table_name, items):
    # code_name 추출
    code_names = sorted(set(item['code_name'] for item in items if 'code_name' in item))

    # item_infos 추출
    item_infos = sorted(set((item.get('code_name', table_name), item['id']) for item in items if 'id' in item))

    has_default_items = any('code_name' not in item for item in items)
    has_code_name_items = len(code_names) > 0

    if has_default_items:
        _ensure_default_class(server_src_dir, table_name)

    if has_default_items and table_name not in ['GameMode', 'Map']:
        _ensure_default_client_class(client_cs_dir, table_name)

    # Jinja2 환경 설정
    for code_name in code_names:
        _ensure_default_derived_class(server_src_dir, table_name, code_name)
        if table_name not in ['GameMode', 'Map']:
            _ensure_default_derived_client_class(client_cs_dir, table_name, code_name)

    env = Environment(loader=FileSystemLoader('templates'))

    # Factory.h 생성. 없으면 베이스 클래스 옆에 만든다(루트에 흩어지면 헤더를 가린다).
    factory_dir = _new_file_dir(server_src_dir, table_name)
    template_h = env.get_template('Factory.h.j2')
    h_path = _resolve_path(server_src_dir, f'{table_name}Factory.h')
    if not os.path.exists(h_path):
        h_path = os.path.join(factory_dir, f'{table_name}Factory.h')
    with open(h_path, 'w', encoding='utf-8') as f:
        f.write(template_h.render(table_name=table_name))

    # Factory.cpp 생성
    template_cpp = env.get_template('Factory.cpp.j2')
    cpp_path = _resolve_path(server_src_dir, f'{table_name}Factory.cpp')
    if not os.path.exists(cpp_path):
        cpp_path = os.path.join(factory_dir, f'{table_name}Factory.cpp')
    with open(cpp_path, 'w', encoding='utf-8') as f:
        f.write(template_cpp.render(
            table_name=table_name,
            code_names=code_names,
            has_default_items=has_default_items,
            has_code_name_items=has_code_name_items))

    # Factory.cs 생성 (클라이언트용)
    os.makedirs(client_cs_dir, exist_ok=True)
    
    # GameMode와 Map은 Factory 패턴이 아닌 데이터 매니저로 처리
    if table_name in ['GameMode', 'Map']:
        template_cs = env.get_template('DataManager.cs.j2')
    else:
        template_cs = env.get_template('Factory.cs.j2')
    
    cs_path = os.path.join(client_cs_dir, f'{table_name}Factory.cs')
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(template_cs.render(
            table_name=table_name,
            item_infos=item_infos,
            code_names=code_names,
            has_default_items=has_default_items,
            has_code_name_items=has_code_name_items))
