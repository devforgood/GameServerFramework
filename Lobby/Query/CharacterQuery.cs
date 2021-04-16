using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class CharacterQuery : IQuery<Models.Character>
    {
        public async Task<List<Models.Character>> Gets(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.character_info.AsNoTracking().Where(x => x.user_no == user_no).ToListAsync();
            }
        }

        public async Task<Models.Character> Get(long member_no, long character_no)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    return await context.character_info.AsNoTracking().Where(x => x.character_no == character_no).FirstOrDefaultAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return null;
            }
        }

        public async Task<Models.Character> Insert(long member_no, Models.Character character)
        {
            using(var context = new Lobby.Models.GameContext(member_no))
            {
                await context.character_info.AddAsync(character);
                await context.SaveChangesAsync();
                return character;
            }
        }

        public async Task<bool> UpdateCharacterGrowth(long member_no, Models.Character character)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    await context.Database.ExecuteSqlRawAsync($"upDate character_info set battle_score = {character.battle_score}, rank_level = {character.rank_level} where character_no = {character.character_no}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return false;
            }
            return true;
        }

        public async Task<bool> Update(long member_no, Models.Character character)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var row = await context.character_info.Where(x => x.character_no == character.character_no).FirstOrDefaultAsync();
                    if (row != null && row != default(Models.Character))
                    {
                        row.Copy(character);
                        await context.SaveChangesAsync();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return false;
            }
            return true;
        }
        public static async Task<Models.Character> Remove(long member_no, Models.Character character)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.character_info.Remove(character);
                await context.SaveChangesAsync();
                return character;
            }
        }
    }
}
