//using GameService;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GmTool
{


    public class MailBox
    {
        public static async Task<bool> SendMail(string receiverId, string title, string body, List<send.request.MailItem> items)
        {
            var msg = new send.request.msg();
            msg.receiverId = receiverId;
            msg.message = new send.request.Message();
            msg.message.messageBoxId = "inbox";
            msg.message.title = title;
            msg.message.body = body;
            msg.items = items.ToArray();

            string result = await WebAPIClient.Web.request(receiverId, "/message/send", JsonConvert.SerializeObject(msg));
            if(result == string.Empty)
            {
                return false;
            }
            return true;
        }
    }
}
