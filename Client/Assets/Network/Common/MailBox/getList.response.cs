using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.getList.response
{
    [Serializable]
    public class msg
    {
        public long nextPageKey;
        public Message[] messages;
        public int totalCount;
        public int maxCount;
    }

    [Serializable]
    public class Message
    {
        public string appId;
        public string senderId;
        public string receiverId;
        public Message1 message;
        public Item[] items;
        public bool existUnconfirmedItems;
    }

    [Serializable]
    public class Message1
    {
        public int deliverySeq;
        public string messageId;
        public string messageBoxId;
        public string senderAppId;
        public string senderId;
        public string receiverAppId;
        public string receiverId;
        public string title;
        public string body;
        public Titlemap titleMap;
        public Bodymap bodyMap;
        public Resourcemap resourceMap;
        public string state;
        public long regTime;
        public long modTime;
        public object readTime;
        public long expiredTime;
        public long expiryTime;
    }

    [Serializable]
    public class Titlemap
    {
    }

    [Serializable]
    public class Bodymap
    {
    }

    [Serializable]
    public class Resourcemap
    {
    }

    [Serializable]
    public class Item
    {
        public string itemId;
        public string itemCode;
        public string itemName;
        public int quantity;
        public string state;
        public int sentCount;
        public long regTime;
        public long modTime;
        public object sentTime;
        public object confirmedTime;
        public object expiredTime;
        public object expiryTime;
        public long validityTime;
    }

}
