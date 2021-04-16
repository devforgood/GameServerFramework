using core;
﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    class GachaBox
    {
        static public async Task<bool> Progress(Session session, Models.User user, Models.Character character, JGameItemData game_item_data, GameService.ItemList item, LogReason reason)
        {
            JGacha_Box_BaseData gacha_base = ACDC.Gacha_Box_BaseData[game_item_data.LinkId];

            var characters = await CharacterCache.Instance.GetEntities(session.member_no, session.user_no, true);
            Dictionary<int, int> gacha_results = new Dictionary<int, int>();
            for (int i = 0; i < gacha_base.reward_item_group_id.Length; ++i)
            {
                _Progress(gacha_results, characters, gacha_base.reward_item_group_id[i], item);
            }

            if (gacha_base.bonus_rate != 0 && MathHelpers.GetRandomInt(100) < gacha_base.bonus_rate)
            {
                _Progress(gacha_results, characters, gacha_base.bonus_group_id, item, true);
            }

            foreach(var gacha_result in gacha_results)
            {
                await Inventory.Insert(session, user, character, gacha_result.Key, gacha_result.Value, reason, null, item?.Items.Where(x => x.ItemId == gacha_result.Key).Count().ToString(), game_item_data.id.ToString());
            }

            return true;
        }

        private static void _Progress(Dictionary<int, int> gacha_results, List<Models.Character> characters, int group_id, GameService.ItemList item = null, bool isBonus = false)
        {
            var gacha_table = ACDC.Gacha_TableData.Values.Where(x => x.GroupId == group_id && characters.IsAvailable(x.ItemID)).ToList();
            if (gacha_table.Count == 0)
                return;
            
            int rate = MathHelpers.GetRandomInt(gacha_table.Sum(x => x.get_rate));
            int gacha_item_id = 0, gacha_item_count = 0;
            foreach (var gacha_data in gacha_table)
            {
                if (rate < gacha_data.get_rate)
                {
                    gacha_item_id = gacha_data.ItemID;
                    gacha_item_count = MathHelpers.GetRandomInt(gacha_data.Count_min, gacha_data.Count_max + 1);
                    break;
                }
                rate -= gacha_data.get_rate;
            }

            if (gacha_item_id == 0)
                return;

            if (!gacha_results.ContainsKey(gacha_item_id))
                gacha_results.Add(gacha_item_id, gacha_item_count);
            else
                gacha_results[gacha_item_id] += gacha_item_count;

            if ((GameItemType)ACDC.GameItemData[gacha_item_id].Item_Type == GameItemType.Character)
                characters.Add(new Models.Character() { character_type = ACDC.GameItemData[gacha_item_id].LinkId });

            if (item != null)
                item.Items.Add(new GameService.ItemInfo() { ItemId = gacha_item_id, ItemCount = gacha_item_count, IsBonus = isBonus });
        }
    }
}
