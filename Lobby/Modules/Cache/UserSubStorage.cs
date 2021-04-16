using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class UserSubStorage<T, T2> 
        where T : class, Models.IBaseModel
        where T2 : class, IQuery<T>
    {
        protected static readonly TimeSpan session_expire = new TimeSpan(2, 0, 0);
        protected T2 query;
        protected string entities_name;
        protected string entity_name;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_no"></param>
        /// <returns></returns>
        public async Task<List<T>> GetEntities(long member_no, long user_no, bool is_read_db)
        {
            T entity = null;
            List<T> entities = null;

            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entities_name}:{user_no}");
            if (ret.HasValue == true)
            {
                var entity_dic = JsonConvert.DeserializeObject<Dictionary<int, long>>(ret);
                entities = new List<T>();
                foreach (var charac in entity_dic)
                {
                    var entity_no = (long)charac.Value;

                    entity = await GetEntity(member_no, entity_no, is_read_db, false, false);
                    if (entity != null && entity != default(T))
                    {
                        entities.Add(entity);
                    }
                }
            }
            else if (is_read_db)
            {
                entities = await query.Gets(member_no, user_no);
                await Cache.Instance.GetDatabase().StringSetAsync($"{entities_name}:{user_no}", JsonConvert.SerializeObject(entities.ToDictionary(x => x.GetKey(), x => x.GetValue())), session_expire);
                for (int i = 0; i < entities.Count; ++i)
                {
                    await CacheEntity(member_no, entities[i], false);
                }
            }

            return entities;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity_no"></param>
        /// <param name="is_read_db"></param>
        /// <returns></returns>
        public async Task<T> GetEntity(long member_no, long entity_no, bool is_read_db, bool is_dirty_update, bool is_dirty)
        {
            T entity = null;
            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entity_name}:{entity_no}:{Shard.GetShardId(member_no)}");
            if (ret.HasValue == true)
            {
                entity = JsonConvert.DeserializeObject<T>(ret);
            }
            else if (is_read_db)
            {
                entity = await query.Get(member_no, entity_no);
                await SetEntity(entity, member_no);
            }

            if (is_dirty_update && entity != null)
            {
                entity.SetUpdater(async () =>
               {
                   await UpdateEntity(member_no, entity);
               });
            }

            if (entity != null)
                entity.SetDirty(is_dirty);

            return entity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user_no"></param>
        /// <returns></returns>
        public async Task<T> GetEntity(long member_no, long user_no, int entity_key, bool is_read_db, bool is_dirty_update, bool is_dirty)
        {
            T entity = null;
            List<T> entities = null;

            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entities_name}:{user_no}");
            if (ret.HasValue == true)
            {
                var entity_dic = JsonConvert.DeserializeObject<Dictionary<int, long>>(ret);
                long entity_no = 0;
                if (entity_dic.TryGetValue(entity_key, out entity_no))
                {
                    entity = await GetEntity(member_no, entity_no, is_read_db, is_dirty_update, is_dirty);
                }
            }
            else if (is_read_db)
            {
                entities = await query.Gets(member_no, user_no);
                await Cache.Instance.GetDatabase().StringSetAsync($"{entities_name}:{user_no}", JsonConvert.SerializeObject(entities.ToDictionary(x => x.GetKey(), x => x.GetValue())), session_expire);
                for (int i = 0; i < entities.Count; ++i)
                {
                    await CacheEntity(member_no, entities[i], false);
                }

                entity = entities.Where(x => x.GetKey() == (byte)entity_key).FirstOrDefault();

            }

            if (is_dirty_update && entity != null)
            {
                entity.SetUpdater(async () =>
                {
                    await UpdateEntity(member_no, entity);
                });
            }

            if (entity != null)
                entity.SetDirty(is_dirty);

            return entity;
        }

        public async Task<bool> SetEntity(T entity, long member_no)
        {
            try
            {
                return await Cache.Instance.GetDatabase().StringSetAsync($"{entity_name}:{entity.GetValue()}:{Shard.GetShardId(member_no)}", JsonConvert.SerializeObject(entity), session_expire);
            }
            catch(Exception ex)
            {
                Log.Error(ex.Message);
                return false;
            }
        }

        public async Task<T> InsertEntity(long member_no, T entity)
        {
            // 디비 인서트
            entity = await query.Insert(member_no, entity);

            await CacheEntity(member_no, entity, true);

            return entity;
        }

        public async Task<T> UpdateEntity(long member_no, T entity)
        {
            await query.Update(member_no, entity);

            await CacheEntity(member_no, entity, false);

            return entity;
        }

        async Task CacheEntity(long member_no, T entity, bool is_change_entity_list)
        {
            if (is_change_entity_list)
            {
                var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entities_name}:{entity.GetUserNo()}");
                if (ret.HasValue == true)
                {
                    var entity_dic = JsonConvert.DeserializeObject<Dictionary<int, long>>(ret);
                    entity_dic[entity.GetKey()] = entity.GetValue();

                    // 케릭터 목록 캐싱
                    await Cache.Instance.GetDatabase().StringSetAsync($"{entities_name}:{entity.GetUserNo()}", JsonConvert.SerializeObject(entity_dic), session_expire);
                }
            }

            // 케릭터 캐싱
            await Cache.Instance.GetDatabase().StringSetAsync($"{entity_name}:{entity.GetValue()}:{Shard.GetShardId(member_no)}", JsonConvert.SerializeObject(entity), session_expire);
        }

        public async Task<bool> RemoveEntity(long member_no, T entity)
        {
            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entities_name}:{entity.GetUserNo()}");
            if (ret.HasValue == true)
            {
                var entity_dic = JsonConvert.DeserializeObject<Dictionary<int, long>>(ret);
                if (entity_dic.Remove(entity.GetKey()) == false)
                    return false;

                // 케릭터 목록 캐싱
                await Cache.Instance.GetDatabase().StringSetAsync($"{entities_name}:{entity.GetUserNo()}", JsonConvert.SerializeObject(entity_dic), session_expire);
            }

            await Cache.Instance.GetDatabase().KeyDeleteAsync($"{entity_name}:{entity.GetValue()}:{Shard.GetShardId(member_no)}");

            return true;
        }

        public async Task<bool> RemoveEntities(long member_no, long user_no)
        {
            var db = Cache.Instance.GetDatabase();
            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"{entities_name}:{user_no}");
            if (ret.HasValue == true)
            {
                var entity_dic = JsonConvert.DeserializeObject<Dictionary<int, long>>(ret);
                foreach (var entity in entity_dic)
                {
                    await Cache.Instance.GetDatabase().KeyDeleteAsync($"{entity_name}:{entity.Value}:{Shard.GetShardId(member_no)}");
                }
            }

            await Cache.Instance.GetDatabase().KeyDeleteAsync($"{entities_name}:{user_no}");
            return true;
        }
    }
}
