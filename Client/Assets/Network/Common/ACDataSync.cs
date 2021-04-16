

public class ACDataSync<TKEY, TCONTAINER> : ACData<TKEY, TCONTAINER> where TCONTAINER : IACDataIdentified<TKEY>
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// ACDataSync()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public ACDataSync( EACDataType eDataType, bool bAllowReload = false ) 
		: base( eDataType, bAllowReload )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		서버 데이터로 동기화함.
	//		IsDataSync : 동기화 유무
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public override void LoadData()
	{

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
		//LobbyController		a_kLobbyController		= LobbyController.Instance; ACDebug.Assert( a_kLobbyController != null, EACError.AC_INSTANCE_NULL );
		//LoginReply			a_kLoginReply			= a_kLobbyController.LoginInfo;
		
		//if( IsDataSync == true )
		//{
		//	base.LoadData( a_kLoginReply.JsonData );
		//}
		//else
		//{
			base.LoadData();
		//}
#else
		base.LoadData();
#endif

	}
}

public class ACDataSync<TCONTAINER> : ACData<TCONTAINER>
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// ACDataSync()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public ACDataSync( EACDataType eDataType, bool bAllowReload = false ) 
		: base( eDataType, bAllowReload )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		서버 데이터로 동기화함.
	//		IsDataSync : 동기화 유무
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public override void LoadData()
	{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
		//LobbyController		a_kLobbyController		= LobbyController.Instance; ACDebug.Assert( a_kLobbyController != null, EACError.AC_INSTANCE_NULL );
		//LoginReply			a_kLoginReply			= a_kLobbyController.LoginInfo;


		//if( IsDataSync == true )
		//{
		//	base.LoadData( a_kLoginReply.JsonData );
		//}
		//else
		//{
		//	/// 서버에서 받을 수가 없음
		//	/// 너가 받게 만드세요
		//	ACDebug.Assert( false, EACError.AC_LOAD_FAILED );
		//}
#endif
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnAdd()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnAdd( TCONTAINER item )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnRemove()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnRemove( TCONTAINER item )
	{
	}
}
