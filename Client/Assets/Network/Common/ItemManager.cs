
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;

public class ItemManager : MonoBehaviour
{
	/// D E F I N E
	private		const	int		ITEM_POOLING_COUNT		= 5;

    [HideInInspector]
    public static List<GameObject> arrItemEffect = new List<GameObject>();

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Initialize()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public void Initialize()
	{
		LoadItems();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// LoadItems()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	private void LoadItems()
	{
		//SkillManager	a_kSkillManager		= SkillManager.GetInstance(); ACDebug.Assert( a_kSkillManager != null, EACError.AC_INSTANCE_NULL );
		//ACItemEntity	a_kItemEntity		= null;

		//foreach( int nItemID in ACDataStorage.MAP_COMMON.ItemIdentifieds )
		//{
		//	a_kItemEntity		= ACComponentFactory.CreateComponentEntity<ACItemEntity>( nItemID );

		//	a_kItemEntity.CreateComponents();
		//	a_kItemEntity.LoadPrefab();
		//	a_kItemEntity.MakeKey();

		//	PoolManager.CreateObjectPool( a_kItemEntity.Prefab, a_kItemEntity.Key, ITEM_POOLING_COUNT );

		//	foreach( ACSpellEntity kSpellEntity in a_kItemEntity.Spells.Values )
		//	{
		//		a_kSkillManager.CreateSpellPool( kSpellEntity );
		//	}
		//}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// GetItemObject()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public static GameObject GetItemObject( int itemID, bool colliderActive = true )
	{
		//ACItemEntity	a_kItemEntity		= ACComponentProvider.GetEntity<ACItemEntity>( itemID );
		//GameObject		a_kItemObject		= PoolManager.Spawn( a_kItemEntity.Prefab, a_kItemEntity.Key, Vector3.zero, Quaternion.identity );
		//BoxCollider		a_kBoexCollider		= a_kItemObject.GetComponentInChildren<BoxCollider>();
		//a_kBoexCollider.enabled				= colliderActive;

		//return a_kItemObject;

		return null;
    }

    public static GameObject GetItemEffect(Vector3 pos)
    {
        GameObject effectGo = null;
        for(int i = 0; i < arrItemEffect.Count; ++i)
        {
            if(arrItemEffect[i].activeSelf == false)
            {
                effectGo = arrItemEffect[i];
                effectGo.transform.position = pos;
                effectGo.transform.rotation = Quaternion.identity;
                return effectGo;
            }
        }
        if (arrItemEffect.Count > 0)
        {
            effectGo = GameObject.Instantiate(arrItemEffect[0], Vector3.zero, Quaternion.identity, arrItemEffect[0].transform.parent);
            effectGo.SetActive(false);
            arrItemEffect.Add(effectGo);
        }
        return effectGo;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // OnDestroy()
    //--------------------------------------------------------------------------------------------------
    //	Desc.
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnDestroy()
    {
        arrItemEffect.Clear();
    }

}
#endif
