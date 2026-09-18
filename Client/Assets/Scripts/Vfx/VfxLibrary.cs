using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이펙트 키 → 에셋 팩 프리팹 표. <see cref="Vfx"/> 가 Resources 에서 읽는다.
///
/// 팩 프리팹은 Assets/VFX 밑(Resources 밖)에 있어 이름으로 불러올 수 없다. 그래서 참조를 이 에셋에 모아
/// Resources/VfxLibrary.asset 한 개로 싣는다. 내용은 VfxLibraryTool 이 코드 표로 매번 다시 채우므로
/// 인스펙터에서 손으로 고치면 덮어써진다 — 바꿀 것은 VfxLibraryTool.Catalog 에 반영하라.
/// </summary>
public class VfxLibrary : ScriptableObject
{
    public const string ResourcePath = "VfxLibrary";

    [Serializable]
    public class Entry
    {
        public string key;
        public GameObject prefab;
        [Tooltip("프리팹 원래 크기에 곱하는 배율. 도구가 실측 크기를 목표 크기(size)에 맞춰 계산한다.")]
        public float scale = 1f;
        [Tooltip("배율 1 에서의 크기(m). Vfx.Play 의 scale 인자를 곱하면 실제 크기가 된다.")]
        public float size = 1f;
        [Tooltip("이 시간이 지나면 방출을 멈춘다(0 = 한 주기). 반복 프리팹의 긴 주기를 자르는 데 쓴다.")]
        public float emitTime;
        [Tooltip("알파가 0 보다 크면 파티클 원래 색 대신 이 색을 쓴다. 한 이펙트를 속성별로 다시 칠해 쓸 때.")]
        public Color recolor = Color.clear;
    }

    public List<Entry> entries = new List<Entry>();
}
