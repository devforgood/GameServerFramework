// This file is auto-generated. Do not modify directly.
#include "ResourceLoader.h"
#include <fstream>
#include <iterator>
#include <limits>
#include <memory>
#include <type_traits>
#include <vector>
#include "gamedata.pb.h"
namespace {
template<typename TMessage, typename TMap, typename TGetter>
bool LoadProtobufFile(
    const char* filename,
    std::unique_ptr<TMessage>& storage,
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
    auto message = std::make_unique<TMessage>();
    if (!message->ParseFromArray(buffer.data(), static_cast<int>(buffer.size()))) {
        printf("[ERROR] %s parse failed. (size=%zu)\n", filename, buffer.size());
        return false;
    }
    for (const auto& entry : getter(*message)) {
        if (map.count(entry.id())) {
            printf("[WARN] %s duplicate id=%d, overwriting.\n", filename, entry.id());
        }
        // new 복사 없이 RepeatedPtrField 내부 포인터를 직접 참조
        map[entry.id()] = &entry;
    }
    storage = std::move(message);
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
    skills_storage.reset();

    items.clear();
    items_storage.reset();

    quests.clear();
    quests_storage.reset();

    game_modes.clear();
    game_modes_storage.reset();

    maps.clear();
    maps_storage.reset();

}
bool ResourceLoader::LoadResources(const std::string& basePath)
{

    std::unique_ptr<gamedata::SkillList> tmp_storage_skills;
    std::unordered_map<long, const gamedata::Skill*> tmp_skills;

    std::unique_ptr<gamedata::ItemList> tmp_storage_items;
    std::unordered_map<long, const gamedata::Item*> tmp_items;

    std::unique_ptr<gamedata::QuestList> tmp_storage_quests;
    std::unordered_map<long, const gamedata::Quest*> tmp_quests;

    std::unique_ptr<gamedata::GameModeList> tmp_storage_game_modes;
    std::unordered_map<long, const gamedata::GameMode*> tmp_game_modes;

    std::unique_ptr<gamedata::MapList> tmp_storage_maps;
    std::unordered_map<long, const gamedata::Map*> tmp_maps;



    std::string path_skills = basePath + "skill.bytes";
    if (!LoadProtobufFile<gamedata::SkillList>(
        path_skills.c_str(),
        tmp_storage_skills,
        tmp_skills,
        [](const gamedata::SkillList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Skill>& { return list.skills(); }))
        return false;

    std::string path_items = basePath + "item.bytes";
    if (!LoadProtobufFile<gamedata::ItemList>(
        path_items.c_str(),
        tmp_storage_items,
        tmp_items,
        [](const gamedata::ItemList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Item>& { return list.items(); }))
        return false;

    std::string path_quests = basePath + "quest.bytes";
    if (!LoadProtobufFile<gamedata::QuestList>(
        path_quests.c_str(),
        tmp_storage_quests,
        tmp_quests,
        [](const gamedata::QuestList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Quest>& { return list.quests(); }))
        return false;

    std::string path_game_modes = basePath + "gamemode.bytes";
    if (!LoadProtobufFile<gamedata::GameModeList>(
        path_game_modes.c_str(),
        tmp_storage_game_modes,
        tmp_game_modes,
        [](const gamedata::GameModeList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::GameMode>& { return list.game_modes(); }))
        return false;

    std::string path_maps = basePath + "map.bytes";
    if (!LoadProtobufFile<gamedata::MapList>(
        path_maps.c_str(),
        tmp_storage_maps,
        tmp_maps,
        [](const gamedata::MapList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Map>& { return list.maps(); }))
        return false;


    // 모든 파일 로드 성공 후 한 번에 교체

    skills_storage = std::move(tmp_storage_skills);
    skills = std::move(tmp_skills);

    items_storage = std::move(tmp_storage_items);
    items = std::move(tmp_items);

    quests_storage = std::move(tmp_storage_quests);
    quests = std::move(tmp_quests);

    game_modes_storage = std::move(tmp_storage_game_modes);
    game_modes = std::move(tmp_game_modes);

    maps_storage = std::move(tmp_storage_maps);
    maps = std::move(tmp_maps);

    return true;
}