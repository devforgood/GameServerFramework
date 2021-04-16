using core;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class Inventory
    {
        public static bool UseGoods(Session session, Models.User user, int item_id, int item_count, LogReason reason)
        {
            var game_item_data = ACDC.GameItemData[item_id];
            if (game_item_data == null || game_item_data == default(JGameItemData))
            {
                Log.Error($"cannot find item:{item_id}, user_name:{session.user_name}");
                return false;
            }

            if (user[item_id] < item_count)
                return false;

            user[item_id] -= item_count;

            user.IsDirty = true;
            //History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.UseItem, (byte)reason, (int)item_id, (int)item_count, "", "");

            if (reason != null)
            {
                _ = LogProxy.writeResourceLog(session, reason.paid, item_id.ToString(), -1 * item_count, 0, user[item_id], "sub", reason.reason, reason.sub_reason).ConfigureAwait(false);
            }

            return true;
        }

        public static async Task<bool> Insert(Session session, Models.User user, Models.Character character, int item_id, int item_count, LogReason reason, GameService.ItemList item = null, string sParam1 = "", string sParam2 = "")
        {
            var game_item_data = ACDC.GameItemData[item_id];
            if (game_item_data == null || game_item_data == default(JGameItemData))
            {
                Log.Error($"cannot find item:{item_id}, user_name:{session.user_name}");
                return false;
            }

            switch ((GameItemType)game_item_data.Item_Type)
            {
                case GameItemType.Goods:
                    {
                        user[item_id] += item_count;

                        user.IsDirty = true;

                        if (item != null)
                        {
                            item.Items.Add(new GameService.ItemInfo() { ItemId = item_id, ItemCount = item_count });
                        }

                    }
                    break;
                case GameItemType.Medal_Charging:
                    {
                        user.medal_charge = (int)MedalChargeConst.MaxCharge;
                        user.medal_charge_time = DateTime.UtcNow;

                        user.IsDirty = true;

                        if (item != null)
                        {
                            item.Items.Add(new GameService.ItemInfo() { ItemId = item_id, ItemCount = item_count });
                        }
                    }
                    break;
                case GameItemType.Character:
                    {
                        if (await CharacterManager.InsertCharacter(session.member_no, session.user_no, session.user_name, session.player_id, game_item_data.LinkId) == null)
                        {
                            return false;
                        }

                        if (item != null)
                        {
                            item.Items.Add(new GameService.ItemInfo() { ItemId = item_id, ItemCount = item_count });
                        }

                        _ = LogProxy.writeActionLog(session, "캐릭터", "획득", game_item_data.LinkId.ToString()).ConfigureAwait(false);
                    }
                    break;
                case GameItemType.CharacterPiece:
                    {
                        if (await CharacterManager.AddCharacterPiece(session, character, game_item_data.LinkId, item_count) == false)
                        {
                            return false;
                        }

                        if (item != null)
                        {
                            item.Items.Add(new GameService.ItemInfo() { ItemId = item_id, ItemCount = item_count });
                        }

                        _ = LogProxy.writeActionLog(session, "캐릭터", "조각획득", game_item_data.LinkId.ToString()).ConfigureAwait(false);
                    }
                    break;
                case GameItemType.Gacha:
                    {
                        if(await GachaBox.Progress(session, user, character, game_item_data, item, reason) == false)
                            return false;
                    }
                    break;
                default:
                    return false;
            }

            await MissionManager.OnTrigger(session, MissionUserAction.GetItem, item_id, item_count);

            //History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.GainItem, (byte)reason, (int)item_id, (int)item_count, sParam1, sParam2);

            if (reason != null)
            {
                if ((GameItemType)game_item_data.Item_Type == GameItemType.Goods)
                {
                    _ = LogProxy.writeResourceLog(session, reason.paid, item_id.ToString(), item_count, 0, user[item_id], "add", reason.reason, reason.sub_reason).ConfigureAwait(false);
                }
                else
                {
                    _ = LogProxy.writeItemLog(session, game_item_data.Item_Type.ToString(), item_id.ToString(), "y", "", item_count, "", 0, 0, "add", reason.reason, reason.sub_reason, "").ConfigureAwait(false);
                }
            }

            return true;
        }
    }
}
