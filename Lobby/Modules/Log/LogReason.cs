using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class LogReason
    {
        public string reason;
        public string sub_reason;
        public string paid;

        public LogReason(string _reason, string _sub_reason = "", string _paid = "n")
        {
            reason = _reason;
            sub_reason = _sub_reason;
            paid = _paid;
        }
    }
}
