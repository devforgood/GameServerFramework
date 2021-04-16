using System;

namespace ServerCommon
{
    public class ServerInfoRedisKey
    {
        public static readonly string server_info = "server_info";
        public static readonly string server_instance_id = "server_instance_id";
    }

    public struct ServerInfo
    {
        public string server_name;
        public string server_addr;
        public string server_id;
    }
}
