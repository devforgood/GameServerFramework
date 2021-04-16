
using System.Collections;


public class ACData<TKEY, TCONTAINER> : ACDictionaryContainer<TKEY, TCONTAINER>, IACDataLoad where TCONTAINER : IACDataIdentified<TKEY>
{
	/// V A R I A B L E
	protected		const	string		LOAD_PATH			= "JsonData";
	protected		string				m_strDataName		= string.Empty;
	private			bool				m_bAllowReload		= false;

	/// P R O P E R T Y
	public			bool				IsDataSync			{ get; set; }

	/// I N D E X E R
	public new TCONTAINER this[ TKEY key ]
	{
		get
		{
			TCONTAINER		a_kData		= default;

			TryGetValue( key, out a_kData );

			return a_kData;
		}

		set
		{
			base[ key ]		= value;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// ACData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public ACData( EACDataType eDataType, bool bAllowReload = false )
		: base()
	{
		m_strDataName		= eDataType.ToString();
		m_bAllowReload		= bAllowReload;
		IsDataSync			= false;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// IsLoaded()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		로딩이 되어 있나
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected bool IsLoaded()
	{
		return ( m_bAllowReload == false ) && ( this.Count > 0 );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		파일에서 직접 읽어온다
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public virtual void LoadData()
	{
		if( IsLoaded() == true )
		{
			return;
		}

		var		a_arrLoadData	= JsonManager.LoadJsonArray<TCONTAINER>( LOAD_PATH, m_strDataName );

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
		//ACDebug.Assert( a_arrLoadData.Length != 0, EACError.AC_EMPTY );
#endif

		InsertData( a_arrLoadData );

		OnLoaded();
	}


	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnLoaded()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		로드 끝남
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public virtual void OnLoaded() {}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// JsonDataLoad()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void InsertData<T>( T listDtat ) where T : IEnumerable
	{
		foreach( TCONTAINER kData in listDtat )
		{
			this[ kData.IDENTIFIED ]	= kData;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Release()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public virtual void Release()
	{
		Clear();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnAdd()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnAdd( TKEY key, TCONTAINER container )
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnRemove()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void OnRemove( TKEY key )
	{
	}
}

public abstract class ACData<TCONTAINER> : ACListContainer<TCONTAINER>, IACDataLoad
{
	/// D E F I N E
	private			const	int			CAPATITY			= 10;

	/// V A R I A B L E
	protected		TCONTAINER			m_kRawData			= default;
	protected		const	string		LOAD_PATH			= "JsonData";
	protected		string				m_strDataName		= string.Empty;
	private			bool				m_bAllowReload		= false;

	/// P R O P E R T Y
	public			bool				IsDataSync			{ get; set; }

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// ACData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public ACData( EACDataType eDataType, bool bAllowReload = false )
		: base( CAPATITY )
	{
		m_strDataName		= eDataType.ToString();
		m_bAllowReload		= bAllowReload;
	}


	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		데이터 로드
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public abstract void LoadData();

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Shutdown()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected override void Shutdown()
	{
		m_kRawData		= default;
	}
}