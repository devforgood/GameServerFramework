using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Lobby
{
    public static partial class CharacterListExtensions
    {
        /// <summary>
        /// 현재 보유중인 케릭터 리스트를 기준으로 획득 가능한 아이템 인지 확인
        /// </summary>
        /// <param name="characters"></param>
        /// <param name="item_id"></param>
        /// <returns></returns>
        public static bool IsAvailable(this List<Models.Character> characters, int item_id)
        {
            var game_item_data = ACDC.GameItemData[item_id];
            switch ((GameItemType)game_item_data.Item_Type)
            {
                case GameItemType.Goods:
                    {
                        if ((GameItemId)game_item_data.id == GameItemId.UpgradeStone)
                        {
                            var character = characters.Where(x => x.character_level == 9).FirstOrDefault();
                            if (character == null || character == default(Models.Character))
                            {
                                return false;
                            }
                        }
                    }
                    break;
                case GameItemType.CharacterPiece:
                    {
                        var character = characters.Where(x => x.character_type == game_item_data.LinkId).FirstOrDefault();
                        if (character == null || character == default(Models.Character))
                        {
                            return false;
                        }

                        if (ACDC.CharacterData[character.character_type].MaxLevel <= character.character_level)
                        {
                            return false;
                        }
                    }
                    break;
                case GameItemType.Character:
                    {
                        var character = characters.Where(x => x.character_type == game_item_data.LinkId).FirstOrDefault();
                        if (character != null && character != default(Models.Character))
                        {
                            return false;
                        }
                    }
                    break;
            }
            return true;
        }
    }
}