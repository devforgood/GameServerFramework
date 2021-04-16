using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class MapGameObject : NetGameObject
    {
        protected UInt16 mapObjectId;

        public override int GetMapId() { return mapObjectId; }

        protected void Init()
        {
            //core.LogHelper.LogInfo($"MapGameObject.Init world id : {WorldId}, map uid :{GetMapId()}");
            World.Instance(WorldId).RegisterNetGameObject((GameObjectClassId)GetClassId(), GetMapId(), this);
        }
    }
}
