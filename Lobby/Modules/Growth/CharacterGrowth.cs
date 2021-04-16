using GameService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class CharacterGrowth
    {
        public static async Task<(ErrorCode, Goods, CharacterInfo)> UpgradePowerLevel(Session session, int character_id)
        {
            var character_data = ACDC.CharacterData[character_id];
            if (character_data == null || character_data == default(JCharacterData))
            {
                Log.Information($"UpgradePowerLevel cannot find character_id:{character_id}, user_name:{session.user_name}");
                return (ErrorCode.WrongParam, null, null);
            }

            var level_table = character_data.LevelTable;
            var goods = new Goods();
            CharacterInfo character_info = null;
            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.user_no, character_id, true, true, false))
                {
                    if (character == null || character == default(Models.Character))
                    {
                        Log.Information($"UpgradePowerLevel cannot find character_id:{character_id}, user_name:{session.user_name}");
                        return (ErrorCode.WrongParam, null, null);
                    }

                    JPowerLevel_TableData power_level_data;
                    if(level_table.TryGetValue(character.character_level, out power_level_data)==false)
                    {
                        return (ErrorCode.OverLimit, null, null);
                    }

                    if(character.piece < power_level_data.Req_Piece)
                    {
                        return (ErrorCode.NotEnough, null, null);
                    }

                    if (Inventory.UseGoods(session, user, power_level_data.Pricetype, power_level_data.Req_PriceValue, new LogReason("S_UPGRADE_SMASHER", character_id.ToString())) == false)
                    {
                        return (ErrorCode.NotEnough, null, null);
                    }

                    character.piece -= power_level_data.Req_Piece;
                    character.character_level += 1;
                    character.IsDirty = true;

                    goods.Set(user);

                    character_info = CharacterManager.CreateCharacterInfo(character, character_data);

                    var game_item_data = ACDC.GameItemData.Values.Where(x => x.Item_Type == (int)GameItemType.CharacterPiece && x.LinkId == character_id).FirstOrDefault();
                    var item_id = game_item_data.id;
                    var item_count = power_level_data.Req_Piece;
                    _ = LogProxy.writeItemLog(session, game_item_data.Item_Type.ToString(), item_id.ToString(), "y", "", item_count, "", 0, 0, "add", "S_UPGRADE_SMASHER", character_id.ToString(), "").ConfigureAwait(false);
                    _ = LogProxy.writeActionLog(session, "캐릭터", "업그레이드", character.character_type.ToString()).ConfigureAwait(false);

                    History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.UseItem, (byte)HistoryLogReason.UpgradePowerLevel, item_id, item_count, "", "");
                }
            }
            return (ErrorCode.Success, goods, character_info);
        }

        public static void IncreaseBattleScore(Models.Character character, int battle_score)
        {
            if (battle_score < 0)
            {
                character.battle_score += battle_score;
                if (character.battle_score < 0)
                    character.battle_score = 0;
            }
            else
            {
                character.battle_score += battle_score;
            }

            var rank_data = ACDC.RankData.GetRank(character.battle_score);
            if (rank_data != null)
            {
                if (character.rank_level < rank_data.Rank)
                {
                    character.rank_level = rank_data.Rank;
                }
            }
        }
    }
}
