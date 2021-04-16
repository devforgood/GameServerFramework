using core;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif 


public static partial class NetBufferExtensions
{
    public static void Write(this NetBuffer buff, ref Vector3 vec)
    {
        buff.Write(vec.x);
        buff.Write(vec.y);
        buff.Write(vec.z);
    }

    public static void Read(this NetBuffer buff, ref Vector3 vec)
    {
        vec.x = buff.ReadFloat();
        vec.y = buff.ReadFloat();
        vec.z = buff.ReadFloat();
    }



    /// <summary>
    /// 패킷에서 데이터 읽기 (RPC)
    /// </summary>
    /// <param name="type">파라미터 타입</param>
    public static object ReadObjectPacked(this NetBuffer buff, Type type)
    {
        if (type.IsNullable())
        {
            bool isNull = buff.ReadBoolean();

            if (isNull)
            {
                return null;
            }
        }

        if (type == typeof(byte))
            return buff.ReadByte();
        if (type == typeof(sbyte))
            return buff.ReadSByte();
        if (type == typeof(ushort))
            return buff.ReadUInt16();
        if (type == typeof(short))
            return buff.ReadInt16();
        if (type == typeof(int))
            return buff.ReadInt32();
        if (type == typeof(uint))
            return buff.ReadUInt32();
        if (type == typeof(long))
            return buff.ReadInt64();
        if (type == typeof(ulong))
            return buff.ReadUInt64();
        if (type == typeof(float))
            return buff.ReadFloat();
        if (type == typeof(double))
            return buff.ReadDouble();
        if (type == typeof(string))
            return buff.ReadString();
        if (type == typeof(bool))
            return buff.ReadBoolean();
        if (type == typeof(Vector3))
        {
            Vector3 v = default(Vector3);
            buff.Read(ref v);
            return v;
        }
        if (type == typeof(char))
            return buff.ReadByte();
        if (type.IsEnum)
            return buff.ReadInt32();
        if (type == typeof(List<int>))
        {
            List<int> v = new List<int>();
            byte listSize = buff.ReadByte();
            for(byte i=0;i<listSize;++i)
            {
                v.Add(buff.ReadInt32());
            }
            return v;
        }
        if (type == typeof(List<short>))
        {
            List<short> v = new List<short>();
            byte listSize = buff.ReadByte();
            for (byte i = 0; i < listSize; ++i)
            {
                v.Add(buff.ReadInt16());
            }
            return v;
        }
        if (type == typeof(List<float>))
        {
            List<float> v = new List<float>();
            byte listSize = buff.ReadByte();
            for (byte i = 0; i < listSize; ++i)
            {
                v.Add(buff.ReadFloat());
            }
            return v;
        }


        throw new ArgumentException("BitReader cannot read type " + type.Name);
    }

    /// <summary>
    /// 패킷에 데이터 쓰기 (RPC)
    /// </summary>
    /// <param name="value">The object to write</param>
    public static void WriteObjectPacked(this NetBuffer buff, object value)
    {
        if (value == null || value.GetType().IsNullable())
        {
            buff.Write(value == null);

            if (value == null)
            {
                return;
            }
        }

        if (value is byte)
        {
            buff.Write((byte)value);
            return;
        }
        else if (value is sbyte)
        {
            buff.Write((sbyte)value);
            return;
        }
        else if (value is ushort)
        {
            buff.Write((ushort)value);
            return;
        }
        else if (value is short)
        {
            buff.Write((short)value);
            return;
        }
        else if (value is int)
        {
            buff.Write((int)value);
            return;
        }
        else if (value is uint)
        {
            buff.Write((uint)value);
            return;
        }
        else if (value is long)
        {
            buff.Write((long)value);
            return;
        }
        else if (value is ulong)
        {
            buff.Write((ulong)value);
            return;
        }
        else if (value is float)
        {
            buff.Write((float)value);
            return;
        }
        else if (value is double)
        {
            buff.Write((double)value);
            return;
        }
        else if (value is string)
        {
            buff.Write((string)value);
            return;
        }
        else if (value is bool)
        {
            buff.Write((bool)value);
            return;
        }
        else if (value is Vector3)
        {
            Vector3 v = (Vector3)value;
            buff.Write(ref v);
            return;
        }
        else if (value is char)
        {
            buff.Write((char)value);
            return;
        }
        else if (value.GetType().IsEnum)
        {
            buff.Write((int)value);
            return;
        }
        else if (value is List<int>)
        {
            List<int> v = (List<int>)value;

            buff.Write((byte)v.Count);
            for (int i = 0; i < v.Count; ++i)
                buff.Write((Int32)v[i]);

            return;
        }
        else if (value is List<short>)
        {
            List<short> v = (List<short>)value;

            buff.Write((byte)v.Count);
            for (int i = 0; i < v.Count; ++i)
                buff.Write((Int16)v[i]);

            return;
        }
        else if (value is List<float>)
        {
            List<float> v = (List<float>)value;

            buff.Write((byte)v.Count);
            for (int i = 0; i < v.Count; ++i)
                buff.Write((float)v[i]);

            return;
        }
        throw new ArgumentException("BitWriter cannot write type " + value.GetType().Name);
    }


}

