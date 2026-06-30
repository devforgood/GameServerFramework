using FlatBuffers;
using UnityEngine;
using syncnet;

public static class PacketFactory
{
    public static byte[] CreateAddAgentMessage(int messageId, Vector3 pos, syncnet.GameObjectType gameObjectType = GameObjectType.Monster)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.AddAgent.StartAddAgent(builder);
        syncnet.AddAgent.AddPos(builder, syncnet.Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
        syncnet.AddAgent.AddGameObjectType(builder, gameObjectType);
        var offset = syncnet.AddAgent.EndAddAgent(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.AddAgent, offset.Value, messageId);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateRemoveAgentMessage(int agentId)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.RemoveAgent.StartRemoveAgent(builder);
        syncnet.RemoveAgent.AddAgentId(builder, agentId);
        var offset = syncnet.RemoveAgent.EndRemoveAgent(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.RemoveAgent, offset.Value);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateSetMoveTargetMessage(int agentId, Vector3 pos)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.SetMoveTarget.StartSetMoveTarget(builder);
        syncnet.SetMoveTarget.AddAgentId(builder, agentId);
        syncnet.SetMoveTarget.AddPos(builder, syncnet.Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
        var offset = syncnet.SetMoveTarget.EndSetMoveTarget(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.SetMoveTarget, offset.Value);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreatePingMessage(int seq)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.Ping.StartPing(builder);
        syncnet.Ping.AddSeq(builder, seq);
        var offset = syncnet.Ping.EndPing(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.Ping, offset.Value);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateSetRaycastMessage(Vector3 pos)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.SetRaycast.StartSetRaycast(builder);
        syncnet.SetRaycast.AddPos(builder, syncnet.Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
        var offset = syncnet.SetRaycast.EndSetRaycast(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.SetRaycast, offset.Value);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateLoginMessage(int messageId)
    {
        var builder = new FlatBufferBuilder(1024);
        var nameOffSet = builder.CreateString("test");
        var passwordOffSet = builder.CreateString("1234");
        syncnet.Login.StartLogin(builder);
        syncnet.Login.AddUserId(builder, nameOffSet);
        syncnet.Login.AddPassword(builder, passwordOffSet);
        var offset = syncnet.Login.EndLogin(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.Login, offset.Value, messageId);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateEnterGateMessage(int messageId, int mapId, int gateId)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.EnterGate.StartEnterGate(builder);
        syncnet.EnterGate.AddMapId(builder, mapId);
        syncnet.EnterGate.AddGateId(builder, gateId);
        var offset = syncnet.EnterGate.EndEnterGate(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.EnterGate, offset.Value, messageId);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }

    public static byte[] CreateUseSkillMessage(int skillId, int agentId, Vector3 pos, int type, long timestamp)
    {
        var builder = new FlatBufferBuilder(1024);
        syncnet.UseSkill.StartUseSkill(builder);
        syncnet.UseSkill.AddSkillId(builder, skillId);
        syncnet.UseSkill.AddId(builder, agentId);
        syncnet.UseSkill.AddPos(builder, syncnet.Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
        syncnet.UseSkill.AddTargetId(builder, type);
        syncnet.UseSkill.AddDuration(builder, 1);
        syncnet.UseSkill.AddTimestamp(builder, timestamp);
        var offset = syncnet.UseSkill.EndUseSkill(builder);
        var msg = syncnet.GameMessage.CreateGameMessage(builder, syncnet.GameMessages.UseSkill, offset.Value);
        builder.Finish(msg.Value);
        return builder.SizedByteArray();
    }
}
