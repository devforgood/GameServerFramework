import json
from jinja2 import Environment, FileSystemLoader
import os

def generate_skill_factory(server_src_dir, client_cs_dir, skills):
    # code_name 추출
    code_names = sorted(set(skill['code_name'] for skill in skills if 'code_name' in skill))

    # skill_infos 추출
    skill_infos = sorted(set((skill['code_name'], skill['id']) for skill in skills if 'code_name' in skill and 'id' in skill))

    # Jinja2 환경 설정
    env = Environment(loader=FileSystemLoader('templates'))

    # SkillFactory.h 생성
    template_h = env.get_template('SkillFactory.h.j2')
    h_path = os.path.join(server_src_dir, 'SkillFactory.h')
    with open(h_path, 'w', encoding='utf-8') as f:
        f.write(template_h.render())

    # SkillFactory.cpp 생성
    template_cpp = env.get_template('SkillFactory.cpp.j2')
    cpp_path = os.path.join(server_src_dir, 'SkillFactory.cpp')
    with open(cpp_path, 'w', encoding='utf-8') as f:
        f.write(template_cpp.render(skill_infos=skill_infos))

    # SkillFactory.cs 생성 (클라이언트용)
    os.makedirs(client_cs_dir, exist_ok=True)
    template_cs = env.get_template('SkillFactory.cs.j2')
    cs_path = os.path.join(client_cs_dir, 'SkillFactory.cs')
    with open(cs_path, 'w', encoding='utf-8') as f:
        f.write(template_cs.render(skill_infos=skill_infos))