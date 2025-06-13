using System.Collections;
using UnityEngine;

public class NormalAttackSkill : Skill
{
    public float jumpDuration = 1f;
    public float jumpHeight = 3f;
    public string Name => "NormalAttack";
    private Coroutine jumpCoroutine;
    private Coroutine swingCoroutine;

    // 부채꼴 공격 범위 설정
    public float attackRadius = 3f;     // 공격 범위
    public float attackAngle = 90f;     // 부채꼴 각도
    private bool isSelected = false;

    // 무기 휘두르기 설정
    public float swingDuration = 0.5f;  // 휘두르는 시간
    private Transform weaponTransform;
    private Vector3 originalWeaponRotation;
    private Vector3 originalWeaponPosition;
    private bool isSwinging = false;

    override public void OnSelect(GameInputManager context) 
    {
        Debug.Log($"[NormalAttackSkill] OnSelect");
        isSelected = true;
        isSwinging = false; // 선택 시 스윙 상태 초기화

        // Weapon 오브젝트 찾기 시도
        if (context.player != null)
        {
            // 먼저 직접적인 자식으로 찾기
            weaponTransform = context.player.transform.Find("Weapon");
            
            // 못 찾았다면 전체 자식들 중에서 찾기
            if (weaponTransform == null)
            {
                weaponTransform = context.player.transform.GetComponentInChildren<Transform>(true).Find("Weapon");
            }

            // 그래도 못 찾았다면 임시 Weapon 오브젝트 생성
            if (weaponTransform == null)
            {
                Debug.LogWarning("[NormalAttackSkill] Creating temporary weapon object");
                GameObject weaponObj = new GameObject("Weapon");
                weaponObj.transform.SetParent(context.player.transform);
                weaponObj.transform.localPosition = new Vector3(0.5f, 0, 0); // 캐릭터 오른쪽에 위치
                weaponTransform = weaponObj.transform;
            }

            if (weaponTransform != null)
            {
                Debug.Log($"[NormalAttackSkill] Found/Created Weapon at {weaponTransform.position}");
                originalWeaponRotation = weaponTransform.localEulerAngles;
                originalWeaponPosition = weaponTransform.localPosition;
            }
        }
        else
        {
            Debug.LogError("[NormalAttackSkill] Player object is null!");
        }
    }

    override public void OnDeselect(GameInputManager context) 
    {
        Debug.Log($"[NormalAttackSkill] OnDeselect");
        isSelected = false;
        context.ClearArcDraw();
        
        // 무기 원위치
        if (weaponTransform != null)
        {
            weaponTransform.localEulerAngles = originalWeaponRotation;
            weaponTransform.localPosition = originalWeaponPosition;
        }

        // 스윙 중이면 코루틴 정지
        if (swingCoroutine != null)
        {
            context.StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }
        isSwinging = false;
    }

    override public void OnSkillButtonDown(GameInputManager context)
    {
        Debug.Log($"[NormalAttackSkill] OnSkillButtonDown - isSelected: {isSelected}, isSwinging: {isSwinging}");

        // 스킬이 선택되지 않았거나 이미 휘두르는 중이면 리턴
        if (!isSelected)
        {
            Debug.LogWarning("[NormalAttackSkill] Skill not selected!");
            return;
        }

        Vector3? mousePos = context.GetMouseGroundPosition();
        if (!mousePos.HasValue)
        {
            Debug.LogWarning("[NormalAttackSkill] No valid mouse position!");
            return;
        }

        // 캐릭터를 마우스 방향으로 회전
        Vector3 direction = (mousePos.Value - context.PlayerPosition).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        context.player.transform.rotation = Quaternion.Euler(0, targetAngle, 0);

        // 이전 스윙 코루틴이 있다면 정지
        if (swingCoroutine != null)
        {
            context.StopCoroutine(swingCoroutine);
            swingCoroutine = null;
            isSwinging = false;
        }

        // 무기 휘두르기 시작
        if (weaponTransform != null)
        {
            Debug.Log("[NormalAttackSkill] Starting swing animation");
            swingCoroutine = context.StartCoroutine(SwingWeapon(context));
        }
        else
        {
            Debug.LogError("[NormalAttackSkill] Weapon transform is null!");
        }
    }

    override public void OnSkillButtonUp(GameInputManager context) { }

    override public void Update(GameInputManager context) 
    {
        if (isSelected)
        {
            DrawAttackRange(context);
        }
    }

    private void DrawAttackRange(GameInputManager context)
    {
        Vector3 playerPosition = context.PlayerPosition;
        Vector3? mouseGroundPos = context.GetMouseGroundPosition();
        
        if (!mouseGroundPos.HasValue) return;

        Vector3 direction = (mouseGroundPos.Value - playerPosition).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        // 부채꼴의 시작과 끝 각도 계산
        float startAngle = targetAngle - attackAngle / 2;
        float endAngle = targetAngle + attackAngle / 2;

        // 부채꼴 그리기 요청
        context.DrawArc(playerPosition, attackRadius, startAngle, endAngle, Color.red);
    }

    private IEnumerator SwingWeapon(GameInputManager context)
    {
        isSwinging = true;

        // 무기의 초기 위치와 회전 저장
        Vector3 startRotation = weaponTransform.localEulerAngles;
        startRotation.y -= attackAngle / 2; // 시작 각도

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            float t = elapsed / swingDuration;
            
            // 부드러운 시작과 끝을 위한 보간
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // 회전 각도 계산
            Vector3 newRotation = startRotation;
            newRotation.y += attackAngle * smoothT;
            weaponTransform.localEulerAngles = newRotation;

            // 약간의 원호 움직임 추가
            float heightOffset = Mathf.Sin(smoothT * Mathf.PI) * 0.5f;
            Vector3 newPosition = originalWeaponPosition;
            newPosition.y += heightOffset;
            weaponTransform.localPosition = newPosition;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 무기 원위치
        weaponTransform.localEulerAngles = originalWeaponRotation;
        weaponTransform.localPosition = originalWeaponPosition;

        isSwinging = false;
        swingCoroutine = null;
    }

    private IEnumerator JumpToPosition(GameInputManager context, Vector3 start, Vector3 end, float duration, float height)
    {
        float time = 0;
        float dropPoint = 0.7f; // 상승 구간 비율 (0~1)
        float fallDuration = duration * (1f - dropPoint); // 하강 구간 시간
        Vector3 lastPos = start;

        while (time < duration)
        {
            float t = time / duration;
            float yOffset;

            if (t < dropPoint)
            {
                // 천천히 상승 (곡선 조정 가능)
                yOffset = Mathf.Lerp(0, height, t / dropPoint);
            }
            else
            {
                // 완만하게 하강 (선형 하강)
                float fallT = (t - dropPoint) / (1f - dropPoint); // 0~1
                yOffset = Mathf.Lerp(height, 0, fallT);
            }

            Vector3 pos = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            context.PlayerPosition = pos;
            lastPos = pos;
            time += Time.deltaTime;
            yield return null;
        }
        // 마지막 위치는 착지점
        context.PlayerPosition = end;
        context.IsSkillActive = false;
    }
}
