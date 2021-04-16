using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public static partial class StringExtensions
    {
        public static bool IsLetterOrDigit(this string str)
        {
            for (int i = 0; i < str.Length; ++i)
            {
                if (Char.IsLetterOrDigit(str, i) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
