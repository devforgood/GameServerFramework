#include "BTDebugSync.h"
#include "BTDebugManager.h"
#include "SendMessage.h"
#include "syncnet_generated.h"

#if defined(ENABLE_BT_DEBUG)

namespace
{
	syncnet::TreeNodeStatus ToTreeNodeStatus(BT::NodeStatus status)
	{
		switch (status)
		{
		case BT::NodeStatus::IDLE:
			return syncnet::TreeNodeStatus_Idle;
		case BT::NodeStatus::RUNNING:
			return syncnet::TreeNodeStatus_Running;
		case BT::NodeStatus::SUCCESS:
			return syncnet::TreeNodeStatus_Success;
		case BT::NodeStatus::FAILURE:
			return syncnet::TreeNodeStatus_Failure;
		case BT::NodeStatus::SKIPPED:
			return syncnet::TreeNodeStatus_Skipped;
		default:
			return syncnet::TreeNodeStatus_Unknown;
		}
	}

	syncnet::TreeNodeType ToTreeNodeType(BTDebugNodeType node_type)
	{
		switch (node_type)
		{
		case BTDebugNodeType::Control:
			return syncnet::TreeNodeType_Control;
		case BTDebugNodeType::Condition:
			return syncnet::TreeNodeType_Condition;
		case BTDebugNodeType::Action:
		default:
			return syncnet::TreeNodeType_Action;
		}
	}
}

std::shared_ptr<send_message> BTDebugSync::BuildMessage()
{
	auto snapshot = BTDebugManager::Instance().ConsumeSnapshot();
	if (snapshot.empty())
		return nullptr;

	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::TreeDebugDefinition>> definition_vector;
	definition_vector.reserve(snapshot.definitions.size());
	for (const auto& definition : snapshot.definitions)
	{
		std::vector<flatbuffers::Offset<syncnet::TreeDebugNodeDefinition>> node_vector;
		node_vector.reserve(definition.nodes.size());
		for (const auto& node : definition.nodes)
		{
			auto name = builder_ptr->CreateString(node.name);
			node_vector.push_back(syncnet::CreateTreeDebugNodeDefinition(
				*builder_ptr,
				node.node_id,
				node.parent_node_id,
				name,
				ToTreeNodeType(node.node_type)));
		}

		auto tree_id = builder_ptr->CreateString(definition.tree_id);
		auto nodes = builder_ptr->CreateVector(node_vector);
		definition_vector.push_back(syncnet::CreateTreeDebugDefinition(
			*builder_ptr,
			tree_id,
			definition.monster_id,
			nodes));
	}

	std::vector<flatbuffers::Offset<syncnet::TreeDebugRuntimeFrame>> frame_vector;
	frame_vector.reserve(snapshot.frames.size());
	for (const auto& frame : snapshot.frames)
	{
		std::vector<flatbuffers::Offset<syncnet::TreeDebugNodeChange>> change_vector;
		change_vector.reserve(frame.changes.size());
		for (const auto& change : frame.changes)
		{
			auto name = builder_ptr->CreateString(change.node_name);
			auto reason = builder_ptr->CreateString(change.reason);
			change_vector.push_back(syncnet::CreateTreeDebugNodeChange(
				*builder_ptr,
				change.node_id,
				name,
				ToTreeNodeStatus(change.status),
				reason,
				change.success_count,
				change.failure_count,
				change.running_count));
		}

		auto tree_id = builder_ptr->CreateString(frame.tree_id);
		auto executed_path = builder_ptr->CreateVector(frame.executed_path);
		auto changes = builder_ptr->CreateVector(change_vector);
		frame_vector.push_back(syncnet::CreateTreeDebugRuntimeFrame(
			*builder_ptr,
			tree_id,
			frame.monster_id,
			frame.tick,
			frame.ai_state,
			frame.target_actor_id,
			executed_path,
			changes));
	}

	auto definition_offsets = builder_ptr->CreateVector(definition_vector);
	auto frame_offsets = builder_ptr->CreateVector(frame_vector);
	auto tree_debug_sync = syncnet::CreateTreeDebugSync(*builder_ptr, definition_offsets, frame_offsets);
	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_TreeDebugSync,
		tree_debug_sync.Union());
	builder_ptr->Finish(send_msg);

	return builder_ptr;
}

#else

std::shared_ptr<send_message> BTDebugSync::BuildMessage()
{
	return nullptr;
}

#endif
