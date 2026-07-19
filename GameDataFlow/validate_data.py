"""GameData JSON 정적 검증.

코드 생성 전에 각 테이블을 검사해 데이터 입력 실수를 생성 단계에서 차단한다.
스키마가 JSON 에서 추론되므로(schema_infer), 필드명 오타는 에러 없이 별개의
컬럼이 되어 C++/C# 모델까지 조용히 전파된다(예: 과거 skill.json 의 "hieght").

검사 항목:
  1) 모든 엔트리는 객체이며 정수 id 를 가져야 한다.
  2) 테이블 안에서 id 는 유일해야 한다.
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
     - two_way: 상대 게이트가 존재하고, 그 게이트도 two_way 이며 이 게이트를
       되가리켜야 한다(입구/출구 짝). 짝이 어긋나면 한쪽 방향 이동이 조용히 실패한다.
     - one_way: 레이드 등 인스턴스 던전 입구용. target_gate_id 가 0 이면 목적지
       맵의 player_spawn 지점에 도착하므로 목적지에 player_spawn 이 있어야 하고,
       0 이 아니면 해당 게이트가 목적지 맵에 존재해야 한다.
"""

from collections import defaultdict


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


def _validate_map_gates(table_name, entries):
    """Map 테이블 전용: 게이트 type 유효성과 맵 간 참조/짝(pairing)을 검증한다."""
    errors = []
    maps_by_id = {e.get('id'): e for e in entries if isinstance(e, dict)}

    for m in entries:
        if not isinstance(m, dict):
            continue
        map_id = m.get('id')
        seen_gate_ids = set()
        for g in m.get('gates') or []:
            if not isinstance(g, dict):
                continue
            gate_id = g.get('id')
            label = f"{table_name}: 맵 {map_id} 게이트 {gate_id}('{g.get('name', '')}')"

            if not isinstance(gate_id, int) or isinstance(gate_id, bool) or gate_id <= 0:
                errors.append(f"{label} — 게이트 id 는 1 이상의 정수여야 합니다")
                continue
            if gate_id in seen_gate_ids:
                errors.append(f"{label} — 맵 안에서 게이트 id 가 중복됩니다")
            seen_gate_ids.add(gate_id)

            gate_type = g.get('type')
            if gate_type not in GATE_TYPES:
                errors.append(
                    f"{label} — type 은 'two_way' 또는 'one_way' 여야 합니다 (현재: {gate_type!r})")
                continue

            target_map_id = g.get('target_map_id')
            target_gate_id = g.get('target_gate_id')
            target_map = maps_by_id.get(target_map_id)
            if target_map is None:
                errors.append(f"{label} — 존재하지 않는 target_map_id {target_map_id}")
                continue

            target_gates = {tg.get('id'): tg
                            for tg in (target_map.get('gates') or []) if isinstance(tg, dict)}

            if gate_type == 'two_way':
                back = target_gates.get(target_gate_id)
                if back is None:
                    errors.append(
                        f"{label} — two_way 인데 맵 {target_map_id} 에 게이트 {target_gate_id} 가 없습니다")
                elif (back.get('type') != 'two_way'
                      or back.get('target_map_id') != map_id
                      or back.get('target_gate_id') != gate_id):
                    errors.append(
                        f"{label} — two_way 게이트는 짝을 이뤄야 합니다: "
                        f"맵 {target_map_id} 게이트 {target_gate_id} 가 two_way 로 "
                        f"이 게이트(맵 {map_id}, 게이트 {gate_id})를 되가리켜야 합니다")
            else:  # one_way
                if target_gate_id == 0:
                    spawn_points = target_map.get('spawn_points') or {}
                    if not (spawn_points.get('player_spawn') or []):
                        errors.append(
                            f"{label} — target_gate_id 0(스폰 지점 도착)인데 "
                            f"맵 {target_map_id} 에 player_spawn 이 없습니다")
                elif target_gate_id not in target_gates:
                    errors.append(
                        f"{label} — 맵 {target_map_id} 에 게이트 {target_gate_id} 가 없습니다")

    return errors


def validate_table(table_name, entries):
    """오류 메시지 리스트를 반환한다(비어 있으면 통과)."""
    errors = []

    if not isinstance(entries, list):
        return [f"{table_name}: 최상위가 배열이 아닙니다"]

    # 1) & 2) id 검사
    ids = []
    for i, entry in enumerate(entries):
        if not isinstance(entry, dict):
            errors.append(f"{table_name}[{i}]: 엔트리가 객체가 아닙니다")
            continue
        entry_id = entry.get('id')
        if not isinstance(entry_id, int) or isinstance(entry_id, bool):
            errors.append(f"{table_name}[{i}]: 정수 'id' 필드가 필요합니다")
        else:
            ids.append(entry_id)
    duplicated = sorted({x for x in ids if ids.count(x) > 1})
    if duplicated:
        errors.append(f"{table_name}: 중복 id {duplicated}")

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

    # 5) Map 전용: 게이트 타입/참조/짝 검증
    if table_name == "Map":
        errors.extend(_validate_map_gates(table_name, entries))

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
