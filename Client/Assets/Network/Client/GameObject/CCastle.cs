using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CCastle : core.Castle
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CCastle(); }
    public static bool isPlayDestroyEffect; //기지 파괴 연출 사용중 여부.
    public GameObject prop;
    //public MapCastleProperty mapCastleProperty;
    public static Vector3[] resultCamPos = new Vector3[] { new Vector3(0f, 19.5f, -30.0f), new Vector3(0f, 19.5f, 14.5f), new Vector3(0f, 19.5f, -14.5f), new Vector3(0, 19.5f, 30.0f) };//내가 A팀일때 기지A,기지B, 내가 B팀일때 기지A,기지B 위치. 

    protected CCastle()
    {

    }

    public void OnCreate()
    {
        isPlayDestroyEffect = false;
        Debug.Log($"create castle {GetNetworkId()}, {mapObjectId}");
        //MapManager.SetActiveMapObj((int)mapObjectId, true);

        //prop = MapManager.GetMapObject((int)mapObjectId);
        //if (prop == null)
        //{
        //    return;
        //}
        //mapCastleProperty = prop.GetComponent<MapCastleProperty>();
        //if (mapCastleProperty)
        //    mapCastleProperty.ChangeHP(hp);
        //var behaviour = prop.AddComponent<PropBehaviour>();
        //behaviour.NetworkId = GetNetworkId();
        //var itemData = ACDataStorage.ITEM[ (int)ITEM_ID_TYPE.CASTLE_SHIELD_ITEM ];
        //if (itemData != null)
        //{
        //    var spellData = ACDataStorage.SPELL[ itemData.SpellID[0] ];
        //    shield_hp_max = (ushort)spellData.AddStatus;
        //}


        JMapObjectData mapData;
        if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
        {
            Debug.Log($"cannot find map object {mapObjectId}");
            return;
        }
        else
        {
            this.Set(mapData);

#if _USE_BEPU_PHYSICS
            collision = core.World.Instance(WorldId).CreateCollision(mapData);
#endif
        }

        core.World.Instance().castleList[Team] = this;
    }

    public override void HandleDying()
    {
        base.HandleDying();
    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        if (IsCreate == true)
        {
            OnCreate();
        }

        if ((readState & (UInt32)ReplicationState.Buff) != 0)
        {
            OnChangeBuff();
        }

        if ((readState & (UInt32)ReplicationState.Health) != 0)
        {
            OnChangeHealth();
        }
    }

    void OnChangeBuff()
    {
        Debug.Log(Team + "Castle OnChangeBuff : " + shield_hp);
        //if (shield_hp == shield_hp_max)
        //{
        //    float shieldActiveTime = shield_time - core.Timing.sInstance.GetFrameStartTime();
        //    if (shieldActiveTime > 0)
        //    {
        //        mapCastleProperty.shieldEffect?.SetShieldHpUI(shield_hp);
        //        mapCastleProperty.shieldEffect?.Show(shieldActiveTime);

        //        mapCastleProperty.uiHP.ActiveShieldHp(true);
        //    }
        //}
        //else if (shield_hp > 0)
        //{
        //    float? fillAmount = mapCastleProperty.shieldEffect?.SetShieldHpUI(shield_hp);
        //    if (fillAmount.HasValue)
        //        mapCastleProperty.uiHP.SetShieldHp(fillAmount.Value);
        //}
        //else if (shield_hp == 0)
        //{
        //    mapCastleProperty.shieldEffect?.Hide();
        //    float? fillAmount = mapCastleProperty.shieldEffect?.SetShieldHpUI(shield_hp);

        //    mapCastleProperty.uiHP.ActiveShieldHp(false);
        //}
    }

    void OnChangeHealth()
    {
        Debug.Log(Team + "Castle OnChangeHealth : " + hp);

        //if (IsHealthUp)
        //{
        //    SoundManager.Instance.Play((int)EAInGameSoundID.BASE_HEAL, mapCastleProperty.transform.position);
        //    mapCastleProperty.ShowHealEffect();
        //    if(MyTeamCheck())
        //        CastleWarning();
        //}
        //else
        //{
        //    if (hp_max == hp)
        //        return;

        //    if (MyTeamCheck())
        //    {
        //        SoundManager.Instance.Play(ACDataStorage.MAP_OBJECT_PROPERTY.GetData((int)MapPropertyType.Castle, 1).SoundId1, mapCastleProperty.transform.position);
        //        CastleWarning();
        //    }

        //    if (prop)
        //        prop.GetComponentInChildren<Animator>()?.SetTrigger("Hit");
        //    //기지가 파괴된 경우 
        //    if (hp == 0 && prop)
        //    {
        //        if (mapCastleProperty)
        //        {
        //            mapCastleProperty.ChangeHP(0);
        //            mapCastleProperty.shieldEffect?.Hide();

        //            mapCastleProperty.uiHP.shieldSlider.gameObject.SetActive(false);
        //        }
        //        //맵 오브젝트 파괴 연출.
        //        isPlayDestroyEffect = true;
        //        Camera.main.GetComponent<FollowTarget>().isActive = false;
        //        core.Actor actor = null;
        //        core.World.Instance().playerList.TryGetValue(NetworkManagerClient.sInstance.GetPlayerId(), out actor);
        //        if (actor == null)
        //            return;
        //        int posValue = 0;
        //        if (actor.Team == core.Team.TeamA)
        //            posValue = (core.Team)mapCastleProperty.team == core.Team.TeamA ? 0 : 1;
        //        else
        //            posValue = (core.Team)mapCastleProperty.team == core.Team.TeamA ? 2 : 3;
        //        //기지로 카메라이동 및 파괴 연출.
        //        MoveCastleCam(resultCamPos[posValue]);
        //    }
        //}
        //if (mapCastleProperty)
        //    mapCastleProperty.ChangeHP(hp);
    }

    public void MoveCastleCam(Vector3 pos, bool isDestroy = true)
    {
//        Camera.main.transform.DOMove(pos, 2.0f).OnComplete(() =>
//        {
//            if(isDestroy)
//            { 
//                mapCastleProperty.DestroyEffect();
//#if _USE_BEPU_PHYSICS
//                    if (collision != null)
//                        core.World.Instance(WorldId).space.Remove(collision);
//#endif
//            }
//            if (isDestroy)
//				SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.Castle, 1 ).SoundId2 );
//			Debug.Log($"remove castle {GetNetworkId()}, {mapObjectId}");
//        });
    }

    private void CastleWarning()
    {                                       

   //     if(hp < (hp_max * 0.3))
   //     {
			//if( MapCastleProperty._bCastlHpWarning)
   //         {
   //             return;
   //         }
   //         SoundManager.Instance.Play((int)EAInGameSoundID.BASE_EMERGENCY);
   //         MapCastleProperty._bCastlHpWarning = true;
   //     }
   //     else
   //     {
   //         MapCastleProperty._bCastlHpWarning = false;
   //     }
    }
    private bool MyTeamCheck()
    {
        core.Actor actor = null;
        core.World.Instance().playerList.TryGetValue(NetworkManagerClient.sInstance.GetPlayerId(), out actor);
        if (actor != null)
        {
            if (actor.Team == (core.Team)Team)
            {
                return true;

            }                    
        }
        return false;
    }

    [core.ClientRPC]
    public override void NoticeHealth(int health)
    {
        //if (mapCastleProperty)
        //    mapCastleProperty.uiHP.SetDamageText(health);
        //Debug.Log($"Castle.NoticeHealth {GetNetworkId()}, {health}");
    }
}
