# 캐릭터·몬스터·배경·이펙트 리소스 적용 기록

에셋스토어 모델·애니메이션·배경·이펙트를 게임에 입힌 과정을 정리한 문서입니다.
다음에 모델을 바꾸거나 새 팩을 붙일 때 같은 시행착오를 반복하지 않도록, 결론·절차·함정을 함께 적었습니다.

- 대상: `Client/Assets/Resources/Character2.prefab`, `Client/Assets/Resources/Monster.prefab`, 맵 씬 배경(→ 9장), 스킬·피격 이펙트(→ 10장)
- 기간: 2026-09-15 ~ 2026-09-19

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
| Starting Village 배경 재생성 + 검증 + 미리보기 | Tools > Environment > Dress Starting Village | `EnvironmentDressingTool.BuildStartingVillage -previewDir <폴더>` |
| 이펙트 라이브러리 재생성 + 검증 + 미리보기 | Tools > VFX > Build Library | `VfxLibraryTool.BuildAll -previewDir <폴더>` |
| 팩 이펙트 전부 찍어 보기(고를 때) | — | `VfxLibraryTool.RenderCandidates -previewDir <폴더>` |
| 실제 맵 조명·안개 속에서 이펙트 보기 | — | `VfxLibraryTool.RenderInScene -previewDir <폴더>` |
| 게임 전용 이펙트만 다시 저작 | Tools > VFX > Author Game Effects | (Build Library 가 먼저 부름) |

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
   - 속도가 최고 클립 속도(`locomotionTopSpeed`)를 넘으면 `animator.speed` 를 올려 발 미끄러짐을 메웁니다. 상한은 2배이고,
     이동이 아닌 동작(몬스터 공격)에는 걸지 않습니다(`ScalePlaybackWithSpeed`).
   - 이동 방향으로 몸을 돌립니다. 서버가 방향을 보내지 않기 때문입니다.
3. `Monster.UpdateState` 는 서버 상태가 `AIState.Attack` 인 동안 `Attack` bool 을 켭니다. 서버는 사거리 안 교전 내내 Attack 상태이므로 공격 클립을 반복합니다.
4. 사망 시에는 렌더러를 칠하고(캐릭터 회색, 몬스터 빨강), 몬스터는 Animator 를 꺼서 포즈를 멈춥니다. 사망 모션은 없습니다.
   actor id 는 재사용되므로, 그 오브젝트에 살아 있는 상태가 다시 오면 `Monster` 가 색·Animator·체력바를 되돌립니다.

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
- **보행 속도가 서버 속도에 가까울수록** 좋습니다. 차이가 2배를 넘으면 발이 미끄러집니다.
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

### 2026-09-18 · 몬스터 애니메이션 점검

"추격·공격 애니메이션이 이상하다"를 Play Mode 테스트(`-runTests -testPlatform PlayMode`)로 재현해 가며 확인했다.
에디터에서 클립을 직접 샘플링하는 것만으로는 런타임 문제를 못 잡는다 — 실제 게임 루프에서 재야 한다.

| 확인한 것 | 결과 |
|---|---|
| 클립·컨트롤러·전이 | 정상. 공격 클립 1.50초(서버 쿨타임 1.5초와 같음), 루트 이동 0, 전이 0.08초 |
| 서버 10Hz + 보간 경로 | 정상. Speed 3.49~3.50, 걷기 클립 재생, 발뼈 36.5도 움직임 |
| 추격 배속 | 필요 1.98배인데 상한 1.8배로 막혀 디딘 발이 0.24 m/s 끌렸다 → 상한 2배로 |
| 공격 배속 | 추격 배속 1.80이 공격까지 이어졌다 → 이동 동작에만 배속 |
| 상태 깜빡임 | Attack 0.1초 뒤 Detect 가 와도 공격이 1.40초 재생되도록 전이 수정 |
| Animator 컬링 | `CullUpdateTransforms` 라 렌더러가 안 보이면 **포즈가 전혀 갱신되지 않는다**(뼈 0도). 상태 기계는 계속 돈다 — 화면 밖 몬스터가 굳은 자세로 미끄러지는 이유 |

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
| 몬스터 공격 동작이 너무 빠름 | `animator.speed` 는 컨트롤러 **전체**에 걸린다. 걷기 보정 배속(최대 2배)이 공격 클립에도 그대로 남았다(실측 1.80배) | `Actor.ScalePlaybackWithSpeed` 로 이동 동작에만 배속을 건다. 몬스터는 Attack 상태에서 등속 |
| 몬스터 공격 동작이 보이지 않음 | 서버는 사거리 안팎을 오갈 때 Attack ↔ Detect 를 한 틱(0.1초) 단위로 뒤집는다. 예전 전이는 상태가 풀리는 즉시 공격을 끊어 1.5초짜리 공격이 보이기 전에 사라졌다 | 컨트롤러의 Attack → Locomotion 전이에 `hasExitTime`(0.9) — 한 번 시작한 공격은 끝까지 재생 |
| 죽었던 자리의 몬스터가 대기 자세로 미끄러지고 공격도 안 함 | actor id 는 재사용된다. 사망 연출이 애니메이터를 끄고 몸을 빨갛게 칠해 둔 오브젝트를 새 몬스터가 물려받았다 | `Monster.UpdateState` 가 살아 있는 상태를 받으면 색·애니메이터·체력바를 되돌린다 |
| 캐릭터·몬스터가 바닥에서 약 0.2 m 떠 있음 | **모델 크기 문제가 아니라 navmesh 높이.** Recast 가 바닥을 복셀로 쌓으며 높이를 한 칸(`cellHeight` 0.2) 올림해 navmesh 표면이 바닥보다 높다. 서버 y 가 평지에서 전부 0.2(실측 3000점 중 앙값 0.2) | `Actor.GroundedPosition`/`SnapToGround` 가 그릴 때만 바닥 콜라이더 높이에 붙인다. 차이가 −0.25~+0.35 m 를 벗어나면(다른 면을 맞힘) 서버 y 유지. 점프 착지점도 같은 기준 |
| 경사로에서 캐릭터가 파묻힘/뜸 | 옛 `TerrainBuilder` 경사로에 콜라이더가 없어 레이가 밑의 평지에 닿음 | 배경 도구가 경사로에 MeshCollider 를 채운다. 가파른 경사로 일부(약 4%)는 navmesh 세부 높이 오차로 서버 y 를 그대로 쓴다 |
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
| `Assets/ExplosiveLLC/Editor/SetupInputLayers.cs` | 삭제(빈 `Editor` 폴더도 삭제) | 에셋이 임포트될 때마다 "Load Input and Tag Presets" 창을 띄움. 데모 컨트롤러용 안내라 클립만 쓰는 우리와 무관 |
| `Assets/DungeonCharacters/Skeletons_demo/models/Materials/DS_skeleton_standard.mat`, `DemoEquipment.mat` | 셰이더 URP Lit → Standard | Built-in 파이프라인에서 분홍색. 도구가 자동 수정 |
| `Assets/Flooded_Grounds/PostProcessing/` | **폴더째 삭제** | 2018년에 폐기된 Post Processing Stack v1. 에디터 코드가 Unity 6 에서 컴파일되지 않고(CS0619·CS0104, 고치면 다음 에러가 연쇄로 나옴) 프로젝트 전체를 막았다. 데모 `Scene_A` 카메라에 빈 스크립트 참조가 남지만 게임과 무관 |
| `Assets/VFX/Eric VFX Studio/Resource/Materials/*.mat` (5개) | 셰이더 URP Particles/Unlit → Legacy Shaders/Particles/Alpha Blended, `_BaseMap` → `_MainTex` | Built-in 파이프라인에서 분홍색. `VfxLibraryTool` 이 자동 수정 |
| `Assets/_TerrainAutoUpgrade/` | 지우지 말 것 | Unity 가 `Scene_A` 지형을 열며 만든 TerrainLayer 3개. 우리 배경 지형이 이 레이어를 쓴다 |
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

---

## 9. 배경 (Flooded Grounds)

### 원칙: 게임 지오메트리는 건드리지 않고 덧입힌다

서버 이동은 씬의 바닥·장애물·경사로로 구운 navmesh 를 따릅니다. 그래서 배경 작업은 **그 오브젝트의 렌더러만 끄고** 같은 자리에 보기 좋은 껍데기를 올립니다.
MeshFilter·콜라이더는 남기므로 NavMesh 재굽기와 클릭 이동은 그대로입니다. 서버 데이터(Map.json, navmesh)는 바뀌지 않습니다.

| 게임 지오메트리 | 배경 |
|---|---|
| 바닥 `BasePlane` (50×50 m) | Terrain 240 m. 플레이 영역 +3 m 까지 높이 정확히 0, 바깥은 북쪽 언덕 / 남쪽 침수지 |
| 장애물 큐브 2 m | 둥근 바위(CobbleRock A/E)를 큐브의 1.2배 크기로 + 자갈 |
| 경사로(한 장짜리 사면) | 사면과 똑같은 윗면에 뒷벽·옆면을 막은 흙 둔덕 + 자갈 |
| 경계 | 울타리(북·동·서 높은 것, 남 낮은 것), 게이트 자리는 돌 아치와 바깥으로 이어지는 둑길 |
| 바깥 | 나무 420그루(Terrain 나무), 풀, 오두막·초소·라디오탑, 서쪽 묘지, 동쪽 전봇대·폐차, 남쪽 물·배·난파선, 원경 공장(안개 속) |

- 생성물은 씬 루트 `Environment`(`EnvironmentDressing` 컴포넌트) 밑과 `Assets/Environment/{씬 이름}/`(지형·물 메시), `Assets/Environment/Materials/` 에 모입니다.
- 도구는 **매번 통째로 다시 만듭니다**(시드는 씬 이름이라 결과는 같음). 배치를 바꾸려면 `EnvironmentDressingTool.cs` 를 고치세요.
- 실행 시 검증: NavMesh 입력 메시 수 유지, 맵 크기(x·z) 유지, 켜진 콜라이더 0개, 플레이 영역 지형 높이 = 바닥 높이. 하나라도 어긋나면 종료 코드 1.
- `MapPipeline.BakeableMeshes`·`SceneBounds` 는 `EnvironmentDressing` 밑을 건너뜁니다. 같이 구우면 울타리 바깥 언덕이 걸을 수 있는 땅이 됩니다.

### 설계에서 지킨 것

- **남쪽에는 키 큰 것을 두지 않습니다.** 카메라(`DiabloCamera`, yaw 0)가 남쪽에서 내려다봐서 남쪽 가장자리의 나무·건물은 캐릭터를 가립니다. 남쪽 32 m 안에는 나무가 없고, 저지대라 물에 잠겨 있습니다.
- **배경 콜라이더는 전부 끕니다.** 클릭 이동은 마우스 레이가 처음 맞은 점으로 가므로 지붕·나무에 맞으면 엉뚱한 곳으로 갑니다. Terrain 에도 콜라이더가 없어 울타리 바깥을 클릭하면 예전처럼 반응하지 않습니다.
- **분위기 값은 Scene_A 에서 가져와 탑뷰에 맞게 조정했습니다.** 안개 원본 0~400 m → 30~130 m(카메라 거리 10~24 m 에서 원본은 안 보임), 환경광 Skybox → Trilight(조명을 굽지 않은 씬에서 안정), 해 고도 25° → 50°(그림자가 캐릭터를 덮지 않게).

### 2026-09-16 · Starting Village

| 단계 | 한 일 | 결과 / 교훈 |
|---|---|---|
| 1 | 범위 결정 | Scene_A(1024 m 지형)로 맵을 통째로 바꾸는 대신 **기존 맵 꾸미기**를 택함. navmesh·Map.json·서버 데이터를 다시 만들 필요가 없다 |
| 2 | 팩 임포트 후 컴파일 에러 | PostProcessing v1 에디터 코드. 두 곳을 고치자 다음 에러가 나와 폴더째 삭제 |
| 3 | 프리팹 배치 | 팩 프리팹 루트에 원본 씬 좌표(수백 m)가 박혀 있다. 루트가 아니라 **렌더러 경계의 바닥 중심**으로 맞춘다(`Place`) |
| 4 | 플레이 영역이 물에 잠겨 보임 | 처음엔 Terrain LOD 를 의심했으나, 물을 끄고 찍어 보니 지형은 정상. `FG_PBR_Water` 가 파도를 **오브젝트 공간 높이**로 만들어, Plane 을 43배 키우자 파도가 ±4 m 가 됐다. 실제 크기 격자 메시를 스케일 1 로 사용 |
| 5 | 경사로에 판자(WoodPath) 얹기 | 허공에 뜬 것처럼 보이고 받침돌이 판자를 뚫어 폐기. 닫힌 흙 둔덕으로 교체 |
| 6 | 장애물에 Rock_A | 세로로 긴 판석이라 탑뷰에서 비석처럼 보여 뺌 |
| 7 | 미리보기 렌더가 `ProjectSettings/EditorSettings.asset` 을 다시 씀 | `EditorSettings.asyncShaderCompilation` 은 파일에 저장된다. 세션 한정인 `ShaderUtil.allowAsyncCompilation` 으로 교체 |

### 배경 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| 캐릭터가 물에 잠겨 보임, 물 경계가 긴 직선 | 물 셰이더 파도가 오브젝트 스케일만큼 커짐 | 물은 스케일 1 메시로(`BuildWater`). 원인을 가를 땐 물을 끈 샷(`env_overview_nowater.png`)과 비교 |
| 미리보기 전경이 회색뿐 | 안개 끝(130 m)보다 먼 카메라 | 전경 샷만 안개를 끄고 찍음(씬은 이미 저장된 뒤) |
| 소품이 엉뚱한 곳(수백 m 밖)에 생김 | 팩 프리팹 루트 좌표 | `Place` 가 경계 기준으로 옮김. 직접 배치할 때도 같은 방식을 쓸 것 |
| 배경을 클릭하면 이상한 곳으로 이동 | 켜진 콜라이더 | 도구가 끄고 검증함. 손으로 추가한 오브젝트는 직접 끌 것 |
| CLI 후 `ProjectSettings/*.json`·`UserSettings` 가 바뀜 | Unity 가 열 때 파일을 새 형식으로 다시 씀 | 의도한 변경이 아니면 `git checkout` 으로 되돌림 |

### 다른 맵에 적용할 때

- `BuildStartingVillage` 는 씬 경로만 고정돼 있고 `Dress(scene)` 는 씬의 가장 넓은 메시를 바닥, `Cube` 메시를 장애물, 나머지를 경사로로 읽습니다. Field1 은 같은 구조(`Generated_Simple_SimplePlane_Terrain`)라 그대로 됩니다.
- **Field2·Dark Forest 는 구조가 달라**(`BasePlane` 이 없음) 먼저 씬 구성을 확인해야 합니다.
- 건물 배치(`PlanBuildings`)는 플레이 영역 기준 상대 좌표입니다. 게이트 길을 막는 건물은 자동으로 빠집니다.
- 맵마다 분위기를 다르게 하려면 `ApplyAtmosphere` 와 지형 경향(`TerrainShape.RawHeight` 의 북/남 높이)을 인자로 빼세요.

### 배경의 한계

- **팩(2.6 GB)은 저장소에 없습니다**(`Client/.gitignore`). 새 PC 에서는 에셋스토어에서 Flooded Grounds 를 받아 임포트하고 `PostProcessing` 폴더를 지워야 씬 배경이 보입니다. 에셋 GUID 는 패키지 메타에 고정돼 있어 다시 임포트해도 씬 참조가 이어집니다.
  `_TerrainAutoUpgrade` 의 TerrainLayer 3개(작음)는 커밋했습니다. Unity 가 새로 만들면 GUID 가 달라져 지형 텍스처 참조가 끊기기 때문입니다.
- 경사로 둔덕은 게임 지오메트리 모양 그대로라 각진 상자처럼 보입니다. 자연스럽게 하려면 `TerrainBuilder` 경사로 자체를 바꾸고 navmesh 를 다시 구워야 합니다.
- 씬에 원래 있던 게이트(파란 원기둥)·스폰 지점(빨간 원기둥) 마커 메시는 그대로 보입니다.

---

## 10. 이펙트 (VFX)

### 현재 상태

**게임 전용 이펙트를 코드로 만들어 쓴다**(`VfxAuthoringTool` → `Assets/VFX/Game`, 약 4 MB, 저장소에 포함). 받아 둔 팩은 폭발 속 화염 구(Eric) 하나만 쓴다.

| 출처 | 셰이더 | 사용 |
|---|---|---|
| `Assets/VFX/Game` (직접 저작) | Built-in Legacy 파티클(가산·알파) | 전부. 절차적 텍스처(발광·불꽃·연기 2×2·고리·초승달·광선) + 파티클 조립 + 순간 점광원 |
| Eric VFX Studio Free RPG Sprite Sheet | URP → Legacy 파티클로 교정 | `Explosion 01` 을 폭발 프리팹 안에 중첩 |
| Vefects Flipbook VFX Bundle Lite | 팩 자체 BIRP 셰이더 | **미사용**(아래 이력). 196 MB — 필요 없으면 지워도 된다 |
| Free Slash VFX | URP 전용 Shader Graph | **미사용**. Scene Color/Depth(왜곡)에 기대서 Built-in 타깃을 붙여도 같은 모양이 안 나온다. 프로젝트를 URP 로 옮기기 전엔 못 쓴다 |

직접 만든 이펙트의 규칙:

- 텍스처는 흰색 + 알파 모양만 갖는다. 색은 파티클 색으로 입혀서 속성 색(`Vfx.Play` 의 tint)이 그대로 먹는다. `nova`·`circle`·`projectile` 은 흰색으로 두고 호출 측이 칠한다.
- 크기는 실제 미터로 저작한다(카탈로그 size 0 = 재배율 없음). 광역은 지름 2 m, 베기 초승달은 피벗이 호의 중심(캐스터)이다.
- 바닥에 눕히는 층(고리·초승달)은 25 cm 띄운다. 먼지·연기는 밝은 풀밭보다 확실히 어둡게 한다(→ 이력 3).
- 번쩍임에만 파티클 조명 모듈(최대 1개)을 달아 주변 바닥을 잠깐 밝힌다. 폭발·파동·낙뢰·빛기둥·순간이동.

### 구조

```
VfxAuthoringTool (코드로 텍스처·머티리얼·프리팹 저작 → Assets/VFX/Game)
VfxLibraryTool.Catalog (코드 표: 키 → 프리팹, 목표 크기, recolor)
   └ Tools > VFX > Build Library → 저작 → Resources/VfxLibrary.asset
        └ 런타임 Vfx.Play(key, 위치, 배율, 색)
             ├ SkillFxDispatcher  스킬 fx·element 별 연출(지면 호·링 선은 판정 범위 표시로 유지)
             ├ Actor.TakeDamage   피격 "hit.physical" (액터당 0.25초 간격)
             └ Monster.ShowDeathEffect  사망 "death"
```

- 키 규약은 `용도.속성`(예 `hit.fire`, `nova`). 디스패처는 `용도.속성` 이 있으면 그것, 없으면 `용도` 에 속성 색을 입혀 쓴다.
- **라이브러리에 키가 없으면 예전 절차적 도형(`SkillFx`)으로 대신한다.** 그래서 에셋이 빠져도 연출이 사라지지 않는다.
- 크기: 카탈로그의 `size` 는 배율 1 에서의 크기(m)다. 광역(폭발·파동·마법진)은 2 m 라서 반경을 넘기면 지름 = 2 × 반경.
- `Vfx.Play` 는 인스턴스마다 반복을 끄고(한 주기), `scalingMode` 를 Hierarchy 로, 수명이 끝나면 파괴한다. 팩 프리팹 원본은 건드리지 않는다.
  투사체 몸체처럼 끝을 호출 측이 정하는 것은 `looping: true` + `follow` 로 붙이고 그 오브젝트를 파괴한다.

### 2026-09-19 · 적용

| 단계 | 한 일 | 결과 / 교훈 |
|---|---|---|
| 1 | 팩 임포트가 URP·Shader Graph 패키지를 manifest 에 추가함 | 렌더 파이프라인은 여전히 Built-in(`m_CustomRenderPipeline` 0). URP 머티리얼은 분홍색 |
| 2 | 후보 62개를 게임 카메라 각도로 렌더 | 처음엔 대부분 빈 화면 — **수명 비율로 샘플링해서 타격 이펙트(수명 0.3초)가 이미 사라진 뒤를 찍었다.** 절대 시각(0.08/0.18/0.32/0.55초)으로 바꿔 해결 |
| 3 | 배율 자동 계산 | 렌더러 `bounds` 는 최대 크기로 잡혀 실제의 몇 배(불꽃 1 m → 12 m). **살아 있는 파티클 위치 ± 현재 크기**로 잰다 |
| 4 | 속성별 색 입히기 | 파티클 `startColor` 로는 안 바뀐다. **Vefects 셰이더는 색을 머티리얼 `_R/_G/_B/_Outline` 에서 읽고 정점색은 투명도에만 쓴다.** MaterialPropertyBlock 으로 색 속성을 바꾼다(밝기 유지, 색조만). Eric 팩은 색이 텍스처에 구워져 있어 칠할 수 없다 |
| 5 | 플레이 모드 스모크(임시 스크립트, 배치모드) | 24개 키 전부 재생·정리, 스킬 연출 49개 예외 없음. 반복 주기 10초짜리 번개 투사체가 남아 `emitTime`·`looping` 도입. 버스트 없이 흘리는 번개 파동은 제외 |

### 2026-09-19 · 팩 이펙트 → 직접 저작으로 교체

"게임과 어울리지 않는다"는 피드백. 이 PC 에 받아 둔 다른 이펙트 팩은 없었다(에셋스토어 캐시에 세 팩뿐).

| 단계 | 한 일 | 결과 / 교훈 |
|---|---|---|
| 1 | **실제 맵(Starting Village) 조명·안개 속**에서 게임 기본 카메라로 찍는 `RenderInScene` 추가 | 회색 바닥 미리보기로는 어울림을 판단할 수 없었다. 맵은 사실적인 PBR(바위·풀·해골·갑옷)인데 Vefects 는 검은 외곽선 셀 셰이딩·도트라 **스티커처럼 떠 보였다** |
| 2 | 발광·불꽃 튐·연기·충격파·점광원으로 21개 이펙트를 코드로 조립 | 빛이 바닥을 물들여 맵에 녹아든다. Eric 화염 구는 사실적이라 폭발 안에 남김 |
| 3 | 씬 미리보기에서 고리·먼지 일부가 안 보임 | **바닥 콜라이더 높이(0)보다 실제 지형 표면이 조금 높아 8 cm 에 눕힌 고리가 묻혔다**(0.3 m 올려 찍어 확인) → 25 cm. 먼지는 햇빛 받은 풀밭과 명도가 같아 사라짐 → 어둡게, 사망엔 떠나는 빛(영혼)을 더함 |
| 4 | 플레이 모드 스모크 | 키 21개 재생·정리, 스킬 연출 49개 예외 없음, 6초 뒤 남은 파티클·조명 없음 |

### 이펙트 트러블슈팅

| 증상 | 원인 | 해결 |
|---|---|---|
| 이펙트가 분홍색 | URP 셰이더 머티리얼 | Eric 팩은 도구가 Legacy 로 교정. Free Slash 는 못 씀. 새 팩은 `RenderProblems` 검증이 실패로 알려 준다 |
| 미리보기에 아무것도 안 찍힘 | 샘플 시각이 파티클 수명 뒤 | 로그의 `파티클 수 0.08s:1 ...` 를 본다. 0 이면 시뮬레이션·시각 문제, 있는데 안 보이면 셰이더·크기 문제 |
| 속성 색이 안 먹거나 탁해짐 | 색이 텍스처에 구워진 이펙트(Eric)에 색을 곱함 | 흰/단색 Vefects 이펙트에 `recolor` 를 쓰고, Eric 이펙트엔 색을 넘기지 않는다 |
| 이펙트가 끝나지 않고 남음 | 팩 프리팹의 한 주기가 김(10초) | 카탈로그 `emitTime` 으로 방출을 자른다 |
| Poison_Burst 가 분홍 | 팩에 머티리얼 참조가 빠져 있음(GUID 없음) | 사용 안 함 |
| 바닥 고리가 어떤 자리에선 안 보임 | 지형 표면이 바닥 콜라이더보다 높음(특히 경사로 둔덕 근처) | 바닥 층을 25 cm 띄움. 미리보기 캐릭터도 둔덕에서 떨어뜨려 세움 |
| 먼지·연기가 안 보임 | 밝은 풀밭과 명도가 비슷함 | 바닥보다 확실히 어두운 색 + 진한 알파. 회색 바닥 미리보기에선 보여서 놓치기 쉽다 — `RenderInScene` 으로 확인 |

### 이펙트의 한계

- **피격 이펙트는 속성을 모른다.** 클라는 체력 감소만 보고 피격을 알기 때문에 항상 `hit.physical` 이다. 속성 피격을 보이려면 서버가 피해 알림에 스킬 id 나 속성을 실어야 한다.
- **몬스터 공격(서버 AI 상태)에는 이펙트가 없다.** 피격 쪽 이펙트로만 보인다.
- 이펙트 소리가 없다(직접 만든 이펙트엔 AudioSource 가 없다).
- **Vefects 팩은 이제 쓰지 않지만 `Demo/Resources` 폴더(HDRI 약 49 MB)는 `Resources` 라 빌드에 통째로 들어간다.** 팩째(`Assets/VFX/Vefects`) 지우는 게 좋다.
- 파티클 조명은 Forward 렌더링에서 픽셀 조명 하나씩을 더 쓴다. 광역 스킬이 한꺼번에 많이 터지면 비용이 오른다(번쩍임 수명 0.2초 안팎이라 짧다).
