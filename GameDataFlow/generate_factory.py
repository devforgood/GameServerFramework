import json
from jinja2 import Environment, FileSystemLoader
import os

def generate_factory(server_src_dir, client_cs_dir, table_name, items):
    # code_name 추출
    code_names = sorted(set(item['code_name'] for item in items if 'code_name' in item))

    # item_infos 추출
    item_infos = sorted(set((item['code_name'], item['id']) for item in items if 'code_name' in item and 'id' in item))

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
        f.write(template_cpp.render(table_name=table_name, item_infos=item_infos))

    # Factory.cs 생성 (클라이언트용)
    os.makedirs(client_cs_dir, exist_ok=True)
    template_cs = env.get_template('Factory.cs.j2')
    cs_path = os.path.join(client_cs_dir, f'{table_name}Factory.cs')
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(template_cs.render(table_name=table_name, item_infos=item_infos))
