#pragma once
#include <sstream>
#include <string>
#include <vector>

namespace sql_script
{

// SQL 스크립트를 실행 가능한 문장 목록으로 자른다.
//
// 커넥션에 allowMultiQueries 를 켜지 않았으므로 ';' 로 이어 붙인 스크립트를 한 번에
// 넘기면 드라이버가 거부한다. 그래서 문장 단위로 잘라 하나씩 보낸다.
//
// 생성되는 SQL 은 CREATE TABLE / ALTER TABLE 과 '--' 한 줄 주석뿐이다. 문자열 리터럴이나
// 저장 프로시저(BEGIN...END) 가 없으므로 ';' 단순 분리로 충분하다. 그런 것이 생기면
// 이 분리기부터 손봐야 한다.
inline std::vector<std::string> Split(const std::string& script)
{
	std::vector<std::string> statements;
	std::string current;

	std::istringstream lines(script);
	std::string line;
	while (std::getline(lines, line))
	{
		const size_t comment = line.find("--");
		if (comment != std::string::npos)
			line.erase(comment);

		size_t start = 0;
		size_t semicolon;
		while ((semicolon = line.find(';', start)) != std::string::npos)
		{
			current.append(line, start, semicolon - start);
			statements.push_back(current);
			current.clear();
			start = semicolon + 1;
		}
		current.append(line, start, std::string::npos);
		current.push_back('\n');
	}
	statements.push_back(current);

	// 공백만 남은 조각은 버린다(주석만 있던 줄, 마지막 세미콜론 뒤 등).
	std::vector<std::string> result;
	for (const std::string& statement : statements)
	{
		const size_t first = statement.find_first_not_of(" \t\r\n");
		if (first == std::string::npos)
			continue;
		const size_t last = statement.find_last_not_of(" \t\r\n");
		result.push_back(statement.substr(first, last - first + 1));
	}
	return result;
}

} // namespace sql_script
