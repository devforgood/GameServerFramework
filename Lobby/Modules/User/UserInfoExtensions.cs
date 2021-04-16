using GameService;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public static partial class UserInfoExtensions
    {
        public static async Task Load(this UserInfo userInfo, Session session)
        {
            long user_no = session.user_no;
            userInfo.AccountGoods = new Goods();
            var user = await UserCache.GetUser(session.member_no, user_no, true);
            int characterId = 0;
            var characters = await CharacterCache.Instance.GetEntities(session.member_no, user_no, true);
            if (characters != null)
            {
                // 선택된 케릭터가 없다면 첫번째 케릭터로 내려줌
                if (characters.Count > 0)
                {
                    characterId = characters[0].character_type;
                }

                foreach (var character in characters)
                {
                    var character_data = ACDC.CharacterData[character.character_type];

                    userInfo.CharacterList.Add(CharacterManager.CreateCharacterInfo(character, character_data));

                    if (user.character_no == character.character_no)
                        characterId = character.character_type;
                }
            }

            //userInfo.UserName = user.user_name ?? "";
            userInfo.UserName = session.user_name ?? "";
            userInfo.AccountBattleScore = user.battle_score;
            userInfo.SelectedCharacterId = characterId;
            userInfo.SelectedMapId = session.map_id;
            userInfo.AccountGoods.Set(user);


        }
    }
}
