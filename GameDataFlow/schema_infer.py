"""Infer a C++/C# data model from raw GameData JSON.

Each table's JSON is a top-level array of entries. We deep-union every entry into a
single type tree, then flatten that tree into a list of struct descriptors that the
code templates render into plain C++ structs and C# classes.

Node shapes (plain dicts):
    {'kind': 'scalar', 'ctype': 'bool'|'int'|'double'|'string', 'nullable': bool}
    {'kind': 'array',  'element': node, 'nullable': bool}
    {'kind': 'object', 'fields': {name: node}, 'name': str, 'nullable': bool}
    {'kind': 'unknown', 'nullable': bool}   # from JSON null or an empty array
"""


def _pascal(s):
    return ''.join(part.capitalize() for part in s.split('_') if part)


def _snake(name):
    """PascalCase -> snake_case (MapSpawnPointsPlayerSpawn -> map_spawn_points_player_spawn)."""
    out = []
    for i, ch in enumerate(name):
        if ch.isupper() and i > 0:
            out.append('_')
        out.append(ch.lower())
    return ''.join(out)


def _plural(snake):
    return snake + ('es' if snake.endswith(('s', 'x', 'z', 'ch', 'sh')) else 's')


def _singular(name):
    # Strip a trailing plural 's' from a derived struct name (Gates -> Gate).
    return name[:-1] if name.endswith('s') and not name.endswith('ss') else name


def _unify_scalar(t1, t2):
    if t1 == t2:
        return t1
    nums = {'int', 'double'}
    if t1 in nums and t2 in nums:
        return 'double'
    # Unexpected mix (e.g. string vs number) -> fall back to string.
    return 'string'


def infer(value):
    """Infer a type node from a single JSON value."""
    if value is None:
        return {'kind': 'unknown', 'nullable': True}
    # bool must be checked before int (bool is a subclass of int in Python).
    if isinstance(value, bool):
        return {'kind': 'scalar', 'ctype': 'bool', 'nullable': False}
    if isinstance(value, int):
        return {'kind': 'scalar', 'ctype': 'int', 'nullable': False}
    if isinstance(value, float):
        return {'kind': 'scalar', 'ctype': 'double', 'nullable': False}
    if isinstance(value, str):
        return {'kind': 'scalar', 'ctype': 'string', 'nullable': False}
    if isinstance(value, list):
        element = None
        for item in value:
            element = merge(element, infer(item))
        if element is None:
            element = {'kind': 'unknown', 'nullable': False}
        return {'kind': 'array', 'element': element, 'nullable': False}
    if isinstance(value, dict):
        return {'kind': 'object',
                'fields': {k: infer(v) for k, v in value.items()},
                'nullable': False}
    raise TypeError(f"Unsupported JSON value: {value!r}")


def merge(a, b):
    """Unify two type nodes into one that describes both."""
    if a is None:
        return b
    if b is None:
        return a

    if a['kind'] == 'unknown':
        out = dict(b)
        out['nullable'] = b.get('nullable', False) or a.get('nullable', False)
        return out
    if b['kind'] == 'unknown':
        out = dict(a)
        out['nullable'] = a.get('nullable', False) or b.get('nullable', False)
        return out

    nullable = a.get('nullable', False) or b.get('nullable', False)

    if a['kind'] == 'scalar' and b['kind'] == 'scalar':
        return {'kind': 'scalar',
                'ctype': _unify_scalar(a['ctype'], b['ctype']),
                'nullable': nullable}

    if a['kind'] == 'array' and b['kind'] == 'array':
        return {'kind': 'array',
                'element': merge(a['element'], b['element']),
                'nullable': nullable}

    if a['kind'] == 'object' and b['kind'] == 'object':
        fields = {}
        for key in set(a['fields']) | set(b['fields']):
            fields[key] = merge(a['fields'].get(key), b['fields'].get(key))
        return {'kind': 'object', 'fields': fields, 'nullable': nullable}

    # Structural mismatch (object vs scalar, array vs scalar, ...) -> degrade to string.
    print(f"[WARNING] schema_infer: conflicting types {a['kind']} vs {b['kind']}, "
          f"falling back to string")
    return {'kind': 'scalar', 'ctype': 'string', 'nullable': nullable}


def _resolve_unknown(node):
    """Replace leftover 'unknown' nodes (always-null fields / always-empty arrays)
    with a harmless nullable string default."""
    if node['kind'] == 'unknown':
        return {'kind': 'scalar', 'ctype': 'string', 'nullable': True}
    if node['kind'] == 'array':
        node['element'] = _resolve_unknown(node['element'])
    elif node['kind'] == 'object':
        for key in node['fields']:
            node['fields'][key] = _resolve_unknown(node['fields'][key])
    return node


def _assign_names(node, name):
    """Assign a unique, path-based struct name to every object node."""
    if node['kind'] == 'object':
        node['name'] = name
        for key, child in node['fields'].items():
            _assign_names(child, name + _pascal(key))
    elif node['kind'] == 'array':
        _assign_names(node['element'], _singular(name))


_CPP_SCALAR = {'bool': 'bool', 'int': 'int', 'double': 'double', 'string': 'std::string'}
_CS_SCALAR = {'bool': 'bool', 'int': 'int', 'double': 'double', 'string': 'string'}
_CPP_INIT = {'bool': ' = false', 'int': ' = 0', 'double': ' = 0.0', 'string': ''}


def _cpp_type(node):
    if node['kind'] == 'scalar':
        return _CPP_SCALAR[node['ctype']]
    if node['kind'] == 'array':
        return f"std::vector<{_cpp_type(node['element'])}>"
    return node['name']  # object


def _cpp_init(node):
    if node['kind'] == 'scalar':
        return _CPP_INIT[node['ctype']]
    return ''


def _cs_type(node):
    if node['kind'] == 'scalar':
        return _CS_SCALAR[node['ctype']]
    if node['kind'] == 'array':
        return f"System.Collections.Generic.List<{_cs_type(node['element'])}>"
    return node['name']  # object


def _collect(node, out, seen):
    """Post-order walk: emit nested structs before the structs that use them.

    Fields are emitted sorted by name so the generated member order is stable
    across runs (the inference merge uses sets, whose iteration order is not
    deterministic between Python processes)."""
    if node['kind'] == 'object':
        sorted_fields = sorted(node['fields'].items())
        for key, child in sorted_fields:
            _collect(child, out, seen)
        if node['name'] not in seen:
            seen.add(node['name'])
            out.append({
                'name': node['name'],
                'fields': [
                    {'name': key,
                     'cpp_type': _cpp_type(child),
                     'cpp_init': _cpp_init(child),
                     'cs_type': _cs_type(child)}
                    for key, child in sorted_fields
                ],
            })
    elif node['kind'] == 'array':
        _collect(node['element'], out, seen)


def infer_table(entries):
    """Deep-union all entries of one table into a single resolved object node."""
    node = None
    for entry in entries:
        node = merge(node, infer(entry))
    if node is None:
        # Empty table: synthesize an object with just an int id.
        node = {'kind': 'object',
                'fields': {'id': {'kind': 'scalar', 'ctype': 'int', 'nullable': False}},
                'nullable': False}
    return _resolve_unknown(node)


def _has_int_id(node):
    """정수 'id' 필드를 가진 오브젝트 노드인가. 인덱스 테이블을 만들 대상의 기준이다."""
    if node['kind'] != 'object':
        return False
    field = node['fields'].get('id')
    return field is not None and field['kind'] == 'scalar' and field['ctype'] == 'int'


def _collect_indexes(node, steps, owner, owner_depth, out):
    """id 를 가진 중첩 오브젝트마다 (루트에서 그곳까지의 접근 경로 + 소유자)를 모은다.

    소유자는 'id 를 가진 가장 가까운 조상'이다 — 게이트/스폰 지점이면 그 맵,
    item_options 면 그 아이템. 이 값이 생성된 구조체의 parent 포인터 타입이 된다.
    steps 는 [{'field': 이름, 'array': 배열인가}] 로, 템플릿이 루프를 펼치는 데 쓴다.
    """
    if node['kind'] != 'object':
        return

    for key in sorted(node['fields']):
        child = node['fields'][key]

        if child['kind'] == 'array' and child['element']['kind'] == 'object':
            element = child['element']
            child_steps = steps + [{'field': key, 'array': True}]
            child_owner, child_depth = owner, owner_depth

            if _has_int_id(element):
                out.append({
                    'struct': element['name'],
                    'owner': owner,
                    'owner_depth': owner_depth,
                    'map_name': _plural(_snake(element['name'])),
                    'steps': child_steps,
                })
                child_owner, child_depth = element['name'], len(child_steps)

            _collect_indexes(element, child_steps, child_owner, child_depth, out)

        elif child['kind'] == 'object':
            _collect_indexes(child, steps + [{'field': key, 'array': False}],
                             owner, owner_depth, out)


def _build_levels(steps):
    """접근 경로를 중첩 루프로 펼친다.

    [{'spawn_points', False}, {'player_spawn', True}] ->
        [{'var': 'lv0', 'expr': 'root.spawn_points.player_spawn', 'depth': 2}]
    배열이 아닌 단계는 앞 단계의 표현식에 그대로 이어 붙고, 배열 단계에서만 루프가 하나 생긴다.
    """
    levels = []
    prefix = 'root'
    pending = []
    for i, step in enumerate(steps):
        pending.append(step['field'])
        if step['array']:
            var = 'lv%d' % len(levels)
            levels.append({'var': var,
                           'expr': prefix + '.' + '.'.join(pending),
                           'depth': i + 1})
            prefix = var
            pending = []
    return levels


def build_structs(entries, table_name):
    """Return (structs, indexes) for one table.

    structs: 구조체 서술자 목록. 중첩 구조체가 먼저 오고 마지막이 테이블 구조체다.
             id 를 가진 중첩 구조체에는 parent_type 이 채워진다.
    indexes: id 를 가진 중첩 구조체마다의 인덱스 서술자(ResourceLoader 가 테이블을 만든다).
    """
    root = infer_table(entries)
    _assign_names(root, table_name)

    indexes = []
    _collect_indexes(root, [], table_name, 0, indexes)
    for index in indexes:
        index['levels'] = _build_levels(index['steps'])
        # 소유자 변수: 깊이 0 이면 테이블 원소(root), 아니면 그 깊이에서 끝나는 루프 변수.
        index['owner_var'] = 'root'
        for level in index['levels']:
            if level['depth'] == index['owner_depth']:
                index['owner_var'] = level['var']
        index['element_var'] = index['levels'][-1]['var']
        index['table'] = table_name

    parent_of = {index['struct']: index['owner'] for index in indexes}

    out = []
    _collect(root, out, set())
    for struct in out:
        struct['parent_type'] = parent_of.get(struct['name'])
    return out, indexes
