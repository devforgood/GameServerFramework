using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace core
{
    public class WorldMap
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct WorldMapKey
        {
            [FieldOffset(0)]
            public UInt64 pos;

            [FieldOffset(0)]
            public short x;

            [FieldOffset(1)]
            public short y;

            [FieldOffset(2)]
            public short z;

            [FieldOffset(3)]
            public short group_id;
        }


        Dictionary<UInt64, Tile> map = new Dictionary<ulong, Tile>();

        void InitMap()
        {

        }

        //UInt64 MakeKey(short x, short y, short z)
        //{
        //    byte[] recbytes = new byte[8];
        //    recbytes[0] = BitConverter.GetBytes(x)[0];
        //    recbytes[1] = BitConverter.GetBytes(x)[1];
        //    recbytes[2] = BitConverter.GetBytes(y)[0];
        //    recbytes[3] = BitConverter.GetBytes(y)[1];
        //    recbytes[4] = BitConverter.GetBytes(z)[0];
        //    recbytes[5] = BitConverter.GetBytes(z)[1];
        //    return BitConverter.ToUInt64(recbytes, 0);
        //}

        //void GetPositionFromKey(UInt64 key, out short x, out short y, out short z)
        //{
        //    byte[] bytes = BitConverter.GetBytes(key);
        //    x = BitConverter.ToInt16(bytes, 0);
        //    y = BitConverter.ToInt16(bytes, 2);
        //    z = BitConverter.ToInt16(bytes, 4);
        //}

        Tile GetTile(short x, short y, short z)
        {
            WorldMapKey key;
            key.pos = 0;
            key.x = x;
            key.y = y;
            key.z = z;

            Tile t = null;
            map.TryGetValue(key.pos, out t);
            return t;
        }

        Tile CreateTile(short x, short y, short z)
        {
            WorldMapKey key;
            key.pos = 0;
            key.x = x;
            key.y = y;
            key.z = z;

            Tile t = new Tile();
            map.Add(key.pos, t);
            return t;
        }

        public Tile GetTile(Vector3 pos)
        {
            LogHelper.LogInfo("GetTile " + pos);
            return GetTile((short)pos.x, (short)pos.y, (short)pos.z);
        }

        public void ChangeLocation(NetGameObject target, Vector3 src_pos, Vector3 dest_pos)
        {
            var x = (short)Math.Round(dest_pos.x);
            var y = (short)Math.Round(dest_pos.y);
            var z = (short)Math.Round(dest_pos.z);


            var old_x = (short)Math.Round(src_pos.x);
            var old_y = (short)Math.Round(src_pos.y);
            var old_z = (short)Math.Round(src_pos.z);

            if ( old_x == x && old_y == y && old_z == z )
            {
                return;
            }

            var src_tile = GetTile(old_x, old_y, old_z);
            if(src_tile != null)
            {
                src_tile.del(target);
            }

            var dest_tile = GetTile(x, y, z);
            if(dest_tile == null)
            {
                dest_tile = CreateTile(x, y, z);
            }

            dest_tile.add(target);

            LogHelper.LogInfo($"game object {target.GetNetworkId()}, from({old_x},{old_y},{old_z}) to({x},{y},{z})");

        }

        public void InsertObject(NetGameObject target)
        {
            var x = (short)Math.Round(target.GetLocation().x);
            var y = (short)Math.Round(target.GetLocation().y);
            var z = (short)Math.Round(target.GetLocation().z);

            var dest_tile = GetTile(x, y, z);
            if (dest_tile == null)
            {
                dest_tile = CreateTile(x, y, z);
            }

            dest_tile.add(target);
        }

        public void RemoveObject(NetGameObject target)
        {
            var x = (short)Math.Round(target.GetLocation().x);
            var y = (short)Math.Round(target.GetLocation().y);
            var z = (short)Math.Round(target.GetLocation().z);

            var src_tile = GetTile(x, y, z);
            if (src_tile != null)
            {
                src_tile.del(target);
            }
            else
            {
                LogHelper.LogWarning($"cannot find object {target.NetworkId}");
            }
        }
    }
}
