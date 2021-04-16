using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Lobby.Models
{
    public class GameContext : DbContext
    {
#if DEBUG
        public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter((category, level) =>
                    category == DbLoggerCategory.Database.Command.Name
                    && level == LogLevel.Information)
                .AddConsole()
                .AddDebug()
                .AddSerilog()
                ;

        });
#endif

        int shard_id_;

        public DbSet<Lobby.Models.User> user { get; set; }
        public DbSet<Lobby.Models.Character> character_info { get; set; }
        public DbSet<Lobby.Models.Mission> mission { get; set; }
        public DbSet<Lobby.Models.Shop> shop { get; set; }
        public DbSet<Lobby.Models.GameEvent> game_event { get; set; }

        public DbSet<Lobby.Models.AdvertisementReward> advertisement_reward { get; set; }

        public DbSet<Lobby.Models.Mail> mailbox { get; set; }

        public GameContext(long member_no)
        {
            shard_id_ = Shard.GetShardId(member_no);

        }

        /// <summary>
        /// member_no로 부터 샤드 아이디 얻기
        /// </summary>
        /// <param name="user_no"></param>
        /// <returns></returns>
        public GameContext(Session session)
        {
            // 수평샤딩
            //  범용적인 구성 방법
            //            shard key는 일반적으로 유저를 기준으로 하는 accountId(memberId) 를 사용
            //            대상 테이블의 PK는 shard key + @ 로 사용을 권장(ex: accountId + itemId)
            //            분산은 range, modulo 를 일반적으로 사용하지만
            //                 최근 가입한 유저가 더 많은 트래픽을 유발할 수 있는 게임의 특성상 modulo연산 사용을 권장
            //            론칭 후 운영 중인 상태에서는 데이터를 재배치 하기 힘드므로
            //                 성능테스트 일정 이전엔 2개의 shard만 구성하고 성능테스트 결과에 따라 shard 갯수 최종 픽스


            //return user_no % 2;

            shard_id_ = Shard.GetShardId(session.member_no);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // todo :디비풀 이용을 위해 AddDbContextPool로 변경이 필요
            
            optionsBuilder
#if DEBUG
                .UseLoggerFactory(MyLoggerFactory)
#endif
                .UseMySQL(ServerConfiguration.Instance.GameContext[shard_id_]);
        }

        public static void test()
        {
            using (var context = new Lobby.Models.GameContext(0))
            {
                try
                {
                    var row = context.user.Where(x => x.user_no == 4).FirstOrDefault();
                    int a = (int)row.play_point;

                    // System.Threading.Thread.Sleep(10000);
                    row.play_point += 10;
                    context.SaveChanges();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            var result = GetUser().Result;

            //using (var context = new Lobby.Models.GameContext())
            //{
            //    try
            //    {
            //        var user = new User()
            //        {
            //            character_no = 1,
            //            user_name = Guid.NewGuid().ToString("N"),
            //        };

            //        context.user.Add(user);
            //        context.SaveChanges();

            //        var charac = new Character()
            //        {
            //            user_no = user.user_no,
            //            character_level = 1,
            //            character_type = 1,
            //        };
            //        context.character_info.Add(charac);
            //        context.SaveChanges();

            //        user.character_no = charac.character_no;
            //        context.SaveChanges();

            //        var item = new Item()
            //        {
            //            user_no = user.user_no,
            //            item_id = 1000,
            //            item_count = 10,
            //        };
            //        context.item.Add(item);
            //        context.SaveChanges();







            //        var ret = context.character_info.Where(x => x.character_no == 2).ToList();
            //        ret[0].character_type = 3;
            //        context.SaveChanges();

            //        context.Database.ExecuteSqlCommand("update character_info set character_type = 5 where character_no = 1");

            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e);
            //        // Provide for exceptions.
            //    }
            //}

            testRemove();

            var result2 = GetCharacter().Result;


        }

        static void testRemove()
        {
            using (var context = new GameContext(0))
            {
                var deleteOrderDetails =
                    from user in context.user
                    where user.user_no == 2
                    select user;

                foreach (var detail in deleteOrderDetails)
                {
                    context.user.Remove(detail);
                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    // Provide for exceptions.
                }
            }
        }

        private static async Task<User> GetUser()
        {
            using (var context = new Lobby.Models.GameContext(0))
            {
                return await context.user.FirstOrDefaultAsync();
            }
        }

        private static async Task<Character> GetCharacter()
        {
            using (var context = new Lobby.Models.GameContext(0))
            {
                return await context.character_info.FromSqlRaw("SELECT * FROM character_info").FirstOrDefaultAsync();
            }
        }
    }
}
