#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

//using PACKET = ACScrambleNetwork.NET_PACKET;

public class MapCommonData : ACData<JMapCommonData>
{
	/// D E F I N E
	private		const		string		FORMAT_COMMON_DATA		= "{0}_CommonData";
	private		const		int			CURRENT_DATA			= 0;

	/// P R O P E R T Y
	public		int[]		ItemIdentifieds						=> m_kRawData.mapItemID;
	public		int[]		SkillIdentifieds					=> m_kRawData.mapSkillID;
	public		int[]		EffectIdentifieds					=> m_kRawData.mapEffectID;

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// MapCommonData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public MapCommonData( EACDataType eDataType ) 
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
		//m_strDataName							= string.Format( FORMAT_COMMON_DATA, ACScrambleBattle.GAME_MODE.MapDataPath );
		JMapCommonData[]	a_arrLoadData		= JsonManager.LoadJsonArray<JMapCommonData>( LOAD_PATH, m_strDataName );

		foreach( JMapCommonData kCommonData in a_arrLoadData )
		{
			Add( kCommonData );
		}

		m_kRawData		= this[ CURRENT_DATA ];
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnAdd()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnAdd( JMapCommonData item )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnRemove()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnRemove( JMapCommonData item )
	{
	}
}

#endif