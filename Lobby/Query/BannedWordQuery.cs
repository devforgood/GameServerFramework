using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class BannedWordQuery
    {
        public static async Task<List<string>> GetBannedWord()
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                return await context.banned_word.AsNoTracking().Select(x=>x.word).ToListAsync();
            }
        }

    }
}
