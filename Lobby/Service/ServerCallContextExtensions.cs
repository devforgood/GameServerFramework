using Grpc.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public static partial class ServerCallContextExtensions
    {
        public async static Task<Session> GetSession(this ServerCallContext context)
        {
            Metadata.Entry metadataEntry = context.RequestHeaders.FirstOrDefault(m => String.Equals(m.Key, "session_id", StringComparison.Ordinal));

            if (metadataEntry.Equals(default(Metadata.Entry)) || metadataEntry.Value == null)
            {
                Log.Information($"GetSession value is null");
                return null;
            }
            Log.Information($"SessionId value is {metadataEntry.Value}, url{context.Method}, peer{context.Peer}");

            var session = await Session.GetSession(metadataEntry.Value);
            if (session == null)
            {
                Log.Warning($"lost session, url{context.Method}, peer{context.Peer}, session_id{metadataEntry.Value}");
            }
            return session;
        }
    }
}
