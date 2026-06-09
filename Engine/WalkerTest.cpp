#include "Walker.h"


void TestWalker()
{
	Walker* walker = new Walker();

	const Vec2f objPos;
	const Vec2f expDir;
	Vec2f newDir;

	walker->solve(
		objPos, expDir,
		[](const Vec2i& pos) -> Walker::Blocking {
			return Walker::Blocking(false, 0);
		},
		newDir,
			5
			);

	delete walker;
}