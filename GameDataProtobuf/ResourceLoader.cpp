// This file is auto-generated. Do not modify directly.
#include "ResourceLoader.h"
#include <fstream>
#include <iterator>
#include <limits>
#include <memory>
#include <type_traits>
#include <vector>
#include "gamedata.pb.h"
#include <google/protobuf/arena.h>
namespace {
template<typename TMessage, typename TMap, typename TGetter>
bool LoadProtobufFile(
    const char* filename,
    google::protobuf::Arena& arena,
    TMap& map,
    TGetter getter)
{
    std::ifstream input(filename, std::ios::binary);
    if (!input) {
        printf("[ERROR] %s file open failed.\n", filename);
        return false;
    }
    std::vector<char> buffer((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
    if (buffer.empty()) {
        printf("[ERROR] %s file is empty.\n", filename);
        return false;
    }
    if (buffer.size() > static_cast<size_t>(std::numeric_limits<int>::max())) {
        printf("[ERROR] %s file is too large to parse. (size=%zu)\n", filename, buffer.size());
        return false;
    }
    // Arena 위에 할당 — 소멸도 Arena::Reset()/소멸자에서 처리되므로 DLL 경계 힙 충돌 없음
    TMessage* message = google::protobuf::Arena::CreateMessage<TMessage>(&arena);
    if (!message->ParseFromArray(buffer.data(), static_cast<int>(buffer.size()))) {
        printf("[ERROR] %s parse failed. (size=%zu)\n", filename, buffer.size());
        return false;
    }
    for (const auto& entry : getter(*message)) {
        if (map.count(entry.id())) {
            printf("[WARN] %s duplicate id=%d, overwriting.\n", filename, entry.id());
        }
        map[entry.id()] = &entry;
    }
    return true;
}
}
ResourceLoader::~ResourceLoader()
{
    ClearResources();
}
void ResourceLoader::ClearResources()
{

    skills.clear();

    items.clear();

    quests.clear();

    game_modes.clear();

    maps.clear();

    // Arena reset으로 모든 protobuf 객체를 할당된 DLL 내에서 일괄 해제
    if (arena_) {
        arena_.reset();
    }
}
bool ResourceLoader::LoadResources(const std::string& basePath)
{
    auto tmp_arena = std::make_unique<google::protobuf::Arena>();

    std::unordered_map<long, const gamedata::Skill*> tmp_skills;

    std::unordered_map<long, const gamedata::Item*> tmp_items;

    std::unordered_map<long, const gamedata::Quest*> tmp_quests;

    std::unordered_map<long, const gamedata::GameMode*> tmp_game_modes;

    std::unordered_map<long, const gamedata::Map*> tmp_maps;



    std::string path_skills = basePath + "skill.bytes";
    if (!LoadProtobufFile<gamedata::SkillList>(
        path_skills.c_str(),
        *tmp_arena,
        tmp_skills,
        [](const gamedata::SkillList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Skill>& { return list.skills(); }))
        return false;

    std::string path_items = basePath + "item.bytes";
    if (!LoadProtobufFile<gamedata::ItemList>(
        path_items.c_str(),
        *tmp_arena,
        tmp_items,
        [](const gamedata::ItemList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Item>& { return list.items(); }))
        return false;

    std::string path_quests = basePath + "quest.bytes";
    if (!LoadProtobufFile<gamedata::QuestList>(
        path_quests.c_str(),
        *tmp_arena,
        tmp_quests,
        [](const gamedata::QuestList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Quest>& { return list.quests(); }))
        return false;

    std::string path_game_modes = basePath + "gamemode.bytes";
    if (!LoadProtobufFile<gamedata::GameModeList>(
        path_game_modes.c_str(),
        *tmp_arena,
        tmp_game_modes,
        [](const gamedata::GameModeList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::GameMode>& { return list.game_modes(); }))
        return false;

    std::string path_maps = basePath + "map.bytes";
    if (!LoadProtobufFile<gamedata::MapList>(
        path_maps.c_str(),
        *tmp_arena,
        tmp_maps,
        [](const gamedata::MapList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Map>& { return list.maps(); }))
        return false;


    // 모든 파일 로드 성공 후 한 번에 교체

    skills.clear();

    items.clear();

    quests.clear();

    game_modes.clear();

    maps.clear();

    arena_ = std::move(tmp_arena);

    skills = std::move(tmp_skills);

    items = std::move(tmp_items);

    quests = std::move(tmp_quests);

    game_modes = std::move(tmp_game_modes);

    maps = std::move(tmp_maps);

    return true;
}