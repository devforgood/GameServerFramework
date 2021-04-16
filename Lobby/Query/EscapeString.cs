using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public static partial class StringExtensions
    {
        public static string GetEscapeString(this string str)
        {
            return MySql.Data.MySqlClient.MySqlHelper.EscapeString(str);
        }
    }
}