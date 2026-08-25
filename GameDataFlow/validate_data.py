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
  6) (Quest 전용) 스테이지/목표/선행조건/보상/시간 정책 검증:
     - 목표 type 은 서버가 해석할 수 있는 것이어야 하고, target_id 는 실제 다른 테이블
       (몬스터/아이템/스킬/맵)에 있는 id 여야 한다. 오타 난 target_id 는 영원히 진행되지
       않는 퀘스트가 되는데, 데이터만 봐서는 알 수 없다.
     - 스테이지당 목표는 3개까지다(quest_active 의 progress1~3).
     - 선행/차단 퀘스트, 보상 아이템/스킬 id 도 실제 존재해야 한다.
     - 리셋되는 퀘스트는 repeatable 이어야 하고, LimitedTimeQuest 는 제한 시간이 있어야 한다.
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


# 서버 QuestObjective 가 해석할 수 있는 목표 타입.
# 값을 추가하려면 Engine/Quest/QuestObjective.h 의 ParseObjectiveType 도 함께 늘려야 한다.
# 각 항목은 (타입 이름, target_id 가 가리키는 테이블) — None 이면 target_id 를 쓰지 않는다.
OBJECTIVE_TARGET_TABLE = {
    'kill': 'MonsterData',
    'collect': 'Item',
    'use_item': 'Item',
    'use_skill': 'Skill',
    'reach': 'Map',
    'talk': 'Npc',
    'interact': None,  # 맵 오브젝트 id — 맵 안에 있으므로 전역 검사는 생략
    'level': None,     # count 가 도달 목표 레벨이다
    'escort': 'Npc',
    'protect': 'Npc',
}

# 액터로 스폰되어야 성립하는 목표. 대상 NPC 가 hp 를 갖고 있어야 한다
# (hp 가 없는 NPC 는 서버에 액터가 없어 죽지도, 따라오지도 않는다).
OBJECTIVE_NEEDS_NPC_ACTOR = {'escort', 'protect'}

QUEST_STAGE_LOGIC = {'and', 'or'}
QUEST_RESET_TYPES = {'none', 'daily', 'weekly'}

# quest_active 테이블이 progress1~3 세 칸만 가지고 있다. 한 스테이지의 목표는
# 이 칸에 슬롯 순서대로 저장되므로 스테이지당 목표 수가 여기를 넘으면 진행도를 잃는다.
MAX_OBJECTIVES_PER_STAGE = 3


def _ids_of(tables, table_name):
    """다른 테이블의 최상위 id 집합. 테이블이 없으면 None(검사 생략)."""
    if not tables or table_name not in tables:
        return None
    return {e['id'] for e in tables[table_name]
            if isinstance(e, dict) and isinstance(e.get('id'), int)}


def _validate_quest_objective(label, obj, tables, errors):
    if not isinstance(obj, dict):
        errors.append(f"{label} — 목표가 객체가 아닙니다")
        return

    obj_type = obj.get('type')
    if obj_type not in OBJECTIVE_TARGET_TABLE:
        errors.append(
            f"{label} — 알 수 없는 목표 type {obj_type!r}. "
            f"지원 타입: {sorted(OBJECTIVE_TARGET_TABLE)}")
        return

    count = obj.get('count')
    if not isinstance(count, int) or isinstance(count, bool) or count < 1:
        errors.append(f"{label} — count 는 1 이상의 정수여야 합니다 (현재: {count!r})")

    target_table = OBJECTIVE_TARGET_TABLE[obj_type]
    target_id = obj.get('target_id', 0)

    if obj_type == 'level':
        if target_id:
            errors.append(
                f"{label} — level 목표는 target_id 를 쓰지 않습니다. "
                f"도달 목표 레벨은 count 에 적습니다 (현재 target_id: {target_id!r})")
        return

    if not isinstance(target_id, int) or isinstance(target_id, bool) or target_id <= 0:
        errors.append(
            f"{label} — {obj_type} 목표는 1 이상의 target_id 가 필요합니다 (현재: {target_id!r})")
        return

    known = _ids_of(tables, target_table) if target_table else None
    if known is not None and target_id not in known:
        errors.append(
            f"{label} — target_id {target_id} 가 {target_table} 테이블에 없습니다")
        return

    if obj_type in OBJECTIVE_NEEDS_NPC_ACTOR:
        _validate_npc_actor_target(label, obj_type, target_id, tables, errors)


def _validate_npc_actor_target(label, obj_type, npc_id, tables, errors):
    """호위/보호 대상이 실제로 월드에 설 수 있는 NPC 인지 본다.

    서버는 hp 가 있는 NPC 만 액터로 스폰한다. hp 가 없으면 그 NPC 는 씬에 그려진
    정적 데이터일 뿐이라 죽지도, 따라오지도 않는다 — 목표가 영원히 끝나지 않는다.
    """
    npcs = tables.get('Npc') if tables else None
    if not npcs:
        return  # 테이블이 없으면 검사 생략(위에서 이미 보고했다)

    npc = next((n for n in npcs if isinstance(n, dict) and n.get('id') == npc_id), None)
    if npc is None:
        return

    if not isinstance(npc.get('hp'), int) or npc.get('hp', 0) <= 0:
        errors.append(
            f"{label} — {obj_type} 대상 NPC {npc_id} 에 hp 가 없습니다. "
            f"hp 가 0 이면 서버가 액터로 스폰하지 않아 목표가 끝나지 않습니다")

    if obj_type == 'escort' and not npc.get('escort_dest_id'):
        errors.append(
            f"{label} — escort 대상 NPC {npc_id} 에 escort_dest_id 가 없습니다. "
            f"목적지가 없으면 도착 판정이 일어나지 않습니다")


def _validate_quest_stages(quest, label, tables, errors):
    stages = quest.get('stages')
    if not isinstance(stages, list) or not stages:
        errors.append(f"{label} — stages 는 최소 1개가 필요합니다")
        return

    for index, stage in enumerate(stages):
        stage_label = f"{label} stages[{index}]"
        if not isinstance(stage, dict):
            errors.append(f"{stage_label} — 스테이지가 객체가 아닙니다")
            continue

        # step 은 배열 순서와 같아야 한다. DB 의 quest_active.stage 가 이 값이고,
        # 서버는 stage 값으로 배열을 되찾으므로 어긋나면 진행이 엉뚱한 곳으로 간다.
        if stage.get('step') != index + 1:
            errors.append(
                f"{stage_label} — step 은 배열 순서와 같은 {index + 1} 이어야 합니다 "
                f"(현재: {stage.get('step')!r})")

        logic = stage.get('logic')
        if logic not in QUEST_STAGE_LOGIC:
            errors.append(
                f"{stage_label} — logic 은 'and' 또는 'or' 여야 합니다 (현재: {logic!r})")

        objectives = stage.get('objectives')
        if not isinstance(objectives, list) or not objectives:
            errors.append(f"{stage_label} — objectives 는 최소 1개가 필요합니다")
            continue

        if len(objectives) > MAX_OBJECTIVES_PER_STAGE:
            errors.append(
                f"{stage_label} — 목표가 {len(objectives)}개입니다. "
                f"quest_active 는 진행도 칸이 {MAX_OBJECTIVES_PER_STAGE}개뿐이라 "
                f"스테이지를 나눠야 합니다")

        seen = set()
        for slot, obj in enumerate(objectives):
            _validate_quest_objective(
                f"{stage_label} objectives[{slot}]", obj, tables, errors)
            if isinstance(obj, dict):
                key = (obj.get('type'), obj.get('target_id', 0))
                if key in seen:
                    errors.append(
                        f"{stage_label} objectives[{slot}] — 같은 스테이지에 "
                        f"{key[0]}/{key[1]} 목표가 중복됩니다. 진행 이벤트가 "
                        f"두 칸을 동시에 올려 카운트가 어긋납니다")
                seen.add(key)


def _validate_quest_prerequisites(quest, label, quest_ids, tables, errors):
    prereq = quest.get('prerequisites')
    if prereq is None:
        return
    if not isinstance(prereq, dict):
        errors.append(f"{label} — prerequisites 는 객체여야 합니다")
        return

    quest_id = quest.get('id')
    for field in ('completed_quest_ids', 'blocked_quest_ids'):
        for ref in prereq.get(field) or []:
            if ref == quest_id:
                errors.append(f"{label} — prerequisites.{field} 가 자기 자신을 참조합니다")
            elif ref not in quest_ids:
                errors.append(
                    f"{label} — prerequisites.{field} 의 퀘스트 {ref} 가 존재하지 않습니다")

    for field, target_table in (('item_ids', 'Item'), ('skill_ids', 'Skill')):
        known = _ids_of(tables, target_table)
        if known is None:
            continue
        for ref in prereq.get(field) or []:
            if ref not in known:
                errors.append(
                    f"{label} — prerequisites.{field} 의 {ref} 가 "
                    f"{target_table} 테이블에 없습니다")


def _validate_quest_rewards(quest, label, tables, errors):
    rewards = quest.get('rewards')
    if rewards is None:
        return
    if not isinstance(rewards, dict):
        errors.append(f"{label} — rewards 는 객체여야 합니다")
        return

    item_ids = _ids_of(tables, 'Item')
    for field in ('items', 'choice_items'):
        for i, entry in enumerate(rewards.get(field) or []):
            entry_label = f"{label} rewards.{field}[{i}]"
            if not isinstance(entry, dict):
                errors.append(f"{entry_label} — 항목이 객체가 아닙니다")
                continue
            item_id = entry.get('item_id')
            count = entry.get('count')
            if item_ids is not None and item_id not in item_ids:
                errors.append(f"{entry_label} — item_id {item_id!r} 가 Item 테이블에 없습니다")
            if not isinstance(count, int) or isinstance(count, bool) or count < 1:
                errors.append(f"{entry_label} — count 는 1 이상이어야 합니다 (현재: {count!r})")

    skill_ids = _ids_of(tables, 'Skill')
    if skill_ids is not None:
        for ref in rewards.get('skill_ids') or []:
            if ref not in skill_ids:
                errors.append(f"{label} — rewards.skill_ids 의 {ref} 가 Skill 테이블에 없습니다")

    # 선택 보상은 하나만 고를 수 있으므로 후보가 1개면 선택의 의미가 없다.
    choice = rewards.get('choice_items') or []
    if len(choice) == 1:
        errors.append(
            f"{label} — rewards.choice_items 가 1개뿐입니다. "
            f"고정 보상이면 items 로 옮기세요")

    # 자동 완료 퀘스트는 서버가 목표 달성 즉시 끝내므로 보상을 고를 사람이 없다.
    if choice and quest.get('auto_complete'):
        errors.append(
            f"{label} — auto_complete 퀘스트에는 선택 보상을 둘 수 없습니다 "
            f"(고를 기회 없이 완료됩니다)")


def _validate_quest_time(quest, label, errors):
    time = quest.get('time')
    if time is None:
        return
    if not isinstance(time, dict):
        errors.append(f"{label} — time 은 객체여야 합니다")
        return

    reset_type = time.get('reset_type', 'none')
    if reset_type not in QUEST_RESET_TYPES:
        errors.append(
            f"{label} — time.reset_type 은 {sorted(QUEST_RESET_TYPES)} 중 하나여야 합니다 "
            f"(현재: {reset_type!r})")

    for field in ('limit_seconds', 'cooldown_seconds'):
        value = time.get(field, 0)
        if not isinstance(value, int) or isinstance(value, bool) or value < 0:
            errors.append(f"{label} — time.{field} 는 0 이상의 정수여야 합니다 (현재: {value!r})")

    # 리셋되는 퀘스트는 다시 받을 수 있어야 한다. 아니면 첫 완료 후 영영 잠긴다.
    if reset_type != 'none' and not time.get('repeatable'):
        errors.append(
            f"{label} — time.reset_type 이 '{reset_type}' 인데 repeatable 이 false 입니다. "
            f"리셋되어도 재수락이 막혀 있습니다")

    # 제한 시간 퀘스트라고 선언해 놓고 제한이 없으면 클래스가 아무 일도 하지 않는다.
    if quest.get('code_name') == 'LimitedTimeQuest' and not time.get('limit_seconds'):
        errors.append(
            f"{label} — LimitedTimeQuest 는 time.limit_seconds 가 1 이상이어야 합니다")


def _blocked_ids(entry):
    """퀘스트의 prerequisites.blocked_quest_ids 를 정수 목록으로 돌려준다."""
    prerequisites = entry.get('prerequisites')
    if not isinstance(prerequisites, dict):
        return []
    blocked = prerequisites.get('blocked_quest_ids')
    return [b for b in blocked if isinstance(b, int)] if isinstance(blocked, list) else []


def _validate_quests(table_name, entries, tables):
    """Quest 테이블 전용: 스테이지/목표/선행조건/보상/시간 정책 검증."""
    errors = []

    quest_ids = {e['id'] for e in entries
                 if isinstance(e, dict) and isinstance(e.get('id'), int)}
    chains = defaultdict(list)  # chain_id -> [(chain_step, quest_id)]

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        quest_id = entry.get('id')
        label = f"{table_name}: 퀘스트 {quest_id}"

        if not entry.get('code_name'):
            errors.append(
                f"{label} — code_name 이 필요합니다 "
                f"(QuestFactory 가 이 이름으로 클래스를 고른다)")

        min_level = entry.get('min_level', 0)
        max_level = entry.get('max_level', 0)
        if max_level and max_level < min_level:
            errors.append(
                f"{label} — max_level({max_level}) 이 min_level({min_level}) 보다 작습니다")

        # 완료 NPC 를 찾아가야 하는데 그 NPC 가 없으면 퀘스트가 영원히 안 끝난다.
        if not entry.get('auto_complete') and not entry.get('end_npc_id'):
            errors.append(
                f"{label} — auto_complete 가 false 면 end_npc_id 가 필요합니다 "
                f"(완료를 접수할 NPC 가 없습니다)")

        map_ids = _ids_of(tables, 'Map')
        map_id = entry.get('map_id', 0)
        if map_id and map_ids is not None and map_id not in map_ids:
            errors.append(f"{label} — map_id {map_id} 가 Map 테이블에 없습니다")

        # 시작/완료 NPC 가 실제로 없으면 퀘스트를 받거나 끝낼 방법이 사라진다.
        npc_ids = _ids_of(tables, 'Npc')
        if npc_ids is not None:
            for field in ('start_npc_id', 'end_npc_id'):
                npc_id = entry.get(field, 0)
                if npc_id and npc_id not in npc_ids:
                    errors.append(f"{label} — {field} {npc_id} 가 Npc 테이블에 없습니다")

        chain_id = entry.get('chain_id', 0)
        if chain_id:
            chains[chain_id].append((entry.get('chain_step', 0), quest_id))

        _validate_quest_stages(entry, label, tables, errors)
        _validate_quest_prerequisites(entry, label, quest_ids, tables, errors)
        _validate_quest_rewards(entry, label, tables, errors)
        _validate_quest_time(entry, label, errors)

    # 같은 체인 안에서 순서 번호가 겹치면 다음 퀘스트를 하나로 정할 수 없다.
    # 단 하나의 예외가 분기다: 같은 자리에 놓인 퀘스트들이 서로를 blocked_quest_ids 로
    # 막고 있으면 플레이어는 그중 하나만 할 수 있으므로 "다음 퀘스트"는 여전히 하나다.
    blocked_by = {
        entry['id']: set(_blocked_ids(entry))
        for entry in entries
        if isinstance(entry, dict) and isinstance(entry.get('id'), int)
    }

    for chain_id, steps in chains.items():
        by_step = defaultdict(list)
        for step, qid in steps:
            by_step[step].append(qid)

        for step, qids in sorted(by_step.items()):
            if len(qids) < 2:
                continue

            # 서로를 전부 막고 있어야 분기다. 한쪽만 막으면 순서에 따라 둘 다 완료할 수
            # 있어서, 갈림길이 아니라 그냥 겹친 번호가 된다.
            mutually_exclusive = all(
                set(qids) - {qid} <= blocked_by.get(qid, set()) for qid in qids)
            if not mutually_exclusive:
                errors.append(
                    f"{table_name}: 체인 {chain_id} 의 chain_step {step} 이 중복됩니다 "
                    f"(퀘스트 {sorted(qids)}). 분기라면 서로를 blocked_quest_ids 로 막아 "
                    f"하나만 진행할 수 있게 해야 합니다")
        for step, qid in steps:
            if step < 1:
                errors.append(
                    f"{table_name}: 퀘스트 {qid} — chain_id 가 있으면 "
                    f"chain_step 은 1 이상이어야 합니다 (현재: {step})")

    return errors


# dialog.json 의 choices[].action. C++ 의 ParseDialogAction 과 짝을 이룬다.
# param 을 쓰는 동작만 값을 담고, 나머지는 None.
DIALOG_ACTIONS = {
    'close': None,
    'goto': None,
    'accept_quest': 'Quest',
    'complete_quest': 'Quest',
}

# dialog.json 의 choices[].show_if.state. C++ 의 ParseDialogCondition 과 짝을 이룬다.
# 오타는 파싱에서 "조건 없음"이 되어 선택지가 늘 보이게 되는데, 데이터만 봐서는
# 조건을 적었으니 걸러진다고 믿게 된다 — 그래서 여기서 막는다.
DIALOG_CONDITION_STATES = {
    'acceptable',
    'in_progress',
    'ready_to_complete',
    'completed',
    'not_completed',
    'failed',
}

# PlayerDialog 가 "무엇을 내보냈는가"를 비트 하나로 들고 있어 노드당 선택지가 여기를
# 넘으면 넘친 선택지는 영영 보이지 않는다.
MAX_DIALOG_CHOICES = 32


def _validate_dialog_condition(clabel, choice, quest_ids, errors):
    """선택지에 걸린 show_if 를 본다. 조건이 없으면 아무것도 하지 않는다.

    반환값은 "조건 없는 선택지인가" — 노드마다 하나는 있어야 하기 때문이다.
    """
    show_if = choice.get('show_if')
    if show_if is None:
        return True

    if not isinstance(show_if, dict):
        errors.append(f"{clabel} — show_if 는 객체여야 합니다")
        return False

    quest_id = show_if.get('quest_id', 0)
    state = show_if.get('state')

    # quest_id 0 은 코드에서 "조건 없음"으로 읽힌다. 조건을 적어 놓고 빠뜨린 쪽이
    # 훨씬 흔하므로 실수로 본다.
    if not isinstance(quest_id, int) or isinstance(quest_id, bool) or quest_id <= 0:
        errors.append(
            f"{clabel} — show_if.quest_id 는 1 이상의 퀘스트 id 여야 합니다 "
            f"(현재: {quest_id!r})")
    elif quest_ids is not None and quest_id not in quest_ids:
        errors.append(f"{clabel} — show_if.quest_id {quest_id} 가 Quest 테이블에 없습니다")

    if state not in DIALOG_CONDITION_STATES:
        errors.append(
            f"{clabel} — 알 수 없는 show_if.state {state!r}. "
            f"지원: {sorted(DIALOG_CONDITION_STATES)}")

    return False


def _validate_dialogs(table_name, entries, tables):
    """대화 노드가 서로 이어져 있고, 각 선택지가 실제로 무언가를 하는지 본다.

    끊어진 노드 참조는 런타임에 "대화가 그냥 닫힌다"로 나타나서, 데이터를 고칠 단서가
    아무것도 남지 않는다. 여기서 잡는 편이 훨씬 싸다.
    """
    errors = []

    node_ids = {e['id'] for e in entries
                if isinstance(e, dict) and isinstance(e.get('id'), int)}
    npc_ids = _ids_of(tables, 'Npc')
    quest_ids = _ids_of(tables, 'Quest')

    for entry in entries:
        if not isinstance(entry, dict):
            continue

        node_id = entry.get('id')
        label = f"{table_name}[id={node_id}]"

        if npc_ids is not None and entry.get('npc_id') not in npc_ids:
            errors.append(f"{label} — npc_id {entry.get('npc_id')!r} 가 Npc 테이블에 없습니다")

        if not entry.get('text_id'):
            errors.append(f"{label} — text_id 가 비어 있습니다")

        choices = entry.get('choices')
        if not isinstance(choices, list) or not choices:
            errors.append(
                f"{label} — choices 가 비어 있습니다. "
                f"선택지가 없으면 플레이어가 대화를 닫을 방법이 없습니다")
            continue

        if len(choices) > MAX_DIALOG_CHOICES:
            errors.append(
                f"{label} — 선택지가 {len(choices)}개입니다. "
                f"서버가 무엇을 내보냈는지 비트 하나로 들고 있어 "
                f"{MAX_DIALOG_CHOICES}개까지만 보낼 수 있습니다")

        has_unconditional = False
        seen_text_ids = set()

        for i, choice in enumerate(choices):
            clabel = f"{label} choices[{i}]"
            if not isinstance(choice, dict):
                errors.append(f"{clabel} — 선택지가 객체가 아닙니다")
                continue

            text_id = choice.get('text_id')
            if not text_id:
                errors.append(f"{clabel} — text_id 가 비어 있습니다")
            elif text_id in seen_text_ids:
                # 같은 대사가 한 화면에 두 번 뜨는 데이터 실수이기도 하고, 받은 선택지가
                # 데이터의 어느 선택지인지 되짚을 단서가 text_id 뿐이라(프로토콜은 걸러진
                # 목록에서의 번호만 보낸다) 겹치면 짝을 지을 수 없다.
                errors.append(
                    f"{clabel} — text_id {text_id!r} 가 이 노드 안에서 중복됩니다")
            else:
                seen_text_ids.add(text_id)

            if _validate_dialog_condition(clabel, choice, quest_ids, errors):
                has_unconditional = True

            action = choice.get('action')
            if action not in DIALOG_ACTIONS:
                errors.append(
                    f"{clabel} — 알 수 없는 action {action!r}. "
                    f"지원: {sorted(DIALOG_ACTIONS)}")
                continue

            next_id = choice.get('next_id', 0)
            if action == 'goto':
                if next_id not in node_ids:
                    errors.append(
                        f"{clabel} — goto 의 next_id {next_id!r} 에 해당하는 노드가 없습니다")
            elif action == 'close' and next_id:
                errors.append(
                    f"{clabel} — close 는 next_id 를 쓰지 않습니다 (현재: {next_id!r})")
            elif next_id and next_id not in node_ids:
                errors.append(
                    f"{clabel} — next_id {next_id!r} 에 해당하는 노드가 없습니다")

            param_table = DIALOG_ACTIONS[action]
            if param_table == 'Quest':
                param = choice.get('param', 0)
                if quest_ids is not None and param not in quest_ids:
                    errors.append(
                        f"{clabel} — {action} 의 param {param!r} 이 Quest 테이블에 없습니다")

        # 조건이 다 어긋나면 남는 선택지가 없어 대화를 닫을 수 없다. 노드는 서버가
        # 들고 있으므로 그 상태에서는 창을 닫아도 다음 상호작용이 같은 자리로 돌아온다.
        if not has_unconditional:
            errors.append(
                f"{label} — 모든 선택지에 show_if 가 걸려 있습니다. 조건이 전부 어긋나면 "
                f"플레이어가 대화를 닫을 방법이 없으니 조건 없는 선택지가 하나는 필요합니다")

    # 대화가 걸린 NPC 의 시작 노드가 실제로 존재하는지도 함께 본다.
    npcs = tables.get('Npc') if tables else None
    for npc in npcs or []:
        if not isinstance(npc, dict):
            continue
        root = npc.get('dialog_id', 0)
        if root and root not in node_ids:
            errors.append(
                f"Npc[id={npc.get('id')}] — dialog_id {root} 에 해당하는 대화 노드가 없습니다")

    return errors


def validate_table(table_name, entries, tables=None):
    """오류 메시지 리스트를 반환한다(비어 있으면 통과).

    tables 는 {테이블 이름: 엔트리 리스트} 전체 맵이다. 퀘스트처럼 다른 테이블의
    id 를 참조하는 데이터를 교차 검증할 때 쓴다. 없으면 교차 검사만 건너뛴다.
    """
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

    # 6) Quest 전용: 스테이지/목표/선행조건/보상/시간 정책 검증
    if table_name == "Quest":
        errors.extend(_validate_quests(table_name, entries, tables))

    # 7) Dialog 전용: 노드 참조/동작/선택지 검증
    if table_name == "Dialog":
        errors.extend(_validate_dialogs(table_name, entries, tables))

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
