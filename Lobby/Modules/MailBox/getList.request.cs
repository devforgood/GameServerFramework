using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.getList.request
{

    public class msg
    {
        public string messageBoxId { get; set; }
        public int count { get; set; }
        public long? nextPageKey { get; set; }
        public List<string> states { get; set; }
    }

}
