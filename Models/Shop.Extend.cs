using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Shop : IBaseModel
    {
        public void Copy(Shop m)
        {
            shop_id = m.shop_id;
            occ_time = m.occ_time;

            shop_item_id = m.shop_item_id;
            quantity = m.quantity;
            purchase_count = m.purchase_count;
        }

        public void Clear()
        {
            this.Copy(new Shop());
        }

        public long GetUserNo()
        {
            return user_no;
        }

        public int GetKey()
        {
            return (int)shop_no;
        }

        public long GetValue()
        {
            return shop_no;
        }

        public void SetUpdater(Func<Task> func)
        {
            throw new NotImplementedException();
        }

        public void SetDirty(bool isDirty)
        {
        }
    }
}
