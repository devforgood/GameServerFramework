# 캐릭터·몬스터 리소스 적용 기록

에셋스토어 모델과 애니메이션을 게임 프리팹에 입힌 과정을 정리한 문서입니다.
다음에 모델을 바꾸거나 새 팩을 붙일 때 같은 시행착오를 반복하지 않도록, 결론·절차·함정을 함께 적었습니다.

- 대상: `Client/Assets/Resources/Character2.prefab`, `Client/Assets/Resources/Monster.prefab`
- 기간: 2026-09-15 ~ 2026-09-16

---

## 1. 현재 상태 한눈에

| 프리팹 | 모델 팩 | 리그 | 애니메이션 | 컨트롤러 |
|---|---|---|---|---|
| `Character2` (플레이어) | Polytope Studio Lowpoly Characters<br>`PT_Lowpoly_Armors_Male_Moduar_Free` | Humanoid | Warrior Pack Bundle 2 FREE 의 **Knight** 세트(리타게팅)<br>Idle / Walk / Run | `Assets/Animations/Character/Locomotion.controller` |
| `Monster` (모든 몬스터) | Dungeon Skeletons Demo<br>`DungeonSkeleton_demoPrefab` | Generic | 같은 팩 클립<br>idle_A / walk / attack_A | `Assets/Animations/Monster/Skeleton.controller` |

런타임 코드는 프리팹을 이름으로 부릅니다(`ActorSync.Create` 의 `Resources.Load("Character2")`, `Resources.Load("Monster")`).
**파일 이름만 유지하면 게임 코드를 고칠 필요가 없습니다.** 클라이언트는 몬스터 종류를 구분하지 않으므로 슬라임·늑대도 전부 스켈레톤으로 보입니다.

### 실측 수치

| 항목 | 캐릭터 | 몬스터 |
|---|---|---|
| 서버 이동 속도 | 4.5 m/s (`Engine/Actor/Character.cpp`) | 3.5 m/s (`Engine/Actor/Monster.cpp`) |
| 블렌드 문턱값(클립 보행 속도) | Idle 0 / Walk 1.24 / Run 3.47 | Idle 0 / Walk 1.77 |
| 모델 높이 / 콜라이더 반지름 | 2.16 m / 0.43 | 2.56 m / 0.51 |
| 접지 검증(발목 높이) | 0.086 ~ 0.269 m | 걷기 0.125 ~ 0.191 m, 공격 0.135 ~ 0.172 m |

---

## 2. 빠른 실행

모든 작업은 에디터 메뉴와 CLI 양쪽에서 돌릴 수 있습니다. 도구는 프리팹을 **매번 처음부터 다시 만들므로** 프리팹을 손으로 고치면 덮어써집니다. 바꿀 것은 도구 코드에 반영하세요.

| 하고 싶은 일 | 에디터 메뉴 | CLI `-executeMethod` |
|---|---|---|
| 캐릭터 애니 재생성 + 프리팹 재조립 + 검증 | Tools > Character Resource > Rebuild Locomotion | `CharacterAnimationTool.RebuildAll` |
| 몬스터 애니 재생성 + 프리팹 재조립 + 검증 | Tools > Character Resource > Rebuild Monster | `MonsterAnimationTool.RebuildAll` |
| 프리팹만 재조립(모델 교체 등) | Tools > Character Resource > Apply All | `CharacterResourceTool.ApplyAll` |

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -quit -nographics `
  -projectPath "D:\projects\GameServerFramework\Client" `
  -executeMethod MonsterAnimationTool.RebuildAll -logFile build.log
Select-String build.log -Pattern "\[CharacterAnim\]|\[MonsterAnim\]|\[CharacterResource\]|error CS"
```

- **에디터가 이 프로젝트를 열고 있으면 CLI 는 바로 종료 코드 1 로 끝납니다.** 로그가 "Successfully changed project path" 에서 끊기면 락 때문입니다. 먼저 에디터를 닫으세요.
- 한 번 실행에 1분 남짓 걸립니다(부팅과 컴파일 포함).
- 검증에 실패하면 종료 코드 1 을 돌려줍니다. 0 이면 통과입니다.
- 결과를 눈으로 확인하려면 `-nographics` 를 빼고 임시 스크립트로 `Camera.Render()` 결과를 PNG 로 저장합니다. 수치 로그로는 자세 이상이나 분홍 머티리얼을 못 잡습니다.

---

## 3. 구조

### 도구 파일 (`Client/Assets/Scripts/Editor/`)

| 파일 | 역할 |
|---|---|
| `CharacterResourceTool.cs` | 적용 표(`Bindings`)대로 프리팹을 조립합니다: 모델 중첩, 머티리얼 셰이더 교정, 컨트롤러 연결, 콜라이더 맞춤, 게임 스크립트 부착, 최고 보행 속도 기록 |
| `CharacterAnimationTool.cs` | Humanoid 캐릭터용: 클립 임포트 설정 교정, 보행 속도 실측, 블렌드 트리 생성, 접지 검증 |
| `MonsterAnimationTool.cs` | Generic 몬스터용: 루프 설정, 보행 속도와 루트 이동량 실측, 이동+공격 컨트롤러 생성, 접지 검증 |

### 프리팹 구조

```
Character2 / Monster      루트: CapsuleCollider + Character/Monster 스크립트 (서버 좌표 전용)
  └ Model                 원본 팩 프리팹의 중첩 인스턴스 + Animator (applyRootMotion = false)
  └ HealthBar             런타임 생성, 콜라이더 꼭대기 + 0.2 m
```

모델을 자식으로 두는 이유는 두 가지입니다. 루트 Transform 을 서버 좌표 전용으로 남기고, 원본 팩이 갱신되면 중첩 프리팹을 통해 따라가게 하기 위해서입니다.

### 런타임 흐름

1. `Session.Update` 안의 `ActorSync.Tick` 이 서버 좌표를 보간해 `transform.position` 에 넣습니다.
2. `Actor.LateUpdate` 가 프레임 사이 이동량으로 속도를 재서 다음을 합니다.
   - `Speed` 파라미터에 넣습니다. 블렌드 트리가 대기·걷기·달리기를 섞습니다.
   - 속도가 최고 클립 속도(`locomotionTopSpeed`)를 넘으면 `animator.speed` 를 올려 발 미끄러짐을 메웁니다. 상한은 1.8배입니다.
   - 이동 방향으로 몸을 돌립니다. 서버가 방향을 보내지 않기 때문입니다.
3. `Monster.UpdateState` 는 서버 상태가 `AIState.Attack` 인 동안 `Attack` bool 을 켭니다. 서버는 사거리 안 교전 내내 Attack 상태이므로 공격 클립을 반복합니다.
4. 사망 시에는 렌더러를 칠하고(캐릭터 회색, 몬스터 빨강), 몬스터는 Animator 를 꺼서 포즈를 멈춥니다. 사망 모션은 없습니다.

`locomotionTopSpeed` 는 `[HideInInspector]` 필드이고 도구가 컨트롤러의 블렌드 문턱값을 읽어 프리팹에 기록합니다. 손으로 고치지 마세요.

---

## 4. 새 리소스를 붙이는 절차

1. **패키지를 임포트합니다.** `Unity.exe -batchmode -quit -projectPath ... -importPackage <파일.unitypackage>` 로도 됩니다.
   에셋스토어 다운로드 캐시는 `%APPDATA%\Unity\Asset Store-5.x\` 에 있습니다.
2. **컴파일 에러부터 확인합니다.** 팩의 데모 스크립트가 Unity 6 에서 컴파일되지 않으면 프로젝트 전체가 막힙니다(→ 7장).
3. **FBX 메타에서 리그 종류를 확인합니다.** `animationType: 3` 은 Humanoid, `2` 는 Generic 입니다.
   - Humanoid: 다른 팩 애니메이션을 리타게팅해 쓸 수 있습니다. `CharacterAnimationTool` 방식을 따르세요.
   - Generic: 같은 팩 클립만 붙습니다(뼈 경로가 같아야 함). `MonsterAnimationTool` 방식을 따르세요.
4. **머티리얼 셰이더를 확인합니다.** 이 프로젝트는 **Built-in 렌더 파이프라인**입니다. URP/HDRP 팩은 분홍색이 됩니다.
   `CharacterResourceTool.FixPipelineMaterials` 가 셰이더를 잃은 머티리얼을 Standard 로 바꿔 줍니다.
5. **`CharacterResourceTool.Bindings` 에 항목을 추가합니다.** 원본 프리팹, 대상 Resources 경로, 스크립트 타입, 컨트롤러 경로를 적습니다.
6. **애니메이션 도구를 만들거나 기존 도구의 클립 경로를 바꿉니다.** 반드시 `RebuildAll` 로 컨트롤러 → 프리팹 → 검증 순서를 묶으세요(→ 6장 "컨트롤러 참조 끊김").
7. **CLI 로 돌려 로그를 확인합니다.** 실측 속도, 루트 이동량, 접지 수치를 봅니다.
8. **PNG 로 렌더해 봅니다.** 방향(얼굴이 +Z), 머티리얼 색, 자세, 체력바 위치를 확인합니다. 측면 렌더는 기울기를 과장하므로 3/4 각도로 보세요.
9. **실제 플레이로 확인합니다.** 발 미끄러짐과 움직임의 자연스러움은 정지 프레임으로 판단할 수 없습니다.

### 애니메이션 팩 고르는 기준

- **InPlace(제자리) 클립**이거나 루트 이동을 구워 없앨 수 있어야 합니다. 위치는 서버가 줍니다.
- **보행 속도가 서버 속도에 가까울수록** 좋습니다. 차이가 1.8배를 넘으면 발이 미끄러집니다.
- **전투 자세(몸을 숙인 One Hand Up 등)가 아닌 기본 이동**을 고릅니다.
- 판단이 서지 않으면 **팩 원본 모델에 같은 클립을 걸어 비교**하세요. 가장 빠른 판별법입니다.

---

## 5. 히스토리

### 2026-09-15 · 캐릭터

| 단계 | 한 일 | 결과 / 교훈 |
|---|---|---|
| 1 | CLI 로 에셋 작업이 되는지 확인 | 됨. 라이선스는 이미 활성화돼 있음. 에디터가 열려 있으면 락에 막힘 |
| 2 | Polytope 남성 갑옷 모델을 `Character2` 에 적용 | 중첩 프리팹 구조로 조립. 콜라이더 반지름이 망토 때문에 0.93 으로 잡혀 키의 0.2배로 상한을 둠 |
| 3 | 걷기 애니를 **근육 커브로 직접 제작** | 기계적이고 어색해서 **폐기**. 제작 과정에서 `RootT.y` 누락, 근육 이름 띄어쓰기, Stretch 부호 문제를 겪음 |
| 4 | **RPG Animations Pack FREE**(DoubleL) 의 One Hand Up 이동으로 교체 | 몸통이 24도 숙은 전투 자세. 임포트 설정으로 안 고쳐져 **폐기**. 팩은 `Assets/DoubleL` 에 남아 있으나 미사용 |
| 5 | **Warrior Pack Bundle 2 FREE**(ExplosiveLLC) 의 Knight 세트로 교체 | 채택. 데모 스크립트 컴파일 에러 2건 수정 |
| 6 | 발 미끄러짐 대응 | 클립 보행 속도 실측을 블렌드 문턱값으로 사용하고, `Actor` 가 재생 속도로 보정 |
| 7 | Knight 도 기울어 보여 리타게팅 오류를 의심 | 원본 Knight 모델 29.2도, 우리 캐릭터 23.9도로 **리타게팅은 정상**. 측면 렌더의 착시였음 |

### 2026-09-16 · 몬스터

| 단계 | 한 일 | 결과 / 교훈 |
|---|---|---|
| 1 | Dungeon Skeletons Demo 를 `Monster` 에 적용 | Generic 리그라 리타게팅 불가. 같은 팩 클립으로 컨트롤러 구성 |
| 2 | 걷기 속도 실측 | 1.77 m/s, 루트 이동량 0 m(제자리 클립). 방향은 +Z 로 정상 |
| 3 | 머티리얼이 URP Lit 을 가리킴 | 분홍색 방지를 위해 Standard 로 자동 교정 추가. 팩 `.mat` 2개가 수정됨 |
| 4 | 공격·사망 연출 | Attack 상태에서 공격 클립 반복. 사망 시 포즈 정지와 빨강 칠 |
| 5 | 모델 키 2.56 m 로 체력바가 머리에 묻힘 | 체력바 높이를 콜라이더 높이 기준으로 변경 |

---

## 6. 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| CLI 가 바로 종료 코드 1, 로그가 "Successfully changed project path" 에서 끊김 | 에디터가 같은 프로젝트를 열고 있음(프로젝트 락) | 에디터를 닫고 `Get-Process Unity` 로 확인 |
| 모델이 분홍색 | URP/HDRP 셰이더 머티리얼(프로젝트는 Built-in) | `FixPipelineMaterials` 가 Standard 로 교정. 로그에 "텍스처 없음"이 떠도 저장된 `_MainTex` 가 남아 실제로는 붙음 |
| T 포즈로 서 있음 | Animator 에 컨트롤러가 없음 | `RebuildAll` 로 재조립 |
| 컨트롤러 참조 끊김, 접지 검증이 "컨트롤러가 없다" | 컨트롤러를 지우고 새로 만들면 **GUID 가 바뀜** | 항상 `RebuildAll`(컨트롤러 → `ApplyAll` → 검증) 사용. 검증 전 프리팹 `ForceUpdate` 임포트 |
| `SampleAnimation` 을 해도 뼈가 안 움직임 | 프리팹 루트를 넘김 | **Animator 가 붙은 GameObject(Model)** 를 넘길 것 |
| 발이 땅에서 미끄러짐 | 클립 보행 속도 < 서버 이동 속도 | 실측 문턱값과 재생 속도 보정(상한 1.8배). 더 빠른 클립을 넣거나 서버 속도 조정 |
| 몸통이 앞으로 크게 숙음 | 전투 자세 클립이거나 루트 회전을 원본 기준으로 구움 | 임포트 설정을 Body Orientation 기준으로 두고, 그래도 숙으면 원본 모델과 비교해 클립 자체 문제인지 판별 |
| 허리까지 땅에 묻힘 | 높이를 발이 아닌 원점 기준으로 구움 / 직접 만든 휴머노이드 클립에 `RootT.y` 없음 | `heightFromFeet = true` / `RootT.y` 커브 추가 |
| 루프마다 몸이 뒤로 튐 | 제자리가 아닌 클립(루트가 전진) | Humanoid 는 `lockRootPositionXZ`. Generic 은 `MonsterAnimationTool` 이 루트 이동량을 경고하니 InPlace 클립으로 교체 |
| 옆으로 미끄러지며 걸음 | 이동 방향으로 회전하지 않음 | `Actor.LateUpdate` 가 이동 방향으로 회전. 모델이 -Z 를 보면 실측 로그의 속도 부호가 음수로 나옴 |
| 체력바가 몸에 묻힘 | 고정 높이 | 콜라이더 높이 기준으로 배치(현재 적용됨) |
| 렌더 PNG 에서 다리가 잘림 | 카메라 종횡비 미설정 | `cam.aspect` 를 명시하고 렌더러 경계로 프레이밍 |

### 직접 휴머노이드 클립을 만들어야 할 때

권장하지 않지만 필요하다면 다음을 지키세요.

- `RootT.y` 커브가 없으면 엉덩이가 원점에 놓여 허리까지 묻힙니다.
- 근육 바인딩 이름은 띄어쓰기를 살립니다(`"Left Upper Leg Front-Back"`). 틀리면 조용히 무시됩니다.
- Stretch 계열(무릎·팔꿈치)은 양수가 펴는 방향입니다.

---

## 7. 서드파티 수정 목록

팩을 재임포트하거나 업데이트하면 아래 수정이 사라지니 다시 적용해야 합니다.

| 파일 | 수정 | 이유 |
|---|---|---|
| `Assets/ExplosiveLLC/SuperCharacterController/SuperCharacterController/Core/SuperCharacterController.cs` | `public struct Ground` 위의 `[SerializeField]` 제거 | CS0592 (구조체에 붙일 수 없음) |
| `Assets/ExplosiveLLC/Warrior FREE/Code/WarriorController.cs` (87행) | `AnimatorUpdateMode.AnimatePhysics` → `AnimatorUpdateMode.Fixed` | CS0619 (Unity 6 에서 제거된 API) |
| `Assets/DungeonCharacters/Skeletons_demo/models/Materials/DS_skeleton_standard.mat`, `DemoEquipment.mat` | 셰이더 URP Lit → Standard | Built-in 파이프라인에서 분홍색. 도구가 자동 수정 |
| Knight 클립 FBX 메타(Idle/Walk/Run) | 루프, 루트 회전·높이·XZ 굽기 설정 | 도구(`ConfigureImporters`)가 자동 수정 |
| Dungeon Skeletons 공격 클립 FBX 메타 | `loopTime` 켬 | 도구가 자동 수정 |

---

## 8. 알려진 한계와 후보 작업

- **캐릭터는 사실상 항상 달립니다.** 서버 속도 4.5 m/s 가 Run 문턱값 3.47 보다 높기 때문입니다. 걷기를 보이려면 서버 속도를 3 m/s 안팎으로 낮춰야 합니다.
- **몬스터 발이 약간 미끄러질 수 있습니다.** 걷기 1.77 m/s 대비 서버 3.5 m/s 로 필요 배율 약 2배가 상한 1.8배를 넘습니다. 달리기 클립이 있는 팩이 있으면 추가하세요.
- **몬스터 종류별 모델 구분이 없습니다.** 필요하면 서버 `AddAgent` 에 몬스터 id 를 싣고 `ActorSync.Create` 에서 프리팹을 골라야 합니다.
- **사망·피격 모션이 없습니다.**
- **몬스터 상태별 색 표시가 동작하지 않습니다.** `ActorSync.UpdateMonsterVisuals` 는 루트 `MeshRenderer` 를 찾는데, 모델 프리팹에는 없습니다.
- **스켈레톤(2.56 m)이 캐릭터(2.16 m)보다 큽니다.** 크기를 맞추려면 `Model` 자식의 스케일을 조정하도록 도구에 반영하세요.
- **`Assets/DoubleL`(RPG Animations Pack) 은 미사용**입니다. 필요 없으면 삭제해도 됩니다.
