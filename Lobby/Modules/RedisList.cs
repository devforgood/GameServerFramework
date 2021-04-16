using Lobby;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

[Obsolete("This class is obsolete. not supported async/await")]
public class RedisList<T> : IList<T>
{
    private static ConnectionMultiplexer _cnn;
    private string key;
    public RedisList(string key)
    {
        this.key = key;
        _cnn = Cache.Instance.GetConnection();
    }
    private IDatabase GetRedisDb()
    {
        return _cnn.GetDatabase();
    }
    private string Serialize(object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }
    private T Deserialize<T>(string serialized)
    {
        return JsonConvert.DeserializeObject<T>(serialized);
    }
    public void Insert(int index, T item)
    {
        var db = GetRedisDb();
        var before = db.ListGetByIndex(key, index);
        db.ListInsertBefore(key, before, Serialize(item));
    }
    public void RemoveAt(int index)
    {
        var db = GetRedisDb();
        var value = db.ListGetByIndex(key, index);
        if (!value.IsNull)
        {
            db.ListRemove(key, value);
        }
    }
    public T this[int index]
    {
        get
        {
            var value = GetRedisDb().ListGetByIndex(key, index);
            return Deserialize<T>(value.ToString());
        }
        set
        {
            Insert(index, value);
        }
    }
    public void Add(T item)
    {
        GetRedisDb().ListRightPush(key, Serialize(item));
    }
    public void Clear()
    {
        GetRedisDb().KeyDelete(key);
    }
    public bool Contains(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (GetRedisDb().ListGetByIndex(key, i).ToString().Equals(Serialize(item)))
            {
                return true;
            }
        }
        return false;
    }
    public void CopyTo(T[] array, int arrayIndex)
    {
        GetRedisDb().ListRange(key).CopyTo(array, arrayIndex);
    }
    public int IndexOf(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (GetRedisDb().ListGetByIndex(key, i).ToString().Equals(Serialize(item)))
            {
                return i;
            }
        }
        return -1;
    }
    public int Count
    {
        get { return (int)GetRedisDb().ListLength(key); }
    }
    public bool IsReadOnly
    {
        get { return false; }
    }
    public bool Remove(T item)
    {
        return GetRedisDb().ListRemove(key, Serialize(item)) > 0;
    }
    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < this.Count; i++)
        {
            yield return Deserialize<T>(GetRedisDb().ListGetByIndex(key, i).ToString());
        }
    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < this.Count; i++)
        {
            yield return Deserialize<T>(GetRedisDb().ListGetByIndex(key, i).ToString());
        }
    }
}