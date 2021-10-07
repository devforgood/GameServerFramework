using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Publish
    {
        public static async Task Send(ServerCommon.InternalMessage msg)
        {
            // 전체 참여자 목록 구성
            var pubMessage = JsonConvert.SerializeObject(msg);
            //Log.Information($"PubStartPlay {pubMessage}");

            // 배틀 체널에 참여자 명단을 알림
            await Cache.Instance.GetSubscriber().PublishAsync($"channel_msg:{msg.channel_id}", pubMessage);
        }
    }
}
