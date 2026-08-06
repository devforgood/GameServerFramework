"""GameData JSON 정적 검증.

코드 생성 전에 각 테이블을 검사해 데이터 입력 실수를 생성 단계에서 차단한다.
스키마가 JSON 에서 추론되므로(schema_infer), 필드명 오타는 에러 없이 별개의
컬럼이 되어 C++/C# 모델까지 조용히 전파된다(예: 과거 skill.json 의 "hieght").

검사 항목:
  1) 모든 엔트리는 객체이며 정수 id 를 가져야 한다.
  2) id 는 같은 종류의 오브젝트끼리 파일 전체에서 유일해야 한다.
     최상위 엔트리뿐 아니라 중첩 오브젝트(게이트, 스폰 지점 등)도 마찬가지다 —
     ResourceLoader 가 종류마다 id 인덱스 테이블을 만들기 때문에, 맵 안에서만
     유일하면 다른 맵의 같은 id 가 인덱스에서 서로를 덮어쓴다.
  3) 필드명 오타 의심: 같은 경로에서 편집거리(전치 포함) 1 이하인 두 필드명이
     한 객체에 함께 등장한 적이 없으면 오타로 본다.
     - min_damage/max_damage 처럼 한 객체에 공존하는 쌍은 의도된 별개 필드로 허용.
     - progress1/progress2 처럼 숫자 접미사만 다른 쌍은 허용.
  4) null 값 금지: Unity 클라이언트의 JsonUtility 는 null 값을 만나면
     "JSON parse error: Invalid value" 로 파싱 전체가 실패한다(과거 Map.json 의
     navmesh_path: null 이 맵 테이블 로드를 통째로 죽인 사례). 값이 없으면
     빈 문자열/0 을 쓰거나 필드를 생략한다(생략은 양쪽 로더 모두 허용).
  5) (Map 전용) 게이트 타입/참조 검증:
     - type 은 'two_way'(양방향) 또는 'one_way'(단방향)여야 한다.
     - target_id 는 도착 지점을 가리키는 전역 유일 id 다. 게이트이거나 player_spawn
       이어야 하며(둘 다 parent 로 소속 맵을 알 수 있어서 목적지 맵 id 가 따로 필요없다),
       자기 자신을 가리킬 수 없다.
     - two_way: 상대가 게이트여야 하고, 그 게이트도 two_way 이며 이 게이트를
       되가리켜야 한다(입구/출구 짝). 짝이 어긋나면 한쪽 방향 이동이 조용히 실패한다.
     - one_way: 레이드 등 인스턴스 던전 입구용. 짝이 필요 없고 스폰 지점을 가리켜도 된다.
"""

from collections import Counter, defaultdict


def _osa_distance(a, b):
    """Optimal String Alignment 거리(인접 전치를 1로 계산하는 편집거리)."""
    la, lb = len(a), len(b)
    if abs(la - lb) > 1:
        return 2  # 이 검증에서는 1 초과인지 여부만 중요하다
    d = [[0] * (lb + 1) for _ in range(la + 1)]
    for i in range(la + 1):
        d[i][0] = i
    for j in range(lb + 1):
        d[0][j] = j
    for i in range(1, la + 1):
        for j in range(1, lb + 1):
            cost = 0 if a[i - 1] == b[j - 1] else 1
            d[i][j] = min(d[i - 1][j] + 1,        # 삭제
                          d[i][j - 1] + 1,        # 삽입
                          d[i - 1][j - 1] + cost)  # 치환
            if i > 1 and j > 1 and a[i - 1] == b[j - 2] and a[i - 2] == b[j - 1]:
                d[i][j] = min(d[i][j], d[i - 2][j - 2] + 1)  # 전치
    return d[la][lb]


def _strip_digits(s):
    return s.rstrip('0123456789')


GATE_TYPES = {'two_way', 'one_way'}


def _validate_map_spawn_ids(table_name, entries):
    """Map 전용: 스폰 지점 세 종류(player/monster/boss)는 하나의 id 공간을 공유한다.

    생성되는 구조체는 종류마다 달라서 경로별 검사(규칙 2)로는 서로 간의 충돌을 못 잡는다.
    하지만 유니티 쪽은 SpawnPoint 컴포넌트 하나라 맵툴이 종류 구분 없이 번호를 매기므로,
    종류가 달라도 겹치면 다음 스캔에서 번호가 밀려 씬과 JSON 이 어긋난다.
    """
    errors = []
    owner = {}  # id -> (맵 id, 종류)

    for m in entries:
        if not isinstance(m, dict):
            continue
        spawn_points = m.get('spawn_points') or {}
        for kind in ('player_spawn', 'monster_spawn', 'boss_spawn'):
            for s in spawn_points.get(kind) or []:
                if not isinstance(s, dict):
                    continue
                spawn_id = s.get('id')
                if not isinstance(spawn_id, int) or isinstance(spawn_id, bool) or spawn_id <= 0:
                    continue
                if spawn_id in owner:
                    prev_map, prev_kind = owner[spawn_id]
                    errors.append(
                        f"{table_name}: 스폰 id {spawn_id} 중복 — "
                        f"맵 {prev_map}/{prev_kind} 와 맵 {m.get('id')}/{kind}. "
                        f"스폰 지점은 종류가 달라도 id 를 공유합니다(맵툴이 한 번호대로 발급).")
                else:
                    owner[spawn_id] = (m.get('id'), kind)

    return errors


def _validate_map_gates(table_name, entries):
    """Map 테이블 전용: 게이트 type 유효성과 target_id 참조/짝(pairing)을 검증한다."""
    errors = []

    # target_id 는 전역 유일 id 다. 게이트든 스폰 지점이든 바로 찾을 수 있어야 한다.
    gates_by_id = {}       # id -> (게이트, 소속 맵)
    spawns_by_id = {}      # id -> 소속 맵 (player_spawn 만: 도착 지점이 될 수 있는 마커)
    for m in entries:
        if not isinstance(m, dict):
            continue
        for g in m.get('gates') or []:
            if isinstance(g, dict):
                gates_by_id[g.get('id')] = (g, m)
        for s in (m.get('spawn_points') or {}).get('player_spawn') or []:
            if isinstance(s, dict):
                spawns_by_id[s.get('id')] = m

    for m in entries:
        if not isinstance(m, dict):
            continue
        map_id = m.get('id')
        for g in m.get('gates') or []:
            if not isinstance(g, dict):
                continue
            gate_id = g.get('id')
            label = f"{table_name}: 맵 {map_id} 게이트 {gate_id}('{g.get('name', '')}')"

            if not isinstance(gate_id, int) or isinstance(gate_id, bool) or gate_id <= 0:
                errors.append(f"{label} — 게이트 id 는 1 이상의 정수여야 합니다")
                continue

            gate_type = g.get('type')
            if gate_type not in GATE_TYPES:
                errors.append(
                    f"{label} — type 은 'two_way' 또는 'one_way' 여야 합니다 (현재: {gate_type!r})")
                continue

            target_id = g.get('target_id')
            if target_id == gate_id:
                errors.append(f"{label} — target_id 가 자기 자신입니다")
                continue

            target_gate = gates_by_id.get(target_id)
            target_spawn_map = spawns_by_id.get(target_id)
            if target_gate is None and target_spawn_map is None:
                errors.append(
                    f"{label} — target_id {target_id} 에 해당하는 게이트/player_spawn 이 없습니다")
                continue

            if gate_type == 'two_way':
                if target_gate is None:
                    errors.append(
                        f"{label} — two_way 게이트는 스폰 지점이 아니라 짝이 되는 게이트를 "
                        f"가리켜야 합니다 (target_id {target_id} 는 player_spawn)")
                    continue
                back, back_map = target_gate
                if back.get('type') != 'two_way' or back.get('target_id') != gate_id:
                    errors.append(
                        f"{label} — two_way 게이트는 짝을 이뤄야 합니다: "
                        f"맵 {back_map.get('id')} 게이트 {target_id} 가 two_way 로 "
                        f"이 게이트({gate_id})를 되가리켜야 합니다")

    return errors


def _validate_map_gate_links(table_name, entries):
    """Map 테이블 전용: gate_links(존 그래프의 도보 가중치) 검증.

    gate_links 는 '같은 맵 안에서 이 마커에서 저 마커까지 걸어가는 비용' 이다. 서버는 맵의
    navmesh 로 이 값을 직접 재지만, 재지 못하는 맵(인스턴스처럼 기동 시 로드되지 않는 맵)이나
    디자이너가 경로를 유도하고 싶을 때 여기에 적은 값이 실측값을 덮어쓴다.

    덮어쓰기이기 때문에 잘못된 항목은 조용히 라우팅을 망가뜨린다 — 다른 맵의 마커를 가리키면
    간선이 아예 안 생기고, 음수 비용은 다익스트라를 망친다. 그래서 여기서 막는다.
    """
    errors = []

    for m in entries:
        if not isinstance(m, dict):
            continue

        map_id = m.get('id')
        # 이 맵 안의 마커 id — 도보 간선의 양 끝이 될 수 있는 것들.
        local_ids = set()
        for g in m.get('gates') or []:
            if isinstance(g, dict):
                local_ids.add(g.get('id'))
        for s in (m.get('spawn_points') or {}).get('player_spawn') or []:
            if isinstance(s, dict):
                local_ids.add(s.get('id'))

        seen_pairs = set()
        for i, link in enumerate(m.get('gate_links') or []):
            label = f"{table_name}: 맵 {map_id} gate_links[{i}]"
            if not isinstance(link, dict):
                errors.append(f"{label} — 항목이 객체가 아닙니다")
                continue

            from_id = link.get('from_id')
            to_id = link.get('to_id')
            cost = link.get('cost')

            if from_id == to_id:
                errors.append(f"{label} — from_id 와 to_id 가 같습니다 ({from_id})")
                continue

            for field, value in (('from_id', from_id), ('to_id', to_id)):
                if value not in local_ids:
                    errors.append(
                        f"{label} — {field} {value} 는 맵 {map_id} 의 마커가 아닙니다. "
                        f"gate_links 는 같은 맵 안의 도보 비용만 적을 수 있습니다")

            if not isinstance(cost, (int, float)) or isinstance(cost, bool) or cost < 0:
                errors.append(f"{label} — cost 는 0 이상의 숫자여야 합니다 (현재: {cost!r})")

            # 방향이 반대여도 같은 통로다. 둘 다 적으면 어느 쪽이 이기는지 데이터만 보고 알 수 없다.
            pair = frozenset((from_id, to_id))
            if pair in seen_pairs:
                errors.append(f"{label} — {from_id} <-> {to_id} 구간이 중복 기록됐습니다")
            seen_pairs.add(pair)

    return errors


def validate_table(table_name, entries):
    """오류 메시지 리스트를 반환한다(비어 있으면 통과)."""
    errors = []

    if not isinstance(entries, list):
        return [f"{table_name}: 최상위가 배열이 아닙니다"]

    # 1) 최상위 엔트리는 객체이고 정수 id 를 가져야 한다.
    for i, entry in enumerate(entries):
        if not isinstance(entry, dict):
            errors.append(f"{table_name}[{i}]: 엔트리가 객체가 아닙니다")
            continue
        entry_id = entry.get('id')
        if not isinstance(entry_id, int) or isinstance(entry_id, bool):
            errors.append(f"{table_name}[{i}]: 정수 'id' 필드가 필요합니다")

    # 2) id 는 같은 종류(같은 JSON 경로 = 같은 생성 구조체)끼리 파일 전체에서 유일해야 한다.
    #    경로별로 모아 보면 최상위 테이블과 중첩 오브젝트를 같은 규칙으로 검사할 수 있다.
    ids_by_path = defaultdict(list)

    def collect_ids(obj, path):
        if isinstance(obj, dict):
            if 'id' in obj and isinstance(obj['id'], int) and not isinstance(obj['id'], bool):
                ids_by_path[path].append(obj['id'])
            for key, value in obj.items():
                collect_ids(value, f"{path}.{key}")
        elif isinstance(obj, list):
            for item in obj:
                collect_ids(item, path + '[]')

    for entry in entries:
        collect_ids(entry, table_name)

    for path in sorted(ids_by_path):
        path_ids = ids_by_path[path]
        counts = Counter(path_ids)
        duplicated = sorted(x for x, c in counts.items() if c > 1)
        if duplicated:
            errors.append(
                f"{table_name}: {path} 의 id 가 중복됩니다 {duplicated} — "
                f"id 는 같은 종류끼리 파일 전체에서 유일해야 합니다 "
                f"(ResourceLoader 가 종류마다 id 인덱스를 만든다).")

    # 4) null 값 금지 (Unity JsonUtility 는 null 에서 파싱 전체가 실패한다)
    def find_nulls(obj, path):
        if obj is None:
            errors.append(
                f"{table_name}: null 값 금지 ({path}). "
                f"클라이언트 JsonUtility 파싱이 실패합니다 — 빈 문자열/0 을 쓰거나 필드를 생략하세요.")
        elif isinstance(obj, dict):
            for key, value in obj.items():
                find_nulls(value, f"{path}.{key}")
        elif isinstance(obj, list):
            for idx, item in enumerate(obj):
                find_nulls(item, f"{path}[{idx}]")

    for i, entry in enumerate(entries):
        find_nulls(entry, f"{table_name}[{i}]")

    # 5) Map 전용: 스폰 id 공유 공간 + 게이트 타입/참조/짝 검증
    if table_name == "Map":
        errors.extend(_validate_map_spawn_ids(table_name, entries))
        errors.extend(_validate_map_gates(table_name, entries))
        errors.extend(_validate_map_gate_links(table_name, entries))

    # 3) 필드명 오타 의심 검사 (중첩 객체 포함, 경로 단위로 비교)
    field_paths = defaultdict(set)  # (path, name) -> 등장한 객체 인스턴스 id 집합
    cooccur = set()                 # 같은 객체에 함께 등장한 (path, name) 쌍

    def walk(obj, path):
        if isinstance(obj, dict):
            names = list(obj.keys())
            for name in names:
                field_paths[(path, name)].add(id(obj))
            for x in range(len(names)):
                for y in range(x + 1, len(names)):
                    cooccur.add(frozenset({(path, names[x]), (path, names[y])}))
            for key, value in obj.items():
                walk(value, f"{path}.{key}")
        elif isinstance(obj, list):
            for item in obj:
                walk(item, f"{path}[]")

    for entry in entries:
        walk(entry, table_name)

    keys = sorted(field_paths.keys())
    for x in range(len(keys)):
        for y in range(x + 1, len(keys)):
            (p1, n1), (p2, n2) = keys[x], keys[y]
            if p1 != p2:
                continue
            if frozenset({keys[x], keys[y]}) in cooccur:
                continue  # 한 객체에 공존 — 의도된 별개 필드
            if _strip_digits(n1) == _strip_digits(n2) and (n1 != _strip_digits(n1) or n2 != _strip_digits(n2)):
                continue  # 숫자 접미사 변형
            if _osa_distance(n1, n2) <= 1:
                errors.append(
                    f"{table_name}: 필드명 오타 의심 '{n1}' vs '{n2}' (경로 {p1}). "
                    f"같은 필드라면 이름을 통일하세요.")

    return errors
