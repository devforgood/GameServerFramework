using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public interface IDirty
    {
        void SetDirty(bool v);
        bool IsDirty();
    }
}
