#pragma once
#include <windows.h>
#include <stdint.h>
struct Dx11Hooks {uint32_t magic;volatile LONG state;uint64_t offsets[2];wchar_t modules[2][MAX_PATH];};
static constexpr uint32_t DX11_HOOK_MAGIC=0x32485050;
static inline void Dx11HookName(wchar_t* name,DWORD pid){wsprintfW(name,L"Local\\PipDx11Hooks2-%lu",pid);}
