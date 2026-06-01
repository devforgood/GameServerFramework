import json
from jinja2 import Environment, FileSystemLoader
import os

def _ensure_default_class(server_src_dir, table_name):
    h_path = os.path.join(server_src_dir, f'{table_name}.h')
    cpp_path = os.path.join(server_src_dir, f'{table_name}.cpp')

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
                f'\tclass {table_name}; // Forward declaration of gamedata::{table_name}\n'
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
    env = Environment(loader=FileSystemLoader('templates'))

    # Factory.h 생성
    template_h = env.get_template('Factory.h.j2')
    h_path = os.path.join(server_src_dir, f'{table_name}Factory.h')
    with open(h_path, 'w', encoding='utf-8') as f:
        f.write(template_h.render(table_name=table_name))

    # Factory.cpp 생성
    template_cpp = env.get_template('Factory.cpp.j2')
    cpp_path = os.path.join(server_src_dir, f'{table_name}Factory.cpp')
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
