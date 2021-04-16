using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public interface IQuery<T>
    {
        Task<List<T>> Gets(long member_no, long user_no);

        Task<T> Get(long member_no, long key);

        Task<T> Insert(long member_no, T row);

        Task<bool> Update(long member_no, T row);
    }
}
