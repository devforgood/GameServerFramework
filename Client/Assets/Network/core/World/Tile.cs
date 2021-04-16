using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class Tile
    {
        public List<NetGameObject> gameObjects = new List<NetGameObject>();


        public bool add(NetGameObject target)
        {
            if (gameObjects.Exists(x => x == target) == true)
            {
                LogHelper.LogError("deplicate game object while add in tile!");
                return false;
            }

            gameObjects.Add(target);
            return true;
        }

        public bool del(NetGameObject target)
        {
            var ret = gameObjects.Remove(target);
            if(ret==false)
            {
                LogHelper.LogError("not exist game object while del in tile!");
            }

            return ret;
        }

    }
}
