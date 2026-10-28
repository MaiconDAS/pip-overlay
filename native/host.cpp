#include <windows.h>
#include <d3d9.h>
#include <stdio.h>
static bool fullscreen=false;static int clicks=0;static bool running=true;
struct HostStats {volatile LONG frames,clicks,stateErrors,resets,sampleCount;float frameMs[4096];};
static LRESULT CALLBACK WindowProc(HWND h,UINT m,WPARAM w,LPARAM l){
    if(m==WM_DESTROY){running=false;PostQuitMessage(0);return 0;}
    if(m==WM_LBUTTONDOWN){clicks++;return 0;}
    if(m==WM_KEYDOWN&&w==VK_ESCAPE){DestroyWindow(h);return 0;}
    return DefWindowProcW(h,m,w,l);
}
int WINAPI WinMain(HINSTANCE instance,HINSTANCE,LPSTR command,int) {
    bool requestedFullscreen=strstr(command,"--fullscreen")!=nullptr;fullscreen=false;
    WNDCLASSW wc={};wc.lpfnWndProc=WindowProc;wc.hInstance=instance;wc.lpszClassName=L"PipD3D9Test";RegisterClassW(&wc);
    HWND window=CreateWindowW(wc.lpszClassName,L"PiP Direct3D9 — teste próprio (Esc fecha)",fullscreen?WS_POPUP:WS_OVERLAPPEDWINDOW,80,80,960,600,nullptr,nullptr,instance,nullptr);
    auto d3d=Direct3DCreate9(D3D_SDK_VERSION);if(!d3d)return 1;
    wchar_t mapName[80];wsprintfW(mapName,L"Local\\PipHost5-%lu",GetCurrentProcessId());HANDLE map=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(HostStats),mapName);auto stats=(HostStats*)MapViewOfFile(map,FILE_MAP_ALL_ACCESS,0,0,sizeof(HostStats));
    D3DDISPLAYMODE mode;d3d->GetAdapterDisplayMode(0,&mode);
    D3DPRESENT_PARAMETERS pp={};pp.hDeviceWindow=window;pp.Windowed=!fullscreen;pp.SwapEffect=D3DSWAPEFFECT_DISCARD;pp.PresentationInterval=D3DPRESENT_INTERVAL_ONE;
    pp.BackBufferFormat=fullscreen?mode.Format:D3DFMT_UNKNOWN;pp.BackBufferWidth=fullscreen?mode.Width:960;pp.BackBufferHeight=fullscreen?mode.Height:600;
    if(fullscreen)SetWindowPos(window,HWND_TOP,0,0,mode.Width,mode.Height,SWP_NOACTIVATE);
    IDirect3DDevice9* device=nullptr;HRESULT hr=d3d->CreateDevice(0,D3DDEVTYPE_HAL,window,D3DCREATE_HARDWARE_VERTEXPROCESSING,&pp,&device);
    if(FAILED(hr)){DestroyWindow(window);d3d->Release();return 2;}ShowWindow(window,SW_SHOW);
    LARGE_INTEGER previous,frequency;QueryPerformanceCounter(&previous);QueryPerformanceFrequency(&frequency);
    DWORD start=GetTickCount(),frames=0;MSG msg;
    while(running){
        while(PeekMessageW(&msg,nullptr,0,0,PM_REMOVE)){TranslateMessage(&msg);DispatchMessageW(&msg);}
        if(!running)break;
        DWORD t=GetTickCount()-start;
        if(requestedFullscreen&&!fullscreen&&t>3000){fullscreen=true;SetWindowLongW(window,GWL_STYLE,WS_POPUP|WS_VISIBLE);SetWindowPos(window,HWND_TOP,0,0,mode.Width,mode.Height,SWP_FRAMECHANGED);pp.Windowed=FALSE;pp.BackBufferFormat=mode.Format;pp.BackBufferWidth=mode.Width;pp.BackBufferHeight=mode.Height;if(SUCCEEDED(device->Reset(&pp))&&stats)stats->resets++;}
        device->Clear(0,nullptr,D3DCLEAR_TARGET,D3DCOLOR_XRGB(12+(clicks*37)%70,20,40+(t/40)%80),1,0);
        device->SetRenderState(D3DRS_CULLMODE,D3DCULL_CW);device->SetRenderState(D3DRS_ALPHABLENDENABLE,TRUE);
        device->BeginScene();device->EndScene();hr=device->Present(nullptr,nullptr,nullptr,nullptr);frames++;
        if(SUCCEEDED(hr)&&stats){LARGE_INTEGER now;QueryPerformanceCounter(&now);stats->frameMs[stats->sampleCount%4096]=float(1000.0*(now.QuadPart-previous.QuadPart)/frequency.QuadPart);InterlockedIncrement(&stats->sampleCount);previous=now;stats->frames++;stats->clicks=clicks;DWORD cull=0,blend=0;device->GetRenderState(D3DRS_CULLMODE,&cull);device->GetRenderState(D3DRS_ALPHABLENDENABLE,&blend);if(cull!=D3DCULL_CW||blend!=TRUE)stats->stateErrors++;}
        if(hr==D3DERR_DEVICELOST){Sleep(100);if(device->TestCooperativeLevel()==D3DERR_DEVICENOTRESET)device->Reset(&pp);}
        if(frames%60==0){wchar_t title[160];wsprintfW(title,L"PiP Direct3D9 — %lu frames · %d cliques · Esc fecha",frames,clicks);SetWindowTextW(window,title);}
        if(strstr(command,"--timed")&&t>40000)DestroyWindow(window);
    }
    device->Release();d3d->Release();if(stats)UnmapViewOfFile(stats);if(map)CloseHandle(map);return 0;
}
