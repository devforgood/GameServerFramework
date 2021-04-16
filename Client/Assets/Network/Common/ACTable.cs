
using System.Collections.Generic;

public class ACTable
{
	protected static class PRIMARY_KEY
	{
		/// C O N S T
		private		const	int		UNIT_SOUND_INDEX	= 1000;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// MakeKey()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int MakeKey<T>( T kValue ) where T : IACPrimaryKey<int, int>
		{
			return MakeKey( kValue.VALUE_1, kValue.VALUE_2 );
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// MakeKey()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public static int MakeKey( int nValue1, int nVaule2 )
		{
			return ( nValue1 * UNIT_SOUND_INDEX ) + nVaule2;
		}
	}

	public class POWER_LEVEL : ACDataSync<int, JPowerLevel_TableData>
	{
		/// V A R I A B L E
		private		Dictionary<int, int>	m_dicKeyMapping		= null;
		private		Dictionary<int, int>	m_dicLevelMax		= null;

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// POWER_LEVEL()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public POWER_LEVEL( EACDataType eDataType, bool bAllowReload = false ) 
			: base( eDataType, bAllowReload )
		{
			m_dicKeyMapping		= new Dictionary<int, int>();
			m_dicLevelMax		= new Dictionary<int, int>();
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// OnLoaded()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//		로드된 데이터 키를 맵핑한다.
		//		Key : Rare_Id, PowerLevel
		//		Value : ID (Key와 동일)
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public override void OnLoaded()
		{
			JPowerLevel_TableData		a_kData		= null;
			int							a_nKey		= 0;

			foreach( KeyValuePair<int, JPowerLevel_TableData> pair in this )
			{
				a_kData		= pair.Value;
				a_nKey		= PRIMARY_KEY.MakeKey<JPowerLevel_TableData>( a_kData );

				m_dicKeyMapping.Add( a_nKey, a_kData.IDENTIFIED );

				SearchMaxLevel( a_kData );
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// SearchMaxLevel()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//
		////////////////////////////////////////////////////////////////////////////////////////////////////
		private void SearchMaxLevel( JPowerLevel_TableData kData )
		{
			int		a_nRareID		= kData.Rare_Id;
			int		a_nPowerLevel	= kData.PowerLevel;

			if( m_dicLevelMax.ContainsKey( a_nRareID ) == true )
			{
				if( m_dicLevelMax[ a_nRareID ] >= a_nPowerLevel )
				{
					return;
				}

				m_dicLevelMax[ a_nRareID ]		= a_nPowerLevel;
			}
			else
			{
				m_dicLevelMax.Add( a_nRareID, a_nPowerLevel );
			}
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// GetData()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//		JPowerLevel_TableData 를 가져온다
		//
		//	Param
		//		nRareID : 캐릭터 레어도 (현재 CharacterInfo RareLabel 사용)
		//		nPowerLevel :현재 파워 레벨
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public JPowerLevel_TableData GetData( int nRareID, int nPowerLevel )
		{
			int		a_nMaxValue		= GetMaxPowerLevel( nRareID );

			if( a_nMaxValue < nPowerLevel )
			{
				nPowerLevel		= a_nMaxValue;
			}

			int		a_nKey		= PRIMARY_KEY.MakeKey( nRareID, nPowerLevel );

			return this[ m_dicKeyMapping[ a_nKey ] ];
		}

		////////////////////////////////////////////////////////////////////////////////////////////////////
		// GetMaxPowerLevel()
		//--------------------------------------------------------------------------------------------------
		//	Desc.
		//		파워 레벨의 최대값을 가져옴
		////////////////////////////////////////////////////////////////////////////////////////////////////
		public int GetMaxPowerLevel( int nRareID )
		{
			return m_dicLevelMax[ nRareID ];
		}
	}
}