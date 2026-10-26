#include <windows.h>
#include <tlhelp32.h>
#include <wchar.h>
#include <stdlib.h>
// Explicit per-process attachment; no service, driver, privilege escalation or persistence.
int wmain(int argc,wchar_t** argv) {
    if(argc!=2)return 1;
    DWORD pid=wcstoul(argv[1],nullptr,10);if(!pid)return 2;
    HANDLE process=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION|PROCESS_CREATE_THREAD|PROCESS_VM_OPERATION|PROCESS_VM_WRITE|PROCESS_VM_READ,FALSE,pid);
    if(!process)return 3;
    wchar_t executable[32768];DWORD length=32768;
    if(!QueryFullProcessImageNameW(process,0,executable,&length)){CloseHandle(process);return 4;}
    const wchar_t* name=wcsrchr(executable,L'\\');name=name?name+1:executable;
    if(_wcsicmp(name,L"DigimonMasters.exe")&&_wcsicmp(name,L"PipTest32.exe")&&_wcsicmp(name,L"PipTest64.exe")){CloseHandle(process);return 5;}
    wchar_t path[32768];GetModuleFileNameW(nullptr,path,32768);wchar_t* slash=wcsrchr(path,L'\\');if(!slash){CloseHandle(process);return 6;}
#ifdef _WIN64
    wcscpy(slash+1,L"PipRender64.dll");
#else
    wcscpy(slash+1,L"PipRender32.dll");
#endif
    if(GetFileAttributesW(path)==INVALID_FILE_ATTRIBUTES){CloseHandle(process);return 7;}
    HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPMODULE|TH32CS_SNAPMODULE32,pid);
    if(snapshot==INVALID_HANDLE_VALUE){CloseHandle(process);return 8;}
    MODULEENTRY32W module={};module.dwSize=sizeof(module);uintptr_t remoteKernel=0;bool loaded=false;
    if(Module32FirstW(snapshot,&module))do{
        if(!_wcsicmp(module.szModule,L"kernel32.dll"))remoteKernel=(uintptr_t)module.modBaseAddr;
        if(!_wcsicmp(module.szExePath,path))loaded=true;
    }while(Module32NextW(snapshot,&module));CloseHandle(snapshot);
    if(loaded){CloseHandle(process);return 0;}
    if(!remoteKernel){CloseHandle(process);return 9;}
    auto localKernel=GetModuleHandleW(L"kernel32.dll");auto load=GetProcAddress(localKernel,"LoadLibraryW");
    // Resolve the function's owning module rather than assuming kernel32 forwards nowhere.
    HMODULE owning=nullptr;GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,(LPCWSTR)load,&owning);
    if(owning!=localKernel){CloseHandle(process);return 10;}
    SIZE_T bytes=(wcslen(path)+1)*sizeof(wchar_t),written=0;
    void* remote=VirtualAllocEx(process,nullptr,bytes,MEM_COMMIT|MEM_RESERVE,PAGE_READWRITE);if(!remote){CloseHandle(process);return 11;}
    HANDLE thread=nullptr;int result=12;
    if(WriteProcessMemory(process,remote,path,bytes,&written)&&written==bytes){
        auto entry=(LPTHREAD_START_ROUTINE)(remoteKernel+(uintptr_t)load-(uintptr_t)localKernel);
        thread=CreateRemoteThread(process,nullptr,0,entry,remote,0,nullptr);
        if(thread){DWORD wait=WaitForSingleObject(thread,10000);if(wait==WAIT_OBJECT_0){DWORD code=0;GetExitCodeThread(thread,&code);result=code?0:13;}else result=14;}
    }
    // On timeout the remote argument must remain valid until the thread completes.
    if(result!=14)VirtualFreeEx(process,remote,0,MEM_RELEASE);
    if(thread)CloseHandle(thread);CloseHandle(process);return result;
}
