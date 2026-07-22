using UnityEngine;

// 패시브 오라의 지면 링 VFX. 대상(부모) 트랜스폼을 따라다니며 반경 링을 그리고 은은하게 맥동/회전한다.
// useWorldSpace=false 라 부모의 자식으로 붙이면 캐릭터를 자동으로 따라간다.
// 패시브는 "보유만으로 지속 적용"이므로, 이 링도 오라가 켜져 있는 동안 계속 표시된다.
public class AuraRing : MonoBehaviour
{
    public float radius = 5f;
    public Color color = new Color(1f, 0.55f, 0.1f, 0.8f);
    public float rotateSpeed = 30f;

    private LineRenderer lr;
    private const int Segments = 48;

    void Awake()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.material = SkillFx.UnlitColor(color);
        lr.useWorldSpace = false; // 부모(대상) 로컬 기준 → 따라다님
        lr.loop = true;
        lr.startWidth = lr.endWidth = 0.15f;
        lr.positionCount = Segments;
        Rebuild();
    }

    // 반경이 바뀌면 호출(오라 색/반경은 gamedata 기반으로 설정된다).
    public void Rebuild()
    {
        for (int i = 0; i < Segments; i++)
        {
            float a = (float)i / Segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0.1f, Mathf.Sin(a) * radius));
        }
    }

    void Update()
    {
        float pulse = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 2f));
        var c = color; c.a *= pulse;
        lr.startColor = lr.endColor = c;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
}
