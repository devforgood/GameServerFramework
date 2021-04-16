using GameService;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby
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

        public static async Task<string> GetMails(long member_no, long user_no, int count, long? nextPageKey = null, List<MailState> states = null)
        {
            int skip = 0;
            if(nextPageKey != null)
            {
                skip = (int)nextPageKey * count;
            }

            // 상태 지정이 없으면 기본값으로 보냄,읽음 상태만 처리하도록 한다.
            if(states == null)
            {
                states = new List<MailState>();
                states.Add(MailState.Send);
                states.Add(MailState.Read);
            }

            var mails = await MailQuery.GetMail(member_no, user_no, skip, count, states);
            if (mails.Count == 0)
                return string.Empty;

            return JsonConvert.SerializeObject(mails);
        }

        public static async Task<bool> MarkAsReadMails(string player_id, List<string> messageIds)
        {
            var msg = new claimItems.request.msg();
            msg.messageIds = messageIds.ToArray();

            string response = await WebAPIClient.Web.request(player_id, "/message/markAsRead", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                Log.Error($"GetMailItems {player_id} empty markAsRead");
                return false;
            }
            return true;
        }

        public static async Task<(bool, ItemList, Goods)> GetMailItems(Session session, List<string> items, int characterId)
        {
            ItemList reply_items = new ItemList();
            Goods reply_goods = new Goods();
            int changed_item_id = 0;
            if(items == null || items.Count == 0)
            {
                items = new List<string>();

                string characterPieceReward = ((int)GameItemId.CharacterPieceReward).ToString();
                getList.response.msg last_msg = null;
                long? nextPageKey = null;
                do
                {
                    var str = await GetMails(session.member_no, session.user_no, 100, nextPageKey);
                    if (str == string.Empty)
                        break;

                    last_msg = JsonConvert.DeserializeObject<getList.response.msg>(str);
                    if (last_msg == null)
                        break;

                    foreach (var mail in last_msg.messages)
                    {
                        // 우편에 아이템이 없는 경우 스킵 (시스템우편)
                        if (mail.items.Length == 0)
                            continue;

                        // 캐릭터 조각 선택 아이템 스킵
                        if (mail.items[0].itemId == characterPieceReward)
                            continue;

                        items.Add(mail.message.messageId);
                    }

                    nextPageKey = last_msg.nextPageKey;

                } while (last_msg.nextPageKey != -1);
            }
            else if (items.Count == 1 && characterId != 0)
            {
                var game_item_data = ACDC.GameItemData.Values.Where(x => x.Item_Type == (int)GameItemType.CharacterPiece && x.LinkId == characterId).FirstOrDefault();
                if(game_item_data == null || game_item_data == default)
                {
                    Log.Error($"GetMailItems {session.player_id} cannot find character : {characterId}");
                    return (false, reply_items, reply_goods);
                }


                var characters = await CharacterCache.Instance.GetEntities(session.member_no, session.user_no, true);
                if (characters.IsAvailable(game_item_data.id) == false)
                {
                    Log.Error($"GetMailItems {session.player_id} cannot receive character piece  : {characterId}");
                    return (false, reply_items, reply_goods);
                }

                changed_item_id = game_item_data.id;
            }

            if (items.Count == 0)
            {
                Log.Error($"GetMailItems {session.player_id} empty messages");
                return (false, reply_items, reply_goods);
            }

            var msg = new claimItems.request.msg();
            msg.messageIds = items.ToArray();

            string response = await WebAPIClient.Web.request(session.player_id, "/message/claimItems", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                Log.Error($"GetMailItems {session.player_id} empty claimItems");
                return (false, reply_items, reply_goods);
            }

            var mail_results = new Dictionary<int, int>();
            var success_msgs = new List<string>();
            var responseMsg = JsonConvert.DeserializeObject<claimItems.response.msg>(response);
            foreach(var result in responseMsg.results)
            {
                //  정상 메시지 체크
                if (result.status != 200)
                    continue;

                foreach(var item in result.items)
                {
                    var item_id = int.Parse(item.itemCode);
                    // 받을 아이템이 캐릭터 조각 보상이면 캐릭터 조각으로 변경
                    if(item_id == (int)GameItemId.CharacterPieceReward)
                    {
                        item_id = changed_item_id;
                    }

                    mail_results.Increment(item_id, (int)item.quantity);
                }

                success_msgs.Add(result.messageId);
            }

            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                {
                    foreach (var mail_result in mail_results)
                    {
                        await Inventory.Insert(session, user, character, mail_result.Key, mail_result.Value, new LogReason("A_MAIL"), reply_items);
                    }

                    reply_goods?.Set(user);
                }
            }


            // 정상적으로 처리된 메시지만 finish 처리한다.
            msg.messageIds = success_msgs.ToArray();
            response = await WebAPIClient.Web.request(session.player_id, "/message/finish", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                Log.Error($"GetMailItems {session.player_id} finish");
            }


            return (true, reply_items, reply_goods);
        }

    }
}
