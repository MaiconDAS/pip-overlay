#pragma once
#include <windows.h>
#include <stdint.h>
struct Addresses {DWORD magic,machine,timestamp,imageSize,present,reset;DWORD resetTimestamp,resetImageSize;wchar_t resetModule[64];};
inline IMAGE_NT_HEADERS* Headers(HMODULE module){return (IMAGE_NT_HEADERS*)((char*)module+((IMAGE_DOS_HEADER*)module)->e_lfanew);}
inline void AddressPath(HMODULE module,wchar_t* path) {
    GetModuleFileNameW(module,path,32768);wchar_t* slash=wcsrchr(path,L'\\');
#ifdef _WIN64
    wcscpy(slash+1,L"PipD3D9-64.bin");
#else
    wcscpy(slash+1,L"PipD3D9-32.bin");
#endif
}
