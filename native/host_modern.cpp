#include <windows.h>
#include <d3d11.h>
#include <d3d12.h>
#include <dxgi1_4.h>
#include <GL/gl.h>
#include <stdio.h>
#include <string.h>
static bool running=true;
static LRESULT CALLBACK WindowProc(HWND h,UINT m,WPARAM w,LPARAM l){if(m==WM_CLOSE){running=false;return 0;}if(m==WM_DESTROY){running=false;return 0;}return DefWindowProcW(h,m,w,l);}
template<class T>static void Release(T*&p){if(p)p->Release();p=nullptr;}
int WINAPI WinMain(HINSTANCE instance,HINSTANCE,LPSTR args,int){
    WNDCLASSW wc={};wc.lpfnWndProc=WindowProc;wc.hInstance=instance;wc.lpszClassName=L"PiPModernTest";wc.style=CS_OWNDC;RegisterClassW(&wc);
    HWND window=CreateWindowW(wc.lpszClassName,L"PiP — validação gráfica isolada",WS_OVERLAPPEDWINDOW,80,80,960,600,nullptr,nullptr,instance,nullptr);
    bool gl=strstr(args,"opengl"),dx12=strstr(args,"d3d12");
    IDXGIFactory4* factory=nullptr;IDXGISwapChain* swap=nullptr;IDXGISwapChain3* chain=nullptr;
    ID3D11Device* d11=nullptr;ID3D11DeviceContext* ctx=nullptr;ID3D11RenderTargetView* target=nullptr;
    ID3D12Device* d12=nullptr;ID3D12CommandQueue* queue=nullptr;ID3D12CommandAllocator* allocator=nullptr;ID3D12GraphicsCommandList* list=nullptr;ID3D12DescriptorHeap* heap=nullptr;ID3D12Resource* buffers[2]={};ID3D12Fence* fence=nullptr;
    HANDLE event=nullptr;UINT64 fenceValue=0;UINT stride=0;
    HDC dc=nullptr;HGLRC rc=nullptr;int error=0;
    if(gl){
        dc=GetDC(window);PIXELFORMATDESCRIPTOR p={sizeof(p),1,PFD_DRAW_TO_WINDOW|PFD_SUPPORT_OPENGL|PFD_DOUBLEBUFFER,PFD_TYPE_RGBA,32};
        p.cAlphaBits=8;int format=ChoosePixelFormat(dc,&p);if(!format||!SetPixelFormat(dc,format,&p))return 10;
        rc=wglCreateContext(dc);wglMakeCurrent(dc,rc);
        typedef HGLRC(WINAPI* Create)(HDC,HGLRC,const int*);auto create=(Create)wglGetProcAddress("wglCreateContextAttribsARB");
        if(create){int a[]={0x2091,4,0x2092,3,0x9126,1,0};HGLRC modern=create(dc,0,a);if(modern){wglMakeCurrent(dc,modern);wglDeleteContext(rc);rc=modern;}}
    }else if(dx12){
        if(FAILED(CreateDXGIFactory1(IID_PPV_ARGS(&factory)))||FAILED(D3D12CreateDevice(nullptr,D3D_FEATURE_LEVEL_11_0,IID_PPV_ARGS(&d12))))return 20;
        D3D12_COMMAND_QUEUE_DESC q={};q.Type=D3D12_COMMAND_LIST_TYPE_DIRECT;if(FAILED(d12->CreateCommandQueue(&q,IID_PPV_ARGS(&queue))))return 21;
        DXGI_SWAP_CHAIN_DESC desc={};desc.BufferDesc.Width=960;desc.BufferDesc.Height=600;desc.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.BufferCount=2;desc.OutputWindow=window;desc.Windowed=TRUE;desc.SwapEffect=DXGI_SWAP_EFFECT_FLIP_DISCARD;
        if(FAILED(factory->CreateSwapChain(queue,&desc,&swap))||FAILED(swap->QueryInterface(IID_PPV_ARGS(&chain))))return 22;
        D3D12_DESCRIPTOR_HEAP_DESC hd={};hd.Type=D3D12_DESCRIPTOR_HEAP_TYPE_RTV;hd.NumDescriptors=2;
        if(FAILED(d12->CreateDescriptorHeap(&hd,IID_PPV_ARGS(&heap)))||FAILED(d12->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT,IID_PPV_ARGS(&allocator)))||FAILED(d12->CreateCommandList(0,D3D12_COMMAND_LIST_TYPE_DIRECT,allocator,nullptr,IID_PPV_ARGS(&list)))||FAILED(d12->CreateFence(0,D3D12_FENCE_FLAG_NONE,IID_PPV_ARGS(&fence))))return 23;
        list->Close();stride=d12->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);auto handle=heap->GetCPUDescriptorHandleForHeapStart();
        for(int i=0;i<2;i++){if(FAILED(chain->GetBuffer(i,IID_PPV_ARGS(&buffers[i]))))return 24;d12->CreateRenderTargetView(buffers[i],nullptr,handle);handle.ptr+=stride;}
        event=CreateEventW(nullptr,FALSE,FALSE,nullptr);
    }else{
        DXGI_SWAP_CHAIN_DESC desc={};desc.BufferDesc.Width=960;desc.BufferDesc.Height=600;desc.BufferDesc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;desc.SampleDesc.Count=1;desc.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;desc.BufferCount=2;desc.OutputWindow=window;desc.Windowed=TRUE;desc.SwapEffect=DXGI_SWAP_EFFECT_DISCARD;
        D3D_FEATURE_LEVEL feature;
        if(FAILED(D3D11CreateDeviceAndSwapChain(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&desc,&swap,&d11,&feature,&ctx)))return 30;
        ID3D11Texture2D* buffer=nullptr;if(FAILED(swap->GetBuffer(0,IID_PPV_ARGS(&buffer))))return 31;
        HRESULT hr=d11->CreateRenderTargetView(buffer,nullptr,&target);buffer->Release();if(FAILED(hr))return 32;
    }
    ShowWindow(window,SW_SHOWNOACTIVATE);DWORD start=GetTickCount();bool fullscreen=false;MSG msg={};
    while(running&&GetTickCount()-start<35000){
        while(PeekMessageW(&msg,nullptr,0,0,PM_REMOVE)){TranslateMessage(&msg);DispatchMessageW(&msg);}
        if(!running)break;
        if(swap&&!fullscreen&&strstr(args,"fullscreen")&&GetTickCount()-start>3000){fullscreen=SUCCEEDED(swap->SetFullscreenState(TRUE,nullptr));}
        float color[]={0.035f,0.06f,0.12f,1};
        if(gl){glClearColor(color[0],color[1],color[2],1);glClear(GL_COLOR_BUFFER_BIT);SwapBuffers(dc);Sleep(15);}
        else if(dx12){
            allocator->Reset();list->Reset(allocator,nullptr);UINT index=chain->GetCurrentBackBufferIndex();
            D3D12_RESOURCE_BARRIER barrier={};barrier.Type=D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;barrier.Transition.pResource=buffers[index];barrier.Transition.Subresource=D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;barrier.Transition.StateBefore=D3D12_RESOURCE_STATE_PRESENT;barrier.Transition.StateAfter=D3D12_RESOURCE_STATE_RENDER_TARGET;list->ResourceBarrier(1,&barrier);
            auto handle=heap->GetCPUDescriptorHandleForHeapStart();handle.ptr+=index*stride;list->ClearRenderTargetView(handle,color,0,nullptr);
            barrier.Transition.StateBefore=D3D12_RESOURCE_STATE_RENDER_TARGET;barrier.Transition.StateAfter=D3D12_RESOURCE_STATE_PRESENT;list->ResourceBarrier(1,&barrier);list->Close();ID3D12CommandList* commands[]={list};queue->ExecuteCommandLists(1,commands);if(FAILED(chain->Present(1,0))){error=40;break;}
            queue->Signal(fence,++fenceValue);if(fence->GetCompletedValue()<fenceValue){fence->SetEventOnCompletion(fenceValue,event);if(WaitForSingleObject(event,5000)!=WAIT_OBJECT_0){error=41;break;}}
        }else{ctx->ClearRenderTargetView(target,color);if(FAILED(swap->Present(1,0))){error=42;break;}}
    }
    if(swap&&fullscreen)swap->SetFullscreenState(FALSE,nullptr);
    if(gl){wglMakeCurrent(nullptr,nullptr);wglDeleteContext(rc);ReleaseDC(window,dc);}
    Release(target);Release(ctx);Release(d11);for(auto &b:buffers)Release(b);Release(list);Release(allocator);Release(heap);Release(fence);Release(chain);Release(swap);Release(queue);Release(d12);Release(factory);if(event)CloseHandle(event);DestroyWindow(window);return error;
}
