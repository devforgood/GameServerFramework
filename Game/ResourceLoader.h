#pragma once
#include <boost/noncopyable.hpp>
#include <unordered_map>

struct RItem;
struct RSkill;

class ResourceLoader : private boost::noncopyable
{
private:
	std::unordered_map<long, RItem*> items;
	std::unordered_map<long, RSkill*> skills;
	
public:
	static ResourceLoader& Instance() {
		static ResourceLoader instance;
		return instance;
	}

	bool LoadResources();

private:
	ResourceLoader() = default;
};

