using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public interface IBaseModel
    {
        long GetUserNo();
        int GetKey();
        long GetValue();

        void SetUpdater(Func<Task> func);

        void SetDirty(bool isDirty);
    }
}
