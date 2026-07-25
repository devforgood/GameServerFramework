# 스킬 시스템 (Skill System)

디아블로2 스타일 스킬을 **데이터 주도(data-driven)** 로 정의하고, **서버 권위(server-authoritative)** 로
검증·적용하는 시스템이다. 새 스킬은 대부분 `skill.json` 한 줄로 추가된다.

---

## 1. 설계 방향

### 1-1. 정의(flyweight)와 상태(state)의 분리
- **`Skill`** ([Skill.h](Skill.h)) = 무상태 스킬 정의. id 당 **1개만** 생성해
  [`SkillRegistry`](SkillRegistry.h) 가 캐싱하고 모든 캐릭터가 공유한다.
- **`SkillState`** ([SkillState.h](SkillState.h)) = 시전마다 달라지는 런타임 값(페이즈/쿨다운/펄스 타이머 등).
  캐릭터별 [`SkillSet`](SkillSet.h) 이 보관한다.
- 덕분에 "캐릭터 수 × 스킬 수" 만큼 객체를 만들지 않는다.

### 1-2. 시전의 단일 관문: `SkillSet::TryCast`
검증(보유 / 페이즈 / 쿨다운 / 입력잠금 / 패시브 여부) → `Skill::Cast` → 페이즈 전환을
**한 곳**에서 처리한다. 검증 실패 시 상태를 바꾸지 않으므로, **성공(`Success`)일 때만 브로드캐스트**하면 된다.
[`CastContext`](CastContext.h) 가 네트워크에 독립적이라 플레이어 핸들러(`syncnet::UseSkill`)와
AI(BehaviorTree)가 **같은 경로**를 쓴다.

### 1-3. 효과(effect) 조합으로 동작 정의
스킬의 동작은 `skill.json` 의 `effects: [{ "type", "phase" }]` 조합으로 정의된다.
효과 구현은 [SkillEffects.cpp](SkillEffects.cpp) 의 `ISkillEffect` 파생 클래스이고,
`type` 문자열 → 구현 매핑은 `SkillEffectRegistry` 가 담당한다.
**새 스킬 = 효과 조합(데이터)**, **새 메커니즘 = 효과 클래스 1개(코드)**.

### 1-4. 서버 권위 & 동기화
- 데미지/회복은 전부 **`combat` 단일 경로**([CombatSystem.h](../Actor/CombatSystem.h))와
  `Actor` 체력 API 를 거친다. 체력 변경은 `GameObjectChangeType::Health` 플래그로 **자동 동기화**되므로
  효과가 체력만 바꾸면 별도 브로드캐스트가 필요 없다.
- 클라가 보고한 지속시간/시차는 참고만 하고, **데이터(`duration`, `cooldown`)가 상한**이다.
- 클라이언트는 서버가 재전송한 `UseSkill`(skillId + 목표 지점 + 방향)로 **연출만** 재생한다.

### 1-5. 액티브 vs 패시브
| 구분 | 발동 | 예시 | 서버 처리 |
|------|------|------|-----------|
| **`type: "active"`** | 유저 액션(`UseSkill`)으로 발동 | 파이어볼, 텔레포트, 도약 공격, 차지 | `TryCast` → 페이즈 머신(Active→Cooldown→Ready) |
| **`type: "passive"`** | **보유만으로 지속 적용**(액션 불필요) | 성화·기도 오라 | `SkillSet::Update` 가 매 틱 `Skill::Tick` 호출, 쿨다운/페이즈 없음 |

> **오라(aura)는 `type` 이 아니라 효과**다. "지면에 지속되는 오라"는 패시브 스킬이
> `phase: "pulse"` 효과(`aura_damage`/`heal`)를 `pulse_interval` 마다 방출해서 표현한다.
> 패시브는 `TryCast` 로 시전할 수 없다(클라가 보내도 `SkillNotFound` 로 거부).

---

## 2. 스킬 추가 방법

### 2-1. 가장 흔한 경우 — 데이터만으로 추가 (코드 0줄)
기존 효과 조합으로 표현되면 [skill.json](../../Client/Assets/Resources/GameData/skill.json) 에
엔트리 하나만 추가하면 된다. `code_name` 을 생략하면 팩토리가 기본 `Skill` 로 생성한다.

```jsonc
{
  "id": 101,
  "type": "active",
  "min_damage": 18, "max_damage": 36,
  "radius": 4,
  "cooldown": 1.0,
  "name_id": "fireball_name", "desc_id": "fireball_desc",
  "effects": [ { "type": "aoe_damage" } ]
}
```

그 다음 **GameDataFlow 를 실행**해 C++/C# 모델·팩토리·로더를 재생성한다(신규 필드가 있으면 필수):

```bash
cd GameDataFlow && python GameDataFlow.py
```

> ⚠️ 데이터 규칙: `null` 값 금지(클라 `JsonUtility` 파싱 전체 실패), id 는 테이블 내 유일,
> 필드명 오타 자동 검사([validate_data.py](../../GameDataFlow/validate_data.py)) 통과. 값이 없으면 필드를 생략한다.

### 2-2. 새 메커니즘이 필요한 경우 — 효과 클래스 추가
1. [SkillEffects.cpp](SkillEffects.cpp) 에 `ISkillEffect` 파생 클래스 구현.
2. `BuildRegistry()` 에 `registry.emplace("<type>", ...)` 등록.
3. 필요한 파라미터를 `skill.json` 의 Skill 레벨 필드로 추가(예: `radius`, `heal`) 후 GameDataFlow 재생성.

효과는 `Apply(caster, state, data)` 만 구현한다. 데미지는 반드시 `combat::` 경로를 써서
킬 크레딧·동기화 일관성을 유지한다.

### 2-3. 클라 전용 연출/입력이 필요한 경우 — `code_name`
`code_name` 을 지정하면 GameDataFlow 가 서버/클라 파생 클래스 스텁을 생성한다
(서버 `Engine/skill/<Name>.h/.cpp`, 클라 `Client/Assets/Scripts/Skill/<Name>.cs`).
서버 훅(`Cast`/`Tick`/`OnActiveEnd`)이나 클라 입력·VFX 훅만 필요할 때 오버라이드한다.
예: 오라의 지면 이펙트/토글 UI 는 클라 `AuraSkill.cs` 가 담당(서버는 기본 pulse tick 사용).

### 2-4. 레퍼런스

**효과 type** (SkillEffects.cpp):
| type | 중심 | 설명 | 쓰는 필드 |
|------|------|------|-----------|
| `damage` | 캐스터(부채꼴) | 목표 방향 range/angle 안에 데미지. 회전 판정 포함 | `min/max_damage`, `range`, `angle` |
| `aoe_damage` | 목표 지점(원형) | 시전 지점 중심 반경에 데미지(메테오/블리자드) | `min/max_damage`, `radius`(없으면 `range`) |
| `aura_damage` | 캐스터(원형) | 캐스터를 따라다니는 원형 데미지. 회전 없음(오라 pulse용) | `min/max_damage`, `radius` |
| `heal` | 캐스터 | 자신 체력 회복 | `heal` |
| `teleport` | 목표 지점 | 목표 지점으로 순간이동 | (targetPos) |
| `dash` | 캐스터→목표 | 목표 방향으로 `range` 만큼(더 가까우면 목표까지) `duration` 동안 **전진**. 도착 지점을 `state.targetPos` 에 되써서 `end` 효과가 착지 지점에 적용된다 | `range`, `duration` |
| `knockback` | 캐스터(원형) | 반경 안의 대상을 캐스터 반대 방향으로 밀어냄(네비메시 스냅으로 벽 통과 없음) | `knockback`, `radius`(없으면 `range`) |
| `input_lock` | 캐스터 | 입력 잠금(Active 종료 시 자동 해제) | — |

> `dash` 는 효과가 목적지/속도만 정하고 실제 전진은 Active 동안 `Skill::Tick`(`skill_dash::Step`)이
> 매 틱 처리한다. 위치는 네비 에이전트가 권위이므로 이동 중에도 그대로 동기화된다(순간이동 `teleport` 와 다른 점).
> `knockback` 은 중심이 항상 캐스터라 강타/전투 함성처럼 **캐스터 주변**을 때리는 스킬에 쓴다.

**phase**:
| phase | 실행 시점 |
|-------|-----------|
| (생략) / `active` | 시전 즉시(`Skill::Cast`) |
| `end` | Active 페이즈 종료 시(`OnActiveEnd`) |
| `pulse` | Active/패시브 동안 `pulse_interval` 마다(`Skill::Tick`) |

**주요 Skill 필드**: `id`, `type`(active/passive), `monster_only`, `code_name`, `name_id`, `desc_id`,
`min_damage`/`max_damage`, `range`, `angle`, `radius`, `cooldown`, `duration`, `height`,
`heal`, `knockback`, `pulse_interval`, `effects[]`, `fx`/`element`(클라 전용).

### 2-5. 조합 예시
- **차지(Charge, 팔라딘)**: `input_lock`(active) → `dash`(active) → `aoe_damage`(end).
  `range` 만큼 돌진해 착지 지점에 광역 데미지. 도약 공격과 달리 **사라지지 않고 지면을 달린다**.
- **회피 도약(Vault)**: `input_lock` → `dash` 만. 데미지 없는 순수 이동기(같은 효과의 재사용 예).
- **도약 공격(Leap Attack)**: `input_lock`(active) → `teleport`(end) → `aoe_damage`(end).
  점프 중 입력잠금 후 착지 지점에 순간이동하며 광역 데미지.
- **강타/전투 함성(Smite, War Cry)**: `damage` → `knockback`. 때린 뒤 주변 대상을 밀어냄.
- **성화 오라(Holy Fire, passive)**: `aura_damage`(pulse), `pulse_interval` 마다 주변 적을 태움.
- **노바(Nova)**: `damage` 에 `angle: 360` — 캐스터 중심 전방위.

현재 [skill.json](../../Client/Assets/Resources/GameData/skill.json) 에는 D2 7개 클래스 계열의
액티브 스킬(1~146)과 패시브 오라(200~203)가 들어 있고, **차지/회피 도약(dash)과
강타/전투 함성/충격파(knockback)를 뺀 나머지는 전부 데이터만으로 추가된 것**이다.

### 2-6. 클라이언트 연출(이펙트) 구현
연출은 **클라이언트 전용**이며 서버 로직과 분리된다. 서버가 시전 성공 시 `UseSkill` 을
브로드캐스트하고(캐스터 제외), 클라는 그걸 근거로 이펙트만 재생한다.

- **두 재생 경로 · 같은 함수**: 캐스터 자신은 [Session.cs](../../Client/Assets/Scripts/Session.cs) `UseSkill`,
  원격 관전자는 `HandleUseSkillNotify` 에서 각각 `SkillFxDispatcher.Play(...)` 를 호출한다.
  브로드캐스트가 캐스터를 제외하므로 자신은 로컬에서 즉시 재생, 남은 브로드캐스트로 재생 → 모두 같은 연출을 본다.
- **`fx` 필드로 연출 선택**: `skill.json` 의 클라 전용 문자열 `fx`
  ("slash"/"projectile"/"meteor"/"nova"/"teleport"/"impact"/"heal"/"charge"/"chain"/"holy"/"tornado"/"cone")로
  [SkillFxDispatcher.cs](../../Client/Assets/Scripts/Skill/SkillFxDispatcher.cs) 가 분기한다.
  `fx` 는 서버가 무시하는 프레젠테이션 데이터라 **`code_name` 과 달리 서버 파생 클래스를 만들지 않는다**(서버 churn 0).
- **`element` 필드로 색 결정**: 같은 `fx` 라도 `element`("physical"/"fire"/"cold"/"lightning"/"poison"/"holy")에 따라
  색이 달라진다(`SkillFxDispatcher.Tint`). `fx` 와 마찬가지로 서버는 무시한다.
  값이 없으면 그 연출의 기본색을 쓴다 — 속성별 **상태이상**(빙결/중독 틱)은 아직 없고 색만 구분한다.
- **절차적 VFX**: [SkillFx.cs](../../Client/Assets/Scripts/Skill/SkillFx.cs) 가 아트 에셋 없이
  프리미티브/LineRenderer 로 투사체·링·확장 파문·폭발 버스트를 생성한다(수명 후 자가 소멸).
- **특수 표현은 클라 Skill 파생**: 지속/입력이 필요한 스킬만 클래스를 둔다.
  예: [AuraSkill.cs](../../Client/Assets/Scripts/Skill/AuraSkill.cs)(패시브 오라 지면 링 = [AuraRing.cs](../../Client/Assets/Scripts/Skill/AuraRing.cs), 캐릭터를 따라다니며 맥동),
  `JumpSkill`(위치 이동). 패시브는 브로드캐스트가 없어 디스패처가 아니라 이 클래스가 링을 붙였다 뗀다.
- **새 연출 추가법**: (1) `SkillFxDispatcher` 에 `case` 추가(필요 시 `SkillFx` 헬퍼 재사용) → (2) `skill.json` 에 `fx` 지정 → GameDataFlow 재생성.

로컬 프리뷰: [GameInputManager.cs](../../Client/Assets/Scripts/Input/GameInputManager.cs) 가 F5~F9 로
데이터 기반 스킬(Fireball/Meteor/Nova/Teleport/Holy Fire)을 지연 생성해 우클릭으로 연출을 미리 볼 수 있다
(순수 프리뷰, 네트워크 전송 없음 — `ActiveFxSkill` 로 감싼다).

---

## 3. 개선이 필요한 점 (TODO)

| 항목 | 현재 | 개선 방향 |
|------|------|-----------|
| **최대 체력(MaxHealth)** | 없음 — `heal` 이 오버힐 가능 | `Actor` 에 MaxHealth 도입 후 `heal`/`Increment` clamp |
| **마나/자원 비용** | 없음 — 모든 스킬 무자원 | `mana`/`mana_cost` 필드 + 소비 게이트를 `TryCast` 검증에 추가, 동기화 플래그 신설 |
| **아군 오사(friendly fire)** | AoE/오라가 **다른 캐릭터도 타격** | `Actor` 진영(faction) 판별로 대상 필터링(적/아군/자신) |
| **DoT/슬로우/디버프** | 미구현(독/냉기 = 즉발 데미지로 근사) | 대상 액터에 상태효과 리스트 + 틱 시스템. 이동속도/방어 수정치 동기화 필요 |
| **버프(스탯 증가)** | 미구현(광신/집중 등 오라 불가) | 스탯 수정치 스택 + 데미지 계산에 반영 + 동기화 |
| **진짜 토글 패시브** | 패시브 = **항상 켜짐**(보유 시 상시) | 오라 선택/토글(액티브 오라)·학습 스킬 게이트가 필요하면 토글 상태 + 동기화 추가 |
| **일회성 패시브** | pulse 효과만 지원 | "습득 시 1회 적용"(상시 스탯) 경로 별도 필요 |
| **투사체(projectile)** | 서버는 즉발 히트스캔(클라 연출만 투사체) | 서버도 이동 투사체 시뮬 + 충돌 판정 + 스냅샷 동기화 |
| **DB 보유 스킬** | 모든 비-몬스터 스킬을 전원에게 등록 | 캐릭터별 학습/장착 스킬 목록을 DB 에서 로드(`InitFromResources` 대체) |
| **연출 색상/속성** | `element` 로 색만 구분(파티클/속성 상태이상 없음) | 속성별 파티클·피격 반응 + 속성 저항/상태이상 연동 |
| **넉백 중심** | `knockback` 은 항상 캐스터 중심(시전 지점 광역기와 조합 불가) | 중심(캐스터/시전 지점) 선택 파라미터 또는 대상별 밀림 방향 계산 |
| **패시브 오라 연출 동기화** | 클라 오라 링이 로컬 선택 기반(남에게 안 보임) | 서버가 활성 패시브 목록을 동기화 → 보유 여부로 상시·원격 표시 |
| **입력 경로 이원화** | 로컬 샌드박스(GameInputManager)와 네트워크(InputHandler→Session) 분리 | 시전 입력을 Session 경로로 일원화(프리뷰는 프리뷰대로 유지) |

---

## 4. 관련 파일
- 정의/상태: [Skill.h](Skill.h) · [SkillState.h](SkillState.h) · [SkillSet.h](SkillSet.h) · [SkillRegistry.h](SkillRegistry.h)
- 효과: [SkillEffects.cpp](SkillEffects.cpp)
- 시전 컨텍스트/결과: [CastContext.h](CastContext.h)
- 데이터: [skill.json](../../Client/Assets/Resources/GameData/skill.json)
- 코드 생성: [GameDataFlow](../../GameDataFlow/README.md)
- 테스트: [SkillSystemTest.cpp](../../UnitTest/SkillSystemTest.cpp)
