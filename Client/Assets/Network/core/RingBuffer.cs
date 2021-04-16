using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class RingBuffer<T>
    {
        T[] m_Buffer;
        public int m_NextWrite; // 다음 데이터를 넣을 곳
        public int m_CurrRead;
        public int Count;

        public RingBuffer(int length)
        {
            m_Buffer = new T[length];
            m_NextWrite = 0;
            m_CurrRead = 0;
            Count = 0;
        }

        public int Length { get { return m_Buffer.Length; } }

        public bool IsFull
        {
            get
            {
                return Count >= Length;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public void Add(T o)
        {
            m_Buffer[m_NextWrite] = o;
            m_NextWrite = mod(m_NextWrite + 1, m_Buffer.Length);
            ++Count;
            if (Count > Length)
                Count = Length;
        }

        public T GetNext()
        {
            if (m_CurrRead == m_NextWrite)
                return default(T);

            m_CurrRead = mod(m_CurrRead + 1, m_Buffer.Length);
            --Count;
            if (Count < 0)
                Count = 0;
            return m_Buffer[m_CurrRead];
        }

        private int mod(int x, int m) // x mod m works for both positive and negative x (unlike x % m).
        {
            return (x % m + m) % m;
        }

#if DEBUG
        public T[] Raw { get { return (T[])m_Buffer.Clone(); } } // For debugging only.
#endif

    }
}
