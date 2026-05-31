from jinja2 import Environment, FileSystemLoader
import os


def _protobuf_class_name(pb_class):
    if pb_class.startswith("gamedata_pb2."):
        return pb_class.replace("gamedata_pb2.", "")
    return pb_class


def generate_resource_loader(protobuf_src_dir, tables):
    env = Environment(loader=FileSystemLoader('templates'))
    render_tables = []

    for table in tables:
        if table.get("enabled", True) == False:
            continue

        render_tables.append({
            "name": table["name"],
            "list_type": _protobuf_class_name(table["pb_class"]),
            "repeated_field": table["repeated_field"],
            "output_file": table["output_file"],
            "map_name": table["repeated_field"],
        })

    os.makedirs(protobuf_src_dir, exist_ok=True)

    template_h = env.get_template('ResourceLoader.h.j2')
    h_path = os.path.join(protobuf_src_dir, 'ResourceLoader.h')
    with open(h_path, 'w', encoding='utf-8') as f:
        f.write(template_h.render(tables=render_tables))

    template_cpp = env.get_template('ResourceLoader.cpp.j2')
    cpp_path = os.path.join(protobuf_src_dir, 'ResourceLoader.cpp')
    with open(cpp_path, 'w', encoding='utf-8') as f:
        f.write(template_cpp.render(tables=render_tables))
