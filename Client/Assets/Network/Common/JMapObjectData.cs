using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#else
using core;
#endif

[System.Serializable]
public class JMapObjectData
{
    public ushort uID;
    public int mapTypes;
    public int colliderTypes; //0: BoxCollider, 1: MeshCollider
    public Vector3 mapPos;
    public Quaternion mapRot;
    public float mapRotY;
    public Vector2 mapScale;
    public Vector3 jumpLandingPos;  //점프 속성인 경우 착지 위치.
    //public float jumpFirstStartDelayTime;   //점프 속성인 경우 처음 시작 딜레이 타임. JMapSwitchData.firstStartDelayTime로 변경.
    public float jumpPower; //점프 속성인 경우 점프 파워.
    public float jumpDuration;  //점프 속성인 경우 점프 지속 시간.
    //public float jumpCoolTime;  //점프 속성인 경우 쿨타임.  JMapSwitchData.coolTime로 변경.
    public float tileSpeed; //이동 속도 변경 속성인 경우 속도값.
    public ushort objectHP; //오브젝트 HP.
    public string prefabsName;
    public Vector3[] meshColliderVertices;
    public int[] meshColliderIndices;
    public JMapItemData[] jMapItemData;
    public JAutoItemData[] jAutoItemData;
    public JCastleData[] jCastleData;
    public JSpawnData[] jSpawnData;
    public JAutoBombData[] jAutoBombData;
    public JDestroyBombData[] jDestroyBombData; //오브젝트가 파괴될 경우 생성되는 폭탄 스킬.
    public JMapMovePathData[] jMapMovePathData;
    public JMapSwitchData[] jMapSwitchData;
    public JMapSpecialObjectData[] jMapSpecialObjectData;

    public JMapObjectData(ushort uid, int maptypes, int collidertypes, Vector3 mappos, Quaternion maprot, float maproty, Vector2 mapscale, Vector3 jumplandingpos, float jumppower, float jumpduration, float tilespeed,
        ushort objecthp, string prefabsname, Vector3[] meshcollidervertices = null, int[] meshcolliderindices = null, JMapItemData[] jmapitemdata = null, JAutoItemData[] jautoitemdata = null, JCastleData[] jcastledata = null,
        JSpawnData[] jspawndata = null, JAutoBombData[] jautobombdata = null, JDestroyBombData[] jdestroybombdata = null, JMapMovePathData[] jmapmovepathdata = null, JMapSwitchData[] jmapswitchdata = null, JMapSpecialObjectData[] jmapspecialobjectdata = null)
    {
        uID = uid;
        mapTypes = maptypes;
        colliderTypes = collidertypes;
        mapPos = new Vector3(Mathf.Round(mappos.x), Mathf.Round(mappos.y), Mathf.Round(mappos.z));
        mapRot = maprot;
        mapRotY = maproty;
        mapScale = mapscale;
        jumpLandingPos = jumplandingpos;
        jumpPower = jumppower;
        jumpDuration = jumpduration;
        tileSpeed = tilespeed;
        objectHP = objecthp;
        prefabsName = prefabsname;
        meshColliderVertices = meshcollidervertices;
        meshColliderIndices = meshcolliderindices;
        jMapItemData = jmapitemdata;
        jAutoItemData = jautoitemdata;
        jCastleData = jcastledata;
        jSpawnData = jspawndata;
        jAutoBombData = jautobombdata;
        jDestroyBombData = jdestroybombdata;
        jMapMovePathData = jmapmovepathdata;
        jMapSwitchData = jmapswitchdata;
        jMapSpecialObjectData = jmapspecialobjectdata;
    }
}

[System.Serializable]
public class JMapItemData
{
    public ushort createItemPer; //아이템이 나타날 확률.
    public int[] createItems;

    public JMapItemData(ushort createitemper, int[] createitems)
    {
        createItemPer = createitemper;
        createItems = createitems;
    }
}

[System.Serializable]
public class JAutoItemData
{
    public bool isSpecial;  //특별한 아이템 생성 상자.
    public bool isRandom;   //아이템 생성 Random or Rolling
    public int[] createItems;
    public float openStartTime;   //처음 생성되는 시간.
    public float createCoolTime;    //아이템 생성 쿨타임.

    public JAutoItemData(bool isspecial, bool israndom, int[] createitems, float openstarttime, float createcooltime)
    {
        isSpecial = isspecial;
        isRandom = israndom;
        createItems = createitems;
        openStartTime = openstarttime;
        createCoolTime = createcooltime;
    }
}
[System.Serializable]
public class JCastleData
{
    public int castleHP;
    public ushort castleTeam;

    public JCastleData(int castlehp, ushort castleteam)
    {
        castleHP = castlehp;
        castleTeam = castleteam;
    }
}

[System.Serializable]
public class JSpawnData
{
    public bool isRespawn;
    public int spawnNum;
    public ushort spawnTeam;
    public Vector3 mapPos;
    public Vector3 startVector;

    public JSpawnData(bool isRespawn, int spawnNum, ushort spawnTeam, Vector3 startvector)
    {
        this.isRespawn = isRespawn;
        this.spawnNum = spawnNum;
        this.spawnTeam = spawnTeam;
        this.startVector = startvector;
    }
}

[System.Serializable]
public class JAutoBombData
{
    public float firstStartDelayTime;
    public float createTime;
    public float explodeTime;
    public int skillID;

    public JAutoBombData(float firststartdelaytime, float createTime, float explodeTime, int skillid)
    {
        this.firstStartDelayTime = firststartdelaytime;
        this.createTime = createTime;
        this.explodeTime = explodeTime;
        this.skillID = skillid;
    }
}

[System.Serializable]
public class JDestroyBombData
{
    public int skillID;

    public JDestroyBombData(int skillid)
    {
        this.skillID = skillid;
    }
}

[System.Serializable]
public class JMapMovePathData
{
    public int moveObjectUID;   //이동되는 오브젝트 Map UID.
    public float firstStartDelayTime;   //처음 시작 딜레이 타임.
    public float createTime;    //처음 시작 이후 주기적으로 생성되는 시간.
    public float moveSpeed;
    public int ObjectDamage;
    public Vector3 moveStartPos;
    public Vector3 moveEndPos;

    public JMapMovePathData(int moveobjectuid, float firststartdelaytime, float createtime, float movespeed, int objectdamage, Vector3 movestartpos, Vector3 moveendpos)
    {
        this.moveObjectUID = moveobjectuid;
        this.firstStartDelayTime = firststartdelaytime;
        this.createTime = createtime;
        this.moveSpeed = movespeed;
        this.ObjectDamage = objectdamage;
        this.moveStartPos = movestartpos;
        this.moveEndPos = moveendpos;
    }
}

[System.Serializable]
public class JMapSwitchData
{
    public bool isAlwaysUse;    //데미지를 받을때마다 항상 동작, hp사용 안함.
    public int[] specialObjectUID;    //스페셜 오브젝트 Map UID.
    public float firstStartDelayTime;   //처음 시작 딜레이 타임.
    public float coolTime;  //다음 사용까지 걸리는 쿨타임.

    public JMapSwitchData(bool isalwaysuse, int[] specialobjectuid, float firststartdelaytime, float cooltime)
    {
        this.isAlwaysUse = isalwaysuse;
        this.specialObjectUID = specialobjectuid;
        this.firstStartDelayTime = firststartdelaytime;
        this.coolTime = cooltime;
    }
}

[System.Serializable]
public class JMapSpecialObjectData
{
    public ushort specialObjectType;    //스페셜 오브젝트 타입:Create = 0,Delete = 1,Transform = 2
    public int transformObjUID; //트랜스폼 타입인 경우 사용할 연동 오브젝트.

    public JMapSpecialObjectData(ushort specialobjecttype, int transformobjuid)
    {
        this.specialObjectType = specialobjecttype;
        this.transformObjUID = transformobjuid;
    }
}

[System.Serializable]
public class JMapCommonData
{
    public int[] mapSkillID;
    public int[] mapItemID;
    public int[] mapEffectID;

    public JMapCommonData(int[] mapskillid, int[] mapitemid, int[] mapeffectid)
    {
        this.mapSkillID = mapskillid;
        this.mapItemID = mapitemid;
        this.mapEffectID = mapeffectid;
    }
}
