#include <windows.h>
#include <tlhelp32.h>
#include <wchar.h>
#include <stdlib.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include "dx11_probe.h"
// Probe in this helper, never by creating a second swapchain inside a fullscreen game.
static bool Probe(Dx11Hooks* info){
    HWND window=CreateWindowExW(WS_EX_NOACTIVATE|WS_EX_TOOLWINDOW,L"STATIC",L"PiP probe",WS_POPUP,0,0,32,32,nullptr,nullptr,GetModuleHandleW(nullptr),nullptr);
    DXGI_SWAP_CHAIN_DESC sd={};sd.BufferDesc.Width=sd.BufferDesc.Height=32;sd.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;sd.SampleDesc.Count=1;sd.BufferCount=2;sd.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;sd.OutputWindow=window;sd.Windowed=TRUE;
    IDXGISwapChain* chain=nullptr;ID3D11Device* device=nullptr;
    HRESULT hr=D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&sd,&chain,&device,nullptr,nullptr);
    bool good=SUCCEEDED(hr);IDXGISwapChain1* next=nullptr;void* methods[2]={};
    if(good){methods[0]=(*(void***)chain)[8];if(SUCCEEDED(chain->QueryInterface(__uuidof(IDXGISwapChain1),(void**)&next)))methods[1]=(*(void***)next)[22];}
    for(int i=0;i<2&&good;i++)if(methods[i]){
        HMODULE module=nullptr;good=GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)methods[i],&module)!=0;
        wchar_t path[32768];if(good){good=GetModuleFileNameW(module,path,32768)>0;wchar_t* slash=wcsrchr(path,L'\\');const wchar_t* name=slash?slash+1:path;if(wcslen(name)>=MAX_PATH)good=false;else{wcscpy(info->modules[i],name);info->offsets[i]=(uintptr_t)methods[i]-(uintptr_t)module;}}
    }
    if(next)next->Release();if(chain)chain->Release();if(device)device->Release();DestroyWindow(window);return good;
}
// Explicit per-process attachment; no service, driver, privilege escalation or persistence.
int wmain(int argc,wchar_t** argv) {
    if(argc!=2&&argc!=3)return 1;
    bool dx11=argc==3&&!wcscmp(argv[2],L"dx11");if(argc==3&&!dx11)return 1;
    DWORD pid=wcstoul(argv[1],nullptr,10);if(!pid)return 2;
    HANDLE process=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION|PROCESS_CREATE_THREAD|PROCESS_VM_OPERATION|PROCESS_VM_WRITE|PROCESS_VM_READ,FALSE,pid);
    if(!process)return 3;
    wchar_t executable[32768];DWORD length=32768;
    if(!QueryFullProcessImageNameW(process,0,executable,&length)){CloseHandle(process);return 4;}
    wchar_t path[32768];GetModuleFileNameW(nullptr,path,32768);wchar_t* slash=wcsrchr(path,L'\\');if(!slash){CloseHandle(process);return 6;}
#ifdef _WIN64
    wcscpy(slash+1,dx11?L"PipDx1164.dll":L"PipRender64.dll");
#else
    wcscpy(slash+1,dx11?L"PipDx1132.dll":L"PipRender32.dll");
#endif
    if(GetFileAttributesW(path)==INVALID_FILE_ATTRIBUTES){CloseHandle(process);return 7;}
    HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPMODULE|TH32CS_SNAPMODULE32,pid);
    if(snapshot==INVALID_HANDLE_VALUE){CloseHandle(process);return 8;}
    MODULEENTRY32W module={};module.dwSize=sizeof(module);uintptr_t remoteKernel=0;bool loaded=false,hasD3D9=false;
    if(Module32FirstW(snapshot,&module))do{
        if(!_wcsicmp(module.szModule,L"kernel32.dll"))remoteKernel=(uintptr_t)module.modBaseAddr;
        if(!_wcsicmp(module.szExePath,path))loaded=true;
        if(!_wcsicmp(module.szModule,dx11?L"d3d11.dll":L"d3d9.dll"))hasD3D9=true;
    }while(Module32NextW(snapshot,&module));CloseHandle(snapshot);
    if(loaded){CloseHandle(process);return 0;}
    // Attach only the explicitly selected, bundled renderer.
    if(!hasD3D9){CloseHandle(process);return 5;}
    if(!remoteKernel){CloseHandle(process);return 9;}
    auto localKernel=GetModuleHandleW(L"kernel32.dll");auto load=GetProcAddress(localKernel,"LoadLibraryW");
    // Resolve the function's owning module rather than assuming kernel32 forwards nowhere.
    HMODULE owning=nullptr;GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)load,&owning);
    if(owning!=localKernel){CloseHandle(process);return 10;}
    HANDLE hookMap=nullptr;Dx11Hooks* hooks=nullptr;
    if(dx11){
        wchar_t name[80];Dx11HookName(name,pid);hookMap=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(Dx11Hooks),name);
        if(hookMap)hooks=(Dx11Hooks*)MapViewOfFile(hookMap,FILE_MAP_ALL_ACCESS,0,0,sizeof(Dx11Hooks));
        if(!hooks){if(hookMap)CloseHandle(hookMap);CloseHandle(process);return 15;}
        memset(hooks,0,sizeof(*hooks));hooks->magic=DX11_HOOK_MAGIC;
        if(!Probe(hooks)){UnmapViewOfFile(hooks);CloseHandle(hookMap);CloseHandle(process);return 16;}
    }
    SIZE_T bytes=(wcslen(path)+1)*sizeof(wchar_t),written=0;
    void* remote=VirtualAllocEx(process,nullptr,bytes,MEM_COMMIT|MEM_RESERVE,PAGE_READWRITE);if(!remote){if(hooks)UnmapViewOfFile(hooks);if(hookMap)CloseHandle(hookMap);CloseHandle(process);return 11;}
    HANDLE thread=nullptr;int result=12;
    if(WriteProcessMemory(process,remote,path,bytes,&written)&&written==bytes){
        auto entry=(LPTHREAD_START_ROUTINE)(remoteKernel+(uintptr_t)load-(uintptr_t)localKernel);
        thread=CreateRemoteThread(process,nullptr,0,entry,remote,0,nullptr);
        if(thread){DWORD wait=WaitForSingleObject(thread,10000);if(wait==WAIT_OBJECT_0){DWORD code=0;GetExitCodeThread(thread,&code);result=code?0:13;}else result=14;}
    }
    // On timeout the remote argument must remain valid until the thread completes.
    if(result!=14)VirtualFreeEx(process,remote,0,MEM_RELEASE);
    if(result==0&&hooks){DWORD started=GetTickCount();while(hooks->state==0&&GetTickCount()-started<5000)Sleep(10);if(hooks->state!=1)result=17;}
    if(hooks)UnmapViewOfFile(hooks);if(hookMap)CloseHandle(hookMap);
    if(thread)CloseHandle(thread);CloseHandle(process);return result;
}
