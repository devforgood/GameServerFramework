
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

public partial class JRankData : IACDataIdentified<int>
{
    [System.NonSerialized]
    public		int		AccumulateCBS	= 0; // 누적 케릭터 배틀 스코어

}
