using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class DictionaryExtensions
{
    public static void Increment<T>(this Dictionary<T, int> dictionary, T key)
    {
        int count;
        dictionary.TryGetValue(key, out count);
        dictionary[key] = count + 1;
    }

    public static void Increment<T>(this Dictionary<T, int> dictionary, T key, int value)
    {
        int count;
        dictionary.TryGetValue(key, out count);
        dictionary[key] = count + value;
    }
}