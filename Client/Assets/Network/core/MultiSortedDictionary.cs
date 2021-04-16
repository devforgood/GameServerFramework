using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class MultiSortedDictionary<Key, Value>
    {
        private SortedDictionary<Key, List<Value>> dic_ = null;

        public MultiSortedDictionary()
        {
            dic_ = new SortedDictionary<Key, List<Value>>();
        }

        public void Clear()
        {
            dic_.Clear();
        }

        public void Add(Key key, Value value)
        {
            List<Value> list = null;

            if (dic_.TryGetValue(key, out list))
            {
                list.Add(value);
            }
            else
            {
                list = new List<Value>();
                list.Add(value);
                dic_.Add(key, list);
            }
        }

        public bool ContainsKey(Key key)
        {
            return dic_.ContainsKey(key);
        }

        public List<Value> this[Key key]
        {
            get
            {
                List<Value> list = null;
                if (!dic_.TryGetValue(key, out list))
                {
                    list = new List<Value>();
                    dic_.Add(key, list);
                }

                return list;
            }
        }

        public IEnumerable keys
        {
            get
            {
                return dic_.Keys;
            }
        }
    }
}
