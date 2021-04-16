using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#else

public class Mathf
{
    public static float Round(float a)
    {
        return (float)Math.Round(a);
    }
}

#endif


namespace core
{
    public static class MathHelpers
    {
        public const float degToRad = 0.0174532924F;
        public const float Rad2Deg = 57.29578F;

        //private static System.Random rand = new System.Random();

        public static int GetRandomInt(int max)
        {
            return ThreadSafeRandom.Get().Next(max);
        }
        public static int GetRandomInt(int min, int max)
        {
            return ThreadSafeRandom.Get().Next(min, max);
        }
        public static float GetRandomFloat(double maximum = 1.0, double minimum = 0.0)
        {
            return (float)(ThreadSafeRandom.Get().NextDouble() * (maximum - minimum) + minimum);
        }

        public static Vector3 GetRandomVector(int minValue, int maxValue, float fix_y = 1.0f)
        {
            return new Vector3(ThreadSafeRandom.Get().Next(minValue, maxValue), fix_y, ThreadSafeRandom.Get().Next(minValue, maxValue));
        }

        private static Vector3[] direction = null;

        public static Vector3 DegreeToVector3Cached(int degree)
        {
            if (degree < 0 || degree >= 360)
                return default(Vector3);

            if(direction== null)
            {
                direction = new Vector3[360];
                for(int i=0;i<direction.Length;++i)
                {
                    direction[i] = DegreeToVector3((float)i);
                }
            }

            return direction[degree];
        }


        public static double DegreeToRadian(double angle)
        {
            return angle * degToRad;
        }
        public static Vector3 RadianToVector3(float radian)
        {
            return new Vector3((float)Math.Round(Math.Cos(radian)*1000000)/1000000, 0, (float)Math.Round(Math.Sin(radian)*1000000)/1000000);
        }
        public static Vector3 RadianToVector3(float radian, float length)
        {
            return RadianToVector3(radian) * length;
        }
        public static Vector3 DegreeToVector3(float degree)
        {
            return RadianToVector3((float)DegreeToRadian(degree));
        }
        public static Vector3 DegreeToVector3(float degree, float length)
        {
            return RadianToVector3((float)DegreeToRadian(degree)) * length;
        }

        public static bool circleRect(float cx, float cy, float radius, float rx, float ry, float rw, float rh)
        {

            float Dx = cx - Math.Max(rx, Math.Min(cx, rx + rw));
            float Dy = cy - Math.Max(ry, Math.Min(cy, ry + rh));

            return (Dx * Dx + Dy * Dy) < (radius * radius);
        }

        //Returns true if the circles are touching, or false if they are not
        public static bool circlesColliding(float x1, float y1, float radius1, float x2, float y2, float radius2)
        {
            //compare the distance to combined radii
            float dx = x2 - x1;
            float dy = y2 - y1;
            float radii = radius1 + radius2;
            if ((dx * dx) + (dy * dy) < radii * radii)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool Approximately(float a, float b, float delta = 0.0001f)
        {
            if (Math.Abs(a - b) < delta)
            {
                return false;
            }
            return true;
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static int countBits(int n)
        {
            //return (int)System.Math.Log((double)n, 2.0) + 1;
            int count = 0;
            while (n != 0)
            {
                count++;
                n >>= 1;
            }
            return count;
        }

        public static int MakeDWord(ushort a, ushort b)
        {
            return ((int)a << 16) | b;
        }


        /// <summary>
        /// 메달 획득 허용 수치 얻기
        /// </summary>
        /// <param name="medal_charge"></param>
        /// <param name="medal_charge_time"></param>
        /// <param name="current_time"></param>
        /// <returns></returns>
        public static (int, DateTime) GetMedalCharge(int medal_charge, DateTime? medal_charge_time, DateTime current_time)
        {
            if(medal_charge_time== null)
            {
                return ((int)MedalChargeConst.MaxCharge, current_time);
            }

            var diff = current_time- (DateTime)medal_charge_time;
            var count = (int)diff.TotalMinutes / (int)MedalChargeConst.ChargePeriod;
            // 충전 주기 보다 아래면 기존 값으로 리턴
            if(count <= 0)
            {
                return (medal_charge, (DateTime)medal_charge_time);
            }

            // 최대 충전 값이면
            if(medal_charge + count >= (int)MedalChargeConst.MaxCharge)
            {
                medal_charge = (int)MedalChargeConst.MaxCharge;
                medal_charge_time = current_time;
            }
            else
            {
                medal_charge += count;
                medal_charge_time = medal_charge_time?.AddMinutes(count * (int)MedalChargeConst.ChargePeriod);
            }

            return (medal_charge, (DateTime)medal_charge_time);
        }

        public static int weekDiff(DateTime d1, DateTime d2, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = d2.Subtract(d1);

            var weeks = (int)diff.Days / 7;

            // need to check if there's an extra week to count
            var remainingDays = diff.Days % 7;
            var cal = CultureInfo.InvariantCulture.Calendar;
            var d1WeekNo = cal.GetWeekOfYear(d1, CalendarWeekRule.FirstFullWeek, startOfWeek);
            var d1PlusRemainingWeekNo = cal.GetWeekOfYear(d1.AddDays(remainingDays), CalendarWeekRule.FirstFullWeek, startOfWeek);

            if (d1WeekNo != d1PlusRemainingWeekNo)
                weeks++;

            return weeks;
        }

        public static DateTime GetResetTime(string reset_time, DateTime current_time)
        {
            string[] timestring = reset_time.Split(':');


            System.DateTime resetTime = current_time.Date + new TimeSpan(int.Parse(timestring[0]), int.Parse(timestring[1]), int.Parse(timestring[2]));
            if (current_time >= resetTime)
            {
                resetTime = resetTime.AddDays(1);
            }
            return resetTime;
        }

        public static long ToEpochTime(this DateTime time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((time - epoch).TotalMilliseconds);
        }
        public static DateTime FromEpochTime(this long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

        public static uint Angle(this Vector2 v)
        {
            v.Normalize();
            double angle = Math.Atan2(v.y, v.x) * core.MathHelpers.Rad2Deg;
            if (angle < 0)
                angle = 360 + angle;

            if (angle == 360)
                angle = 0;
            return (uint)angle;
        }

        public static double WeekDifference(this DateTime lValue, DateTime rValue)
        {
            return Math.Abs((lValue - rValue).TotalDays) / 7;
        }

        public static double MonthDifference(this DateTime lValue, DateTime rValue)
        {
            return Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
        }
    }

    public static class Colors
    {
        public static readonly Vector3 Black = new Vector3(0.0f, 0.0f, 0.0f);
        public static readonly Vector3 White = new Vector3(1.0f, 1.0f, 1.0f);
        public static readonly Vector3 Red = new Vector3(1.0f, 0.0f, 0.0f);
        public static readonly Vector3 Green = new Vector3(0.0f, 1.0f, 0.0f);
        public static readonly Vector3 Blue = new Vector3(0.0f, 0.0f, 1.0f);
        public static readonly Vector3 LightYellow = new Vector3(1.0f, 1.0f, 0.88f);
        public static readonly Vector3 LightBlue = new Vector3(0.68f, 0.85f, 0.9f);
        public static readonly Vector3 LightPink = new Vector3(1.0f, 0.71f, 0.76f);
        public static readonly Vector3 LightGreen = new Vector3(0.56f, 0.93f, 0.56f);
    }
}
