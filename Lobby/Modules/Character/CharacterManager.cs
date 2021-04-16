using GameService;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class CharacterManager
    {
        public static CharacterInfo CreateCharacterInfo(Models.Character character, JCharacterData character_data)
        {
            return new CharacterInfo()
            {
                CharacterId = character.character_type,
                CharacterBattleScore = character.battle_score,
                CharacterRank = character.rank_level,
                CharacterLevel = character.character_level,
                CharacterPiece = character.piece,
                StrikingPower = character_data.GetStrikingPower(character.character_level),
                HP = character_data.GetCharacterHp(character.character_level),
                RareLabel = character_data.RareLabel,
                UnionType = character_data.UnionType,
                IsPowerLevelUp = character_data.IsPowerLevelUp(character.character_level, character.piece),
            };
        }

        public static async Task<Models.Character> InsertCharacter(long member_no, long user_no, string user_name, string player_id, int character_id)
        {
            var character_data = ACDC.CharacterData[character_id];
            if (character_data == null || character_data == default(JCharacterData))
            {
                Log.Error($"Character cannot find character_id:{character_id}, user_name:{user_name}");
                return null;
            }

            var character = await CharacterCache.Instance.GetEntity(member_no, user_no, character_id, true, false, false);
            if (character == null || character == default(Models.Character))
            {
                character = await CharacterCache.Instance.InsertEntity(member_no, new Models.Character()
                {
                    user_no = user_no,
                    character_type = character_id,
                    character_level = character_data.Level,
                    rank_level = 1,
                });
            }
            else
            {
                Log.Error($"Character already exist character_id:{character_id}, user_name:{user_name}");
                return null;
            }
        
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("Name", user_name);
            await Ranking.PutProperty(player_id, character_id.ToString(), properties);

            return character;
        }

        public static async Task<bool> AddCharacterPiece(Session session, Models.Character character, int character_id, int piece_count)
        {
            var find_character = await CharacterCache.Instance.GetEntity(session.member_no, session.user_no, character_id, true, false, false);
            if (find_character == null || find_character == default(Models.Character))
            {
                Log.Error($"CharacterPiece cannot find character_id:{character_id}, user_name:{session.user_name}");
                return false;
            }

            if (character.character_no == find_character.character_no)
            {
                character.piece += piece_count;
                character.IsDirty = true;
            }
            else
            {
                find_character.piece += piece_count;
                await CharacterCache.Instance.UpdateEntity(session.member_no, find_character);
            }
            return true;
        }
    }
}
