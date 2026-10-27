#include <windows.h>
#include <stdio.h>
#include <string>
// Child-only startup. Environment is supplied by the editor, never written globally.
int wmain(int argc, wchar_t **argv) {
    if (argc < 4) return 1;
    std::wstring command = L"\"" + std::wstring(argv[1]) + L"\"";
    if(argc > 4) { command += L" "; command += argv[4]; }
    std::wstring dir = argv[1]; dir = dir.substr(0,dir.find_last_of(L"\\/"));
    STARTUPINFOW startup = {}; startup.cb = sizeof(startup);
    PROCESS_INFORMATION child = {};
    if (!CreateProcessW(argv[1],command.data(),nullptr,nullptr,FALSE,CREATE_SUSPENDED,nullptr,dir.c_str(),&startup,&child)) return 2;
    int result = 3;
    SIZE_T bytes = (wcslen(argv[2])+1)*sizeof(wchar_t), written = 0;
    void *remote = VirtualAllocEx(child.hProcess,nullptr,bytes,MEM_COMMIT|MEM_RESERVE,PAGE_READWRITE);
    if (remote && WriteProcessMemory(child.hProcess,remote,argv[2],bytes,&written) && written == bytes) {
        // Kernel32 has the same system-image mapping in same-bitness processes in this boot.
        auto load = (LPTHREAD_START_ROUTINE)GetProcAddress(GetModuleHandleW(L"kernel32.dll"),"LoadLibraryW");
        HANDLE thread = CreateRemoteThread(child.hProcess,nullptr,0,load,remote,0,nullptr);
        if (thread) {
            if (WaitForSingleObject(thread,20000)==WAIT_OBJECT_0) {
                DWORD loaded=0; GetExitCodeThread(thread,&loaded);
                if (loaded) {
                    HANDLE file=CreateFileW(argv[3],GENERIC_WRITE,0,nullptr,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
                    if(file!=INVALID_HANDLE_VALUE){char pid[24];int n=sprintf(pid,"%lu",child.dwProcessId);DWORD size;bool saved=WriteFile(file,pid,n,&size,nullptr)&&size==n;CloseHandle(file);if(saved&&ResumeThread(child.hThread)!=DWORD(-1))result=0;}
                }
            }
            CloseHandle(thread);
        }
    }
    // Only terminate our own newly-created suspended child if initialization failed.
    if(result) {TerminateProcess(child.hProcess,result);WaitForSingleObject(child.hProcess,5000);}
    else if(remote)VirtualFreeEx(child.hProcess,remote,0,MEM_RELEASE);
    CloseHandle(child.hThread);CloseHandle(child.hProcess);return result;
}
