
public class RankData : ACDataSync<int, JRankData>
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// RankData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public RankData( EACDataType eDataType ) 
		: base( eDataType )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public override void LoadData()
    {
		var list = JsonManager.LoadJsonArray<JRankData>( LOAD_PATH, m_strDataName );
		int AccumulateCharacterBattleScore = 0;
        foreach (var data in list)
        {
            AccumulateCharacterBattleScore += data.NeedCBS;
            data.AccumulateCBS = AccumulateCharacterBattleScore;
            this[data.ID] = data;
            core.LogHelper.LogInfo($"Rank {data.ID}, {data.NeedCBS}, {data.AccumulateCBS}");
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // GetRank()
    //--------------------------------------------------------------------------------------------------
    //	Desc.
    //		배틀 스코어로 랭크 데이터 얻기
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public JRankData GetRank(int battleScore)
    {
        JRankData rank = null;
        foreach (var data in this.Values)
        {
            if (data.AccumulateCBS > battleScore)
                break;

            rank = data;
        }
        return rank;
    }
}
