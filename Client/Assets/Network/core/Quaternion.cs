using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;

public static partial class QuaternionExtensions
{
    /// <summary>
    /// Transforms the vector using a quaternion.
    /// </summary>
    /// <param name="v">Vector to transform.</param>
    /// <param name="rotation">Rotation to apply to the vector.</param>
    /// <param name="result">Transformed vector.</param>
    public static void Transform(ref Vector3 v, Quaternion rotation, out Vector3 result)
    {
        //This operation is an optimized-down version of v' = q * v * q^-1.
        //The expanded form would be to treat v as an 'axis only' quaternion
        //and perform standard quaternion multiplication.  Assuming q is normalized,
        //q^-1 can be replaced by a conjugation.
        float x2 = rotation.x + rotation.x;
        float y2 = rotation.y + rotation.y;
        float z2 = rotation.z + rotation.z;
        float xx2 = rotation.x * x2;
        float xy2 = rotation.x * y2;
        float xz2 = rotation.x * z2;
        float yy2 = rotation.y * y2;
        float yz2 = rotation.y * z2;
        float zz2 = rotation.z * z2;
        float wx2 = rotation.w * x2;
        float wy2 = rotation.w * y2;
        float wz2 = rotation.w * z2;
        //Defer the component setting since they're used in computation.
        float transformedX = v.x * (1f - yy2 - zz2) + v.y * (xy2 - wz2) + v.z * (xz2 + wy2);
        float transformedY = v.x * (xy2 + wz2) + v.y * (1f - xx2 - zz2) + v.z * (yz2 - wx2);
        float transformedZ = v.x * (xz2 - wy2) + v.y * (yz2 + wx2) + v.z * (1f - xx2 - yy2);
        result.x = transformedX;
        result.y = transformedY;
        result.z = transformedZ;

    }

    public static Vector3 Transform(this Quaternion quat, Vector3 v)
    {
        Vector3 toReturn;
        Transform(ref v, quat, out toReturn);
        return toReturn;

    }
}

#else

using System.Xml.Serialization;
using Newtonsoft.Json;


namespace core
{
    public class Quaternion : IEquatable<Quaternion>
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public float w = 0;

        /// <summary>
        /// Constructs a new Quaternion.
        /// </summary>
        /// <param name="x">X component of the quaternion.</param>
        /// <param name="y">Y component of the quaternion.</param>
        /// <param name="z">Z component of the quaternion.</param>
        /// <param name="w">W component of the quaternion.</param>
        [JsonConstructor]
        public Quaternion(float x = 0f, float y = 0f, float z = 0f, float w = 0f)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary>
        /// Construct a new MyQuaternion from vector and w components
        /// </summary>
        /// <param name="v">The vector part</param>
        /// <param name="w">The w part</param>
        public Quaternion(Vector3 v, float w)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = w;
        }

        /// <summary>
        /// Quaternion representing the identity transform.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public static Quaternion Identity
        {
            get
            {
                return new Quaternion(0, 0, 0, 1);
            }
        }

        public static Quaternion LookRotation(Vector3 forward)
        {
            Vector3 up = Vector3.up;
            return Quaternion.LookRotation(ref forward, ref up);
        }

        private static Quaternion LookRotation(ref Vector3 forward, ref Vector3 up)
        {
            if (forward.x == 0 && forward.y == 0 && forward.z == 0)
                return Quaternion.Identity;

            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
            up = Vector3.Cross(forward, right);
            var m00 = right.x;
            var m01 = right.y;
            var m02 = right.z;
            var m10 = up.x;
            var m11 = up.y;
            var m12 = up.z;
            var m20 = forward.x;
            var m21 = forward.y;
            var m22 = forward.z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new Quaternion();
            if (num8 > 0f)
            {
                var num = (float)System.Math.Sqrt(num8 + 1f);
                quaternion.w = num * 0.5f;
                num = 0.5f / num;
                quaternion.x = (m12 - m21) * num;
                quaternion.y = (m20 - m02) * num;
                quaternion.z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = (float)System.Math.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.x = 0.5f * num7;
                quaternion.y = (m01 + m10) * num4;
                quaternion.z = (m02 + m20) * num4;
                quaternion.w = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = (float)System.Math.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.x = (m10 + m01) * num3;
                quaternion.y = 0.5f * num6;
                quaternion.z = (m21 + m12) * num3;
                quaternion.w = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = (float)System.Math.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.x = (m20 + m02) * num2;
            quaternion.y = (m21 + m12) * num2;
            quaternion.z = 0.5f * num5;
            quaternion.w = (m01 - m10) * num2;
            return quaternion;
        }

        /// <summary>
        /// Transforms the vector using a quaternion.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <param name="result">Transformed vector.</param>
        public static void Transform(ref Vector3 v, Quaternion rotation, out Vector3 result)
        {
            //This operation is an optimized-down version of v' = q * v * q^-1.
            //The expanded form would be to treat v as an 'axis only' quaternion
            //and perform standard quaternion multiplication.  Assuming q is normalized,
            //q^-1 can be replaced by a conjugation.
            float x2 = rotation.x + rotation.x;
            float y2 = rotation.y + rotation.y;
            float z2 = rotation.z + rotation.z;
            float xx2 = rotation.x * x2;
            float xy2 = rotation.x * y2;
            float xz2 = rotation.x * z2;
            float yy2 = rotation.y * y2;
            float yz2 = rotation.y * z2;
            float zz2 = rotation.z * z2;
            float wx2 = rotation.w * x2;
            float wy2 = rotation.w * y2;
            float wz2 = rotation.w * z2;
            //Defer the component setting since they're used in computation.
            float transformedX = v.x * (1f - yy2 - zz2) + v.y * (xy2 - wz2) + v.z * (xz2 + wy2);
            float transformedY = v.x * (xy2 + wz2) + v.y * (1f - xx2 - zz2) + v.z * (yz2 - wx2);
            float transformedZ = v.x * (xz2 - wy2) + v.y * (yz2 + wx2) + v.z * (1f - xx2 - yy2);
            result.x = transformedX;
            result.y = transformedY;
            result.z = transformedZ;

        }

        /// <summary>
        /// Transforms the vector using a quaternion.
        /// </summary>
        /// <param name="v">Vector to transform.</param>
        /// <param name="rotation">Rotation to apply to the vector.</param>
        /// <returns>Transformed vector.</returns>
        public Vector3 Transform(Vector3 v)
        {
            Vector3 toReturn;
            Transform(ref v, this, out toReturn);
            return toReturn;
        }


        public static Quaternion AngleAxis(float angle, Vector3 axis)
        {
            return Quaternion.AngleAxis(angle, ref axis);
        }
        private static Quaternion AngleAxis(float degress, ref Vector3 axis)
        {
            if (axis.sqrMagnitude == 0.0f)
                return Identity;

            Quaternion result = Identity;
            var radians = degress * MathHelpers.degToRad;
            radians *= 0.5f;
            axis.Normalize();
            axis = axis * (float)System.Math.Sin(radians);
            result.x = axis.x;
            result.y = axis.y;
            result.z = axis.z;
            result.w = (float)System.Math.Cos(radians);

            return Normalize(result);
        }

        /// <summary>
        /// Scale the given quaternion to unit length
        /// </summary>
        /// <param name="q">The quaternion to normalize</param>
        /// <returns>The normalized quaternion</returns>
        public static Quaternion Normalize(Quaternion q)
        {
            Quaternion result;
            Normalize(ref q, out result);
            return result;
        }
        /// <summary>
        /// Scale the given quaternion to unit length
        /// </summary>
        /// <param name="q">The quaternion to normalize</param>
        /// <param name="result">The normalized quaternion</param>
        public static void Normalize(ref Quaternion q, out Quaternion result)
        {
            float scale = 1.0f / q.Length;
            result = new Quaternion(q.xyz * scale, q.w * scale);
        }

        /// <summary>
        ///   <para>The dot product between two rotations.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static float Dot(Quaternion a, Quaternion b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
        }

        /// <summary>
        /// Gets the length (magnitude) of the quaternion.
        /// </summary>
        /// <seealso cref="LengthSquared"/>
        [XmlIgnore]
        [JsonIgnore]
        public float Length
        {
            get
            {
                return (float)System.Math.Sqrt(x * x + y * y + z * z + w * w);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public Vector3 xyz
        {
            set
            {
                x = value.x;
                y = value.y;
                z = value.z;
            }
            get
            {
                return new Vector3(x, y, z);
            }
        }


        public bool Equals(Quaternion other)
        {
            return x == other.x && y == other.y && z == other.z && w == other.w;
        }

        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2 ^ this.w.GetHashCode() >> 1;
        }
        public override bool Equals(object other)
        {
            if (!(other is Quaternion))
            {
                return false;
            }
            Quaternion quaternion = (Quaternion)other;
            return this.x.Equals(quaternion.x) && this.y.Equals(quaternion.y) && this.z.Equals(quaternion.z) && this.w.Equals(quaternion.w);
        }
        public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
        {
            return new Quaternion(lhs.w * rhs.x + lhs.x * rhs.w + lhs.y * rhs.z - lhs.z * rhs.y, lhs.w * rhs.y + lhs.y * rhs.w + lhs.z * rhs.x - lhs.x * rhs.z, lhs.w * rhs.z + lhs.z * rhs.w + lhs.x * rhs.y - lhs.y * rhs.x, lhs.w * rhs.w - lhs.x * rhs.x - lhs.y * rhs.y - lhs.z * rhs.z);
        }
        public static Vector3 operator *(Quaternion rotation, Vector3 point)
        {
            float num = rotation.x * 2f;
            float num2 = rotation.y * 2f;
            float num3 = rotation.z * 2f;
            float num4 = rotation.x * num;
            float num5 = rotation.y * num2;
            float num6 = rotation.z * num3;
            float num7 = rotation.x * num2;
            float num8 = rotation.x * num3;
            float num9 = rotation.y * num3;
            float num10 = rotation.w * num;
            float num11 = rotation.w * num2;
            float num12 = rotation.w * num3;
            Vector3 result;
            result.x = (1f - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
            result.y = (num7 + num12) * point.x + (1f - (num4 + num6)) * point.y + (num9 - num10) * point.z;
            result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1f - (num4 + num5)) * point.z;
            return result;
        }
        public static bool operator ==(Quaternion lhs, Quaternion rhs)
        {
            return Quaternion.Dot(lhs, rhs) > 0.999999f;
        }
        public static bool operator !=(Quaternion lhs, Quaternion rhs)
        {
            return Quaternion.Dot(lhs, rhs) <= 0.999999f;
        }

        /// <summary>
        /// Scales the MyQuaternion to unit length.
        /// </summary>
        public void Normalize()
        {
            float scale = 1.0f / this.Length;
            xyz *= scale;
            w *= scale;
        }


        public static void ToAxisAngleRad(Quaternion q, out Vector3 axis, out float angle)
        {
            if (System.Math.Abs(q.w) > 1.0f)
                q.Normalize();
            angle = 2.0f * (float)System.Math.Acos(q.w); // angle
            float den = (float)System.Math.Sqrt(1.0 - q.w * q.w);
            if (den > 0.0001f)
            {
                axis = q.xyz / den;
            }
            else
            {
                // This occurs when the angle is zero. 
                // Not a problem: just set an arbitrary normalized axis.
                axis = new Vector3(1, 0, 0);
            }
        }
        public void ToAngleAxis(out float angle, out Vector3 axis)
        {
            Quaternion.ToAxisAngleRad(this, out axis, out angle);
            angle *= MathHelpers.Rad2Deg;
        }
    }

}
#endif