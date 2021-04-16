using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class ShopQuery : IQuery<Models.Shop>
    {
        public async Task<List<Models.Shop>> Gets(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.shop.AsNoTracking().Where(x => x.user_no == user_no).ToListAsync();
            }
        }

        public async Task<Models.Shop> Get(long member_no, long shop_no)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    return await context.shop.AsNoTracking().Where(x => x.shop_no == shop_no).FirstOrDefaultAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return null;
            }
        }

        public async Task<Models.Shop> Insert(long member_no, Models.Shop shop)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                await context.shop.AddAsync(shop);
                await context.SaveChangesAsync();
                return shop;
            }
        }

        public async Task<bool> Update(long member_no, Models.Shop shop)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var db_shop = await context.shop.Where(x => x.shop_no == shop.shop_no).FirstOrDefaultAsync();
                    if (db_shop != null && db_shop != default(Models.Shop))
                    {
                        db_shop.Copy(shop);
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

        public static async Task<Models.Shop> Remove(long member_no, Models.Shop shop)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.shop.Remove(shop);
                await context.SaveChangesAsync();
                return shop;
            }
        }
    }
}
