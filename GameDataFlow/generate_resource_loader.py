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


def generate_resource_loader(out_dir, tables):
    """Write the C++ ResourceLoader (.h/.cpp) that loads the *.json tables."""
    os.makedirs(out_dir, exist_ok=True)
    env = _env()

    render_tables = [{
        'name': t['name'],
        'map_name': t['repeated_field'],
        'output_file': t['output_file'],
    } for t in tables if t.get('enabled', True)]

    template_h = env.get_template('ResourceLoader.h.j2')
    with open(os.path.join(out_dir, 'ResourceLoader.h'), 'w', encoding='utf-8') as f:
        f.write(template_h.render(tables=render_tables))

    template_cpp = env.get_template('ResourceLoader.cpp.j2')
    with open(os.path.join(out_dir, 'ResourceLoader.cpp'), 'w', encoding='utf-8') as f:
        f.write(template_cpp.render(tables=render_tables))


def generate_csharp_model(out_path, structs, table_names):
    """Write the plain C# data model (Gamedata.cs) for the Unity client."""
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    template = _env().get_template('GameData.cs.j2')
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(template.render(structs=structs, tables=table_names))
