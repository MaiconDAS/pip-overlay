#pragma once
#include <windows.h>
#include <stdint.h>
// Separate, versioned control channel: the existing CPU transport ABI stays intact.
struct GpuSlot { uint64_t handle;uint32_t sequence,reserved; };
struct GpuChannel {
    uint32_t magic, version;
    volatile LONG lock, supported, rejected, active;
    uint32_t generation, width, height, reserved;
    GpuSlot slots[3];
};
static_assert(sizeof(GpuChannel)==88,"GPU transport ABI");
inline void GpuName(wchar_t* name,DWORD pid){wsprintfW(name,L"Local\\PipGpu2-%lu",pid);}
inline bool GpuLock(GpuChannel* p){return InterlockedCompareExchange(&p->lock,1,0)==0;}
inline void GpuUnlock(GpuChannel* p){InterlockedExchange(&p->lock,0);}
constexpr uint32_t GPU_MAGIC=0x31555047;
