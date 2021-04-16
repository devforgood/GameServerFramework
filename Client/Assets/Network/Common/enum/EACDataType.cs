
public enum EACDataType
{
	NONE	= 0,

	Scene,
	Username,
	Sound,
	Map,
	GameMode,
	Grade,
	Rank,
	MenuStyle,
	PlayPoint,
	CharacterInfo,
	MapinfoTable,
	Character,
	Skill,
	Projectile,
	Explosion,
	MotionEffect,
	Item,
	Spell,
	Effect,
	SkillGuide,
	MapObjectProperty,
	CommonPopup,
	MetaTable,
	GameItem,
	Gacha_Box_Base,
	Gacha_Table,
	Mission_Base,
	Mission_info,
	PowerLevel_Table,
	ShopList,
	ShopItemList,
	CharacterSelect,
	InGameUI,
	AdList,
	RankingReward,

	/// 개별로 읽는 데이터 (엑셀에 없음)
	MapCommonData,

	ALL,
}

public enum EACDataLoadType
{
	NONE	= 0,

	PRIMITIVE,
	ESSENTIAL,
	BATTLE
}

public enum EACDataUnloadType
{
	NONE = 0,

	PRIMITIVE		= EACDataLoadType.PRIMITIVE,
	ESSENTIAL		= EACDataLoadType.ESSENTIAL,
	BATTLE			= EACDataLoadType.BATTLE
}

public enum ITEM_ID_TYPE
{
	NONE	= 0,

	ADD_ITEM,				//탄창 충전 속도.
	SHIELD_ITEM,			//방어막.
	HEALTH_ITEM,			//HP회복.
	CASTLE_SHIELD_ITEM,		//거점방어막.
	CASTLE_HEALTH_ITEM,		//거점회복.
	SPEED_ITEM,				//이동속도증가.
	SKILLCOOLTIME_ITEM,		//스킬쿨타임감소.
	SHIELD_ALL_ITEM,		//아군 팀원 전체 방어막.
	POWERUP_ALL_ITEM,       //아군 팀원 전체 파워업.
    HEALTH_ALL_ITEM,        //아국 팀원 전체 HP 회복.
    POWERUP_ITEM,			//파워업.
    MAX,
}

public enum ItemType
{
	Normal,
	Tactic,
}

public enum PlayPointID
{
	None,
	MapObjectDestroy,
	ItemGain,
	EnemyAttack,
	EnemyKill,
	CastleAttack,
	KillTheKing,

	// 이하 값 들은 테이블에 없음. 트리거 처리 용.
	PlayerDeath,
	PlayerReborn
}

public enum MissionType
{
	None,
	Mission_KillCount,
	Mission_Victory,
	Mission_Damage,
	Mission_GetMedal,
	Mission_OpenBox_N,
	Mission_Get_NorItem,
	Mission_Get_TacItem,
}
public enum MissionBase
{
	None,
	Daily,
	Weekly,
}

public enum eEFFECTID
{
	WaterSplash
}

public enum GameItemType
{
	None,
	Goods,
	CharacterPiece,
	Character,
	Gacha,
    Medal_Charging,
    Equipment,
    Special_Equipment,
	CharacterPieceReward,
}

public enum GameItemId
{
    // None 부터 UpgrageStone 까지 재화, eMoneyType 과 통일.
    None,
    Coin,
    Gem,
    BattleCoin,
    Medal,
    UpgradeStone,
	Cash,

    NormalGachaBox = 31,
	AdShopGachaBox = 34,
	OpenEventBox = 36,
	AdResultGachaBox = 39,
	CharacterPieceReward = 75,
}

public enum ShopItemType
{
	None,
	Normal,
	Goods,
}

public enum MedalChargeConst
{
	None,
	ChargePeriod = 5,
	MaxCharge = 200,
}

public enum ShopResetType
{
	None,
	Daily,
	Monthly,
	Fixed,
}
