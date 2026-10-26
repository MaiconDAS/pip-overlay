#pragma once
#include <windows.h>
#include <stdint.h>
// Fixed-width ABI shared by the x86 renderer and x64 capture process.
enum { PIP_MAGIC=0x38504950, PIP_WIDTH=1920, PIP_HEIGHT=1080 };
struct PipMemory {
    uint32_t magic,version; volatile LONG lock,active;
    uint32_t width,height,sequence,heartbeat;
    float x,y,w,h;
    volatile LONG rendered,uploaded,error,ready;
    uint64_t renderTicks;
    unsigned char pixels[PIP_WIDTH*PIP_HEIGHT*4];
    volatile LONG validate,verified,mismatched;
    volatile LONG exclusive,renderWidth,renderHeight;
};
static_assert(offsetof(PipMemory,pixels)==72,"transport ABI");
inline void MappingName(wchar_t* name,DWORD pid) { wsprintfW(name,L"Local\\PipOverlay8-%lu",pid); }
inline bool TryLock(PipMemory* p) {return InterlockedCompareExchange(&p->lock,1,0)==0;}
inline void Unlock(PipMemory* p) {InterlockedExchange(&p->lock,0);}
