#include "ResourceLoader.h"
#include <fstream>
#include <vector>
#include "gamedata.pb.h"


template<typename TMessage, typename TMap, typename TGetter>
bool LoadProtobufFile(const char* filename, TMap& map, TGetter getter) {
    std::ifstream input(filename, std::ios::binary);
    if (!input) {
        printf("[ERROR] %s 파일을 열 수 없습니다.\n", filename);
        return false;
    }
    std::vector<char> buffer((std::istreambuf_iterator<char>(input)), std::istreambuf_iterator<char>());
    if (buffer.empty()) {
        printf("[ERROR] %s 파일이 비어있습니다.\n", filename);
        return false;
    }
    TMessage message;
    if (!message.ParseFromArray(buffer.data(), static_cast<int>(buffer.size()))) {
        printf("[ERROR] %s 파싱 실패 (size=%zu)\n", filename, buffer.size());
        return false;
    }
    for (const auto& entry : getter(message)) {
        using EntryType = std::decay_t<decltype(entry)>;
        map[entry.id()] = new EntryType(entry); // 포인터로 동적 할당
    }
    return true;
}

bool ResourceLoader::LoadResources()
{
    items.clear();
    skills.clear();

    // item.bytes
    if (!LoadProtobufFile<gamedata::ItemList>(
        "GameData/item.bytes",
        items,
        [](const gamedata::ItemList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Item>&{ return list.items(); }))
        return false;

    // skill.bytes
    if (!LoadProtobufFile<gamedata::SkillList>(
        "GameData/skill.bytes",
        skills,
        [](const gamedata::SkillList& list) -> const ::google::protobuf::RepeatedPtrField<gamedata::Skill>&{ return list.skills(); }))
        return false;

    // items 맵 디버그 출력
    printf("[DEBUG] items map size: %zu\n", items.size());
    for (const auto& [id, item] : items) {
        printf("[DEBUG] Item id: %d, type: %s, name_id: %s, desc_id: %s, heal:%d\n",
            item->id(),
            item->type().c_str(),
            item->name_id().c_str(),
            item->desc_id().c_str(),
			item->heal()
        );
    }

    return true;
}