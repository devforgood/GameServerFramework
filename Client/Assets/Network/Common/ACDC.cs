
using MetaTableData				= ACDataSync<int,		JMetaTableData>;
using SceneData					= ACDataSync<string,	JSceneData>;
using UsernameData				= ACDataSync<int,		JUsernameData>;
using SoundData					= ACDataSync<int,		JSoundData>;
using MapData					= ACDataSync<int,		JMapData>;
using GameModeData				= ACDataSync<int,		JGameModeData>;
using MenuStyleData				= ACDataSync<int,		JMenuStyleData>;
using PlayPointData				= ACDataSync<int,		JPlayPointData>;
using CharacterInfoData			= ACDataSync<int,		JCharacterInfoData>;
using MapInfoData				= ACDataSync<int,       JMapinfoTableData>;
using CharacterData				= ACDataSync<int,		JCharacterData>;
using SkillData					= ACDataSync<int,		JSkillData>;
using ProjectileData			= ACDataSync<int,		JProjectileData>;
using ExplosionData				= ACDataSync<int,		JExplosionData>;
using MotionEffectData			= ACDataSync<int,		JMotionEffectData>;
using ItemData					= ACDataSync<int,		JItemData>;
using SpellData					= ACDataSync<int,		JSpellData>;
using EffectData				= ACDataSync<int,		JEffectData>;
using SkillGuideData			= ACDataSync<int,		JSkillGuideData>;
using CommonPopupData			= ACDataSync<string,	JCommonPopupData>;
using GameItemData				= ACDataSync<int,		JGameItemData>;
using Gacha_Box_BaseData		= ACDataSync<int,		JGacha_Box_BaseData>;
using Gacha_TableData			= ACDataSync<int,		JGacha_TableData>;
using Mission_BaseData			= ACDataSync<int,		JMission_BaseData>;
using Mission_infoData			= ACDataSync<int,		JMission_infoData>;
using PowerLevel_TableData		= ACTable.POWER_LEVEL;
using ShopListData				= ACDataSync<int,		JShopListData>;
using ShopItemListData			= ACDataSync<int,		JShopItemListData>;
using CharacterSelectData		= ACDataSync<int,		JCharacterSelectData>;
using InGameUIData				= ACDataSync<int,		JInGameUIData>;
using AdListData				= ACDataSync<int,		JAdListData>;
using RankingRewardData			= ACDataSync<int,		JRankingRewardData>;
public class ACDC
{
	/// DC
	public		static		ACDC						Data									{ get; } = new ACDC();

	/// META TABLE DATA
	public		static		MetaTableData				MetaTableData							{ get; } = new MetaTableData( EACDataType.MetaTable );
	public		static		implicit	operator		MetaTableData(ACDC kDC)					{ return MetaTableData; }

	/// SCENE DATA
	public		static		SceneData					SceneData								{ get; } = new SceneData( EACDataType.Scene );
	public		static		implicit	operator		SceneData( ACDC kDC )					{ return SceneData; }

	/// USER NAME DATA
	public		static		UsernameData				UsernameData							{ get; } = new UsernameData( EACDataType.Username );
	public		static		implicit	operator		UsernameData( ACDC kDC )				{ return UsernameData; }

	/// SOUND DATA
	public		static		SoundData					SoundData								{ get; } = new SoundData( EACDataType.Sound );
	public		static		implicit	operator		SoundData( ACDC kDC )					{ return SoundData; }

	/// MAP DATA
	public		static		MapData						MapData									{ get; } = new MapData( EACDataType.Map );
	public		static		implicit	operator		MapData( ACDC kDC )						{ return MapData; }

	/// GAME MODE DATA
	public		static		GameModeData				GameModeData							{ get; } = new GameModeData( EACDataType.GameMode );
	public		static		implicit	operator		GameModeData( ACDC kDC )				{ return GameModeData; }

	/// GRADE DATA
	public		static		GradeData					GradeData								{ get; } = new GradeData( EACDataType.Grade );
	public		static		implicit	operator		GradeData( ACDC kDC )					{ return GradeData; }

	/// RANK DATA
	public		static		RankData					RankData								{ get; } = new RankData( EACDataType.Rank );
	public		static		implicit	operator		RankData( ACDC kDC )					{ return RankData; }

	/// MENU STYLE DATA
	public		static		MenuStyleData				MenuStyleData							{ get; } = new MenuStyleData( EACDataType.MenuStyle );
	public		static		implicit	operator		MenuStyleData( ACDC kDC )				{ return MenuStyleData; }

	/// PLAY POINT DATA
	public		static		PlayPointData				PlayPointData							{ get; } = new PlayPointData( EACDataType.PlayPoint );
	public		static		implicit	operator		PlayPointData( ACDC kDC )				{ return PlayPointData; }

	/// CHARACTER INFO DATA
	public		static		CharacterInfoData			CharacterInfoData						{ get; } = new CharacterInfoData( EACDataType.CharacterInfo );
	public		static		implicit	operator		CharacterInfoData( ACDC kDC )			{ return CharacterInfoData; }

	/// MAP INFO DATA
	public		static		MapInfoData					MapInfoData								{ get; } = new MapInfoData( EACDataType.MapinfoTable);
	public		static		implicit	operator		MapInfoData( ACDC kDC )					{ return MapInfoData; }

	/// CHARACTER DATA
	public		static		CharacterData				CharacterData							{ get; } = new CharacterData( EACDataType.Character );
	public		static		implicit	operator		CharacterData( ACDC kDC )				{ return CharacterData; }

	/// SKILL DATA
	public		static		SkillData					SkillData								{ get; } = new SkillData( EACDataType.Skill );
	public		static		implicit	operator		SkillData( ACDC kDC )					{ return SkillData; }

	/// PROJECTILE DATA
	public		static		ProjectileData				ProjectileData							{ get; } = new ProjectileData( EACDataType.Projectile );
	public		static		implicit	operator		ProjectileData( ACDC kDC )				{ return ProjectileData; }

	/// EXPLOSION DATA
	public		static		ExplosionData				ExplosionData							{ get; } = new ExplosionData( EACDataType.Explosion );
	public		static		implicit	operator		ExplosionData( ACDC kDC )				{ return ExplosionData; }

	/// MOTION EFFECT DATA
	public		static		MotionEffectData			MotionEffectData						{ get; } = new MotionEffectData( EACDataType.MotionEffect );
	public		static		implicit	operator		MotionEffectData( ACDC kDC )			{ return MotionEffectData; }

	/// ITEM DATA
	public		static		ItemData					ItemData								{ get; } = new ItemData( EACDataType.Item );
	public		static		implicit	operator		ItemData( ACDC kDC )					{ return ItemData; }

	/// SPELL DATA
	public		static		SpellData					SpellData								{ get; } = new SpellData( EACDataType.Spell );
	public		static		implicit	operator		SpellData( ACDC kDC )					{ return SpellData; }

	/// EFFECT DATA
	public		static		EffectData					EffectData								{ get; } = new EffectData( EACDataType.Effect );
	public		static		implicit	operator		EffectData( ACDC kDC )					{ return EffectData; }

	/// MAP OBJECT PROPERTY DATA
	public		static		MapObjectPropertyData		MapObjectPropertyData					{ get; } = new MapObjectPropertyData( EACDataType.MapObjectProperty );
	public		static		implicit	operator		MapObjectPropertyData( ACDC kDC )		{ return MapObjectPropertyData; }

    /// SKILL GUIDE DATA
	public		static		SkillGuideData				SkillGuideData							{ get; } = new SkillGuideData( EACDataType.SkillGuide );
	public		static		implicit	operator		SkillGuideData( ACDC kDC )				{ return SkillGuideData; }

	/// COMMON POPUP MESSAGE DATA
	public		static		CommonPopupData				CommonPopupData							{ get; } = new CommonPopupData( EACDataType.CommonPopup );
	public		static		implicit	operator		CommonPopupData( ACDC kDC )				{ return CommonPopupData; }

	/// GAME ITEM DATA
	public		static		GameItemData				GameItemData							{ get; } = new GameItemData( EACDataType.GameItem );
	public		static		implicit	operator		GameItemData( ACDC kDC )				{ return GameItemData; }

	/// GACHA BOX BASE DATA
	public		static		Gacha_Box_BaseData			Gacha_Box_BaseData						{ get; } = new Gacha_Box_BaseData( EACDataType.Gacha_Box_Base );
	public		static		implicit	operator		Gacha_Box_BaseData( ACDC kDC )			{ return Gacha_Box_BaseData; }

	/// GACHA TABLE DATA
	public		static		Gacha_TableData				Gacha_TableData							{ get; } = new Gacha_TableData( EACDataType.Gacha_Table );
	public		static		implicit	operator		Gacha_TableData( ACDC kDC )				{ return Gacha_TableData; }

	/// MISSION BASE DATA
	public		static		Mission_BaseData			Mission_BaseData						{ get; } = new Mission_BaseData( EACDataType.Mission_Base );
	public		static		implicit	operator		Mission_BaseData( ACDC kDC )			{ return Mission_BaseData; }

	/// MISSION INFO DATA
	public		static		Mission_infoData			Mission_infoData						{ get; } = new Mission_infoData( EACDataType.Mission_info );
	public		static		implicit	operator		Mission_infoData(ACDC kDC)				{ return Mission_infoData; }

	/// POWER LEVEL TABLE DATA
	public		static		PowerLevel_TableData		PowerLevel_TableData					{ get; } = new PowerLevel_TableData( EACDataType.PowerLevel_Table );
	public		static		implicit	operator		PowerLevel_TableData(ACDC kDC)			{ return PowerLevel_TableData; }

	/// SHOP LIST DATA
	public		static		ShopListData				ShopListData							{ get; } = new ShopListData( EACDataType.ShopList );
	public		static		implicit	operator		ShopListData(ACDC kDC)					{ return ShopListData; }

	/// SHOP ITEM LIST DATA
	public		static		ShopItemListData			ShopItemListData						{ get; } = new ShopItemListData( EACDataType.ShopItemList );
	public		static		implicit	operator		ShopItemListData(ACDC kDC)				{ return ShopItemListData; }

	/// CHARACTER SELECT DATA
	public		static		CharacterSelectData			CharacterSelectData						{ get; } = new CharacterSelectData( EACDataType.CharacterSelect );
	public		static		implicit	operator		CharacterSelectData(ACDC kDC)			{ return CharacterSelectData; }

	// AD LIST DATA
	public		static		AdListData					AdListData								{ get; } = new AdListData( EACDataType.AdList );
	public		static		implicit	operator		AdListData(ACDC kDC)					{ return AdListData; }

	public		static		RankingRewardData			RankingRewardData						{ get; } = new RankingRewardData( EACDataType.RankingReward);
	public		static		implicit	operator		RankingRewardData(ACDC kDC)				{ return RankingRewardData; }
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

	/// INGAME UI DATA
	public		static		InGameUIData				InGameUIData							{ get; } = new InGameUIData( EACDataType.InGameUI );
	public		static		implicit	operator		InGameUIData(ACDC kDC)					{ return InGameUIData; }

	/// MAP COMMON DATA
	public		static		MapCommonData				MapCommonData							{ get; } = new MapCommonData( EACDataType.MapCommonData );
	public		static		implicit	operator		MapCommonData(ACDC kDC)					{ return MapCommonData; }

#endif
}