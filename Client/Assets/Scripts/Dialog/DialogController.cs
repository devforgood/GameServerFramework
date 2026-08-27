using syncnet;
using UnityEngine;

/// <summary>
/// NPC 대화의 클라이언트 쪽 절차. Session 이 조립하는 역할 객체 가운데 하나다
/// (ActorSync / SkillController / MapTransition 과 같은 층).
///
/// 대화 상태는 서버가 들고 있다. 여기서 하는 일은 세 가지뿐이다.
///   1. 상호작용을 보낸다(대화를 여는 메시지는 따로 없다 — NPC 를 누르는 동작 하나로 족하다)
///   2. 서버가 보낸 노드를 화면에 그린다
///   3. 고른 번호를 그대로 돌려보낸다
///
/// 조건(show_if)은 서버가 이미 걸러서 보내므로 클라는 조건을 알 필요가 없다. 다만
/// <b>보고 있던 nodeId 를 함께 보내야 한다</b> — 서버가 아는 것과 다르면 거절한다
/// (창을 두 번 누르면 지난 화면의 번호로 지금 노드의 동작이 실행되는 것을 막는다).
/// </summary>
public class DialogController
{
    private readonly ServerConnection connection;

    private int currentNodeId;
    private int currentNpcId;

    public DialogController(ServerConnection connection)
    {
        this.connection = connection;
    }

    public bool IsOpen => currentNodeId != 0;

    /// <summary>지금 대화 중인 NPC(대화가 닫혀 있으면 0).</summary>
    public int CurrentNpcId => currentNpcId;

    /// <summary>NPC/오브젝트에 말을 건다. 대화가 걸린 NPC 면 응답에 이어 노드가 온다.</summary>
    public void Interact(int targetId)
    {
        int messageId = connection.NextMessageId();
        connection.Send(PacketFactory.CreateInteractMessage(messageId, targetId), response =>
        {
            if (response.Result != StatusCode.Success)
            {
                // 거리/맵이 어긋났다. 대화도 열리지 않으므로 여기서 끝이다.
                Debug.Log($"Interact rejected (target {targetId}, result {response.Result}).");
            }
        });
    }

    /// <summary>서버가 보낸 대화 노드를 반영한다.</summary>
    public void Apply(GameMessage message, DialogNode node)
    {
        // nodeId 0 은 "대화가 끝났다"는 뜻이고 나머지 필드는 비어 있다.
        if (node.NodeId == 0)
        {
            Close();
            return;
        }

        currentNodeId = node.NodeId;
        currentNpcId = node.NpcId;

        var resource = GameManager.Instance != null ? GameManager.Instance.resource : null;

        var labels = new string[node.ChoicesLength];
        for (int i = 0; i < node.ChoicesLength; ++i)
        {
            var choice = node.Choices(i);
            string textId = choice.HasValue ? choice.Value.TextId : null;
            labels[i] = resource != null ? resource.GetText(textId) : textId;
        }

        string speaker = SpeakerName(resource, node.NpcId);
        string body = resource != null ? resource.GetText(node.TextId) : node.TextId;

        // 동작이 실패하면(레벨 미달 등) 서버가 같은 노드를 실패 상태로 다시 보낸다.
        // 창을 닫지 않는 이유는 왜 안 됐는지 보여줄 자리를 남기기 위해서다.
        if (message.Result != StatusCode.Success)
            Debug.Log($"Dialog action failed (node {node.NodeId}).");

        DialogWindow.Instance.Show(speaker, body, labels, Select);
    }

    /// <summary>
    /// 고른 번호를 되돌려 보낸다. index 가 음수면 "창을 닫았다"는 뜻이다.
    /// 번호는 <b>받은 목록에서의 번호</b>이며, 데이터의 번호로 되짚는 것은 서버가 한다.
    /// </summary>
    public void Select(int index)
    {
        if (currentNodeId == 0)
            return;

        int nodeId = currentNodeId;
        if (index < 0)
        {
            // 닫기는 응답을 기다리지 않고 화면을 먼저 정리한다(서버도 곧 빈 노드를 보낸다).
            Close();
        }

        connection.Send(PacketFactory.CreateDialogSelectMessage(connection.NextMessageId(), nodeId, index));
    }

    /// <summary>화면과 상태를 정리한다(서버가 끝냈다고 알렸거나, 맵이 바뀌었을 때).</summary>
    public void Close()
    {
        currentNodeId = 0;
        currentNpcId = 0;

        // 창을 만든 적이 없으면 만들지 않는다(종료 중에 오브젝트를 새로 만들지 않기 위해).
        if (DialogWindow.Existing != null)
            DialogWindow.Existing.Hide();
    }

    private static string SpeakerName(Assets.Scripts.GameData.ResourceLoader resource, int npcId)
    {
        if (resource == null)
            return string.Empty;

        Gamedata.Npc npc;
        if (!resource.Npcs.TryGetValue(npcId, out npc) || npc == null)
            return string.Empty;

        // 로컬라이즈 키가 없으면 데이터의 영문 이름이라도 보여 준다(빈칸보다 낫다).
        string localized = resource.GetText(npc.name_id);
        return localized == npc.name_id ? npc.name : localized;
    }
}
