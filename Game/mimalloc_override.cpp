// Routes all C++ operator new/delete in the game server through mimalloc.
//
// On MSVC the override must live in the executable's own object file (not in a
// static library), otherwise the linker never pulls it in. Including this header
// in exactly one translation unit of the Game executable replaces the global
// operator new/delete with mimalloc-backed versions. The mi_* implementation
// itself is compiled into Engine.lib (thirdparty/mimalloc/src/static.c) and is
// pulled in by the references below.
//
// Scope: this covers C++ new/delete (which includes std containers, make_shared,
// etc.) across the whole process. It does NOT redirect C malloc/free called
// directly by third-party libs (flatbuffers, luajit, mariadb, Detour). For that,
// the mimalloc-redirect.dll approach is required and is intentionally not used
// here to keep the change low-risk.

#include <mimalloc-new-delete.h>
