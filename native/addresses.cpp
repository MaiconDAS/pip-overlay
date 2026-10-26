#include <d3d9.h>
#include <stdio.h>
#include "addresses.h"
int main(){
    HWND window=CreateWindowExW(0,L"STATIC",L"PiP Direct3D probe",WS_POPUP,0,0,8,8,nullptr,nullptr,GetModuleHandleW(nullptr),nullptr);
    IDirect3D9Ex* d3d=nullptr;Direct3DCreate9Ex(D3D_SDK_VERSION,&d3d);IDirect3DDevice9Ex* device=nullptr;
    D3DPRESENT_PARAMETERS pp={};pp.Windowed=TRUE;pp.SwapEffect=D3DSWAPEFFECT_DISCARD;pp.hDeviceWindow=window;
    // NULLREF discovers the runtime entry points without claiming the display/GPU.
    HRESULT hr=d3d?d3d->CreateDeviceEx(0,D3DDEVTYPE_NULLREF,window,D3DCREATE_SOFTWARE_VERTEXPROCESSING,&pp,nullptr,&device):E_FAIL;
    int result=1;
    if(SUCCEEDED(hr)) {
        auto module=GetModuleHandleW(L"d3d9.dll");auto headers=Headers(module);auto table=*(void***)device;
        uintptr_t present=(uintptr_t)table[17]-(uintptr_t)module,reset=(uintptr_t)table[16]-(uintptr_t)module;
        for(int index=16;index<=17;index++) {HMODULE owner=nullptr;wchar_t name[32768];GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)table[index],&owner);GetModuleFileNameW(owner,name,32768);wprintf(L"slot %d: %s + %llx\n",index,name,(unsigned long long)((uintptr_t)table[index]-(uintptr_t)owner));}
        HMODULE resetOwner=nullptr;wchar_t resetPath[32768];
        GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)table[16],&resetOwner);
        GetModuleFileNameW(resetOwner,resetPath,32768);auto resetHeaders=Headers(resetOwner);
        reset=(uintptr_t)table[16]-(uintptr_t)resetOwner;
        const wchar_t* resetName=wcsrchr(resetPath,L'\\');resetName=resetName?resetName+1:resetPath;
        if(present<headers->OptionalHeader.SizeOfImage&&reset<resetHeaders->OptionalHeader.SizeOfImage&&wcslen(resetName)<64) {
            Addresses a={0x36414450,headers->FileHeader.Machine,headers->FileHeader.TimeDateStamp,headers->OptionalHeader.SizeOfImage,(DWORD)present,(DWORD)reset,resetHeaders->FileHeader.TimeDateStamp,resetHeaders->OptionalHeader.SizeOfImage};
            wcscpy(a.resetModule,resetName);
            wchar_t path[32768];AddressPath(nullptr,path);HANDLE file=CreateFileW(path,GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
            DWORD written=0;if(file!=INVALID_HANDLE_VALUE){if(WriteFile(file,&a,sizeof(a),&written,nullptr)&&written==sizeof(a))result=0;CloseHandle(file);}
        }
    }else printf("CreateDevice HRESULT=%08lx\n",(unsigned long)hr);
    if(device)device->Release();if(d3d)d3d->Release();DestroyWindow(window);return result;
}
