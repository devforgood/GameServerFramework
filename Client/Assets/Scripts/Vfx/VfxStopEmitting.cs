using UnityEngine;

/// <summary>emitTime 이 지나면 방출만 멈춘다. 남은 파티클은 수명대로 사라진다.</summary>
public class VfxStopEmitting : MonoBehaviour
{
    public float after;
    private float elapsed;

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < after)
            return;
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        enabled = false;
    }
}
