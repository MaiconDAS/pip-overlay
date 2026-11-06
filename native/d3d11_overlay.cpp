#include <stddef.h>
#include "transport.h"
#include "gpu_transport.h"
#include <d3d11_1.h>
#include <dxgi1_2.h>
#include <d3dcompiler.h>
#ifdef PIP_DESKTOP
#include <dcomp.h>
#endif
#include <mutex>
#include <vector>
#ifndef PIP_DESKTOP
#include "MinHook.h"
#include "dx11_probe.h"
#endif

template<class T> static void Drop(T*& p){if(p)p->Release();p=nullptr;}
struct Renderer {
    HANDLE map=nullptr,gmap=nullptr;PipMemory* m=nullptr;GpuChannel* g=nullptr;
    ID3D11Device* device=nullptr;ID3D11DeviceContext1* context=nullptr;ID3DDeviceContextState* state=nullptr;
    ID3D11VertexShader* vs=nullptr;ID3D11PixelShader* ps=nullptr;ID3D11SamplerState* sampler=nullptr;ID3D11BlendState* blend=nullptr;
    ID3D11Texture2D* image=nullptr;ID3D11ShaderResourceView* view=nullptr;ID3D11Buffer* constants=nullptr;
    ID3D11Texture2D* shared[3]={};IDXGIKeyedMutex* mutex[3]={};UINT generation=0,sequence=0,width=0,height=0;bool rgba=false,valid=false;
    ID3D11RenderTargetView* desktopTarget=nullptr;ID3D11Texture2D* desktopBack=nullptr;
    void ClearDesktopTarget(){if(context&&(desktopTarget||desktopBack))context->OMSetRenderTargets(0,nullptr,nullptr);Drop(desktopTarget);Drop(desktopBack);presented=0;}
    UINT presented=0;DWORD opened=0,validated=0;std::vector<unsigned char> pixels;
    UINT layoutWidth=0,layoutHeight=0;float layout[4]={};
    void ClearShared(){for(int i=0;i<3;i++){Drop(mutex[i]);Drop(shared[i]);}generation=0;}
    void Clear(){if(context&&(desktopTarget||desktopBack))context->ClearState();ClearDesktopTarget();ClearShared();Drop(view);Drop(image);Drop(constants);Drop(blend);Drop(sampler);Drop(vs);Drop(ps);Drop(state);Drop(context);Drop(device);valid=false;sequence=presented=0;width=height=0;}
    void CloseMaps(){if(g)UnmapViewOfFile(g);if(gmap)CloseHandle(gmap);if(m)UnmapViewOfFile(m);if(map)CloseHandle(map);g=nullptr;m=nullptr;gmap=map=nullptr;layoutWidth=layoutHeight=0;std::vector<unsigned char>().swap(pixels);}
    ~Renderer(){Clear();CloseMaps();}
    bool Open(){
        if(!m&&GetTickCount()-opened>250){opened=GetTickCount();wchar_t name[80];MappingName(name,GetCurrentProcessId());map=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(map)m=(PipMemory*)MapViewOfFile(map,FILE_MAP_ALL_ACCESS,0,0,sizeof(PipMemory));if(!m&&map){CloseHandle(map);map=nullptr;}if(m&&(m->magic!=PIP_MAGIC||m->version!=1)){UnmapViewOfFile(m);CloseHandle(map);m=nullptr;map=nullptr;}}
        if(!m)return false;
        if(!g){wchar_t name[80];GpuName(name,GetCurrentProcessId());gmap=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(gmap)g=(GpuChannel*)MapViewOfFile(gmap,FILE_MAP_ALL_ACCESS,0,0,sizeof(GpuChannel));if(!g&&gmap){CloseHandle(gmap);gmap=nullptr;}if(g&&(g->magic!=GPU_MAGIC||g->version!=1)){UnmapViewOfFile(g);CloseHandle(gmap);g=nullptr;gmap=nullptr;}}
        return true;
    }
    HRESULT Init(IDXGISwapChain* chain){
        ID3D11Device* next=nullptr;HRESULT hr=chain->GetDevice(__uuidof(ID3D11Device),(void**)&next);if(FAILED(hr))return hr;
        if(device==next){next->Release();return S_OK;}Clear();device=next;
        ID3D11Device1* d=nullptr;hr=device->QueryInterface(__uuidof(ID3D11Device1),(void**)&d);if(FAILED(hr))return hr;
        d->GetImmediateContext1(&context);auto level=device->GetFeatureLevel();hr=d->CreateDeviceContextState(device->GetCreationFlags()&D3D11_CREATE_DEVICE_SINGLETHREADED?D3D11_1_CREATE_DEVICE_CONTEXT_STATE_SINGLETHREADED:0,&level,1,D3D11_SDK_VERSION,__uuidof(ID3D11Device),nullptr,&state);d->Release();if(FAILED(hr))return hr;
        const char* code="Texture2D image:register(t0);SamplerState smp:register(s0);cbuffer C:register(b0){float4 size;}struct V{float4 p:SV_Position;float2 uv:TEXCOORD0;};V VS(uint id:SV_VertexID){V o;o.uv=float2((id<<1)&2,id&2);o.p=float4(o.uv*float2(2,-2)+float2(-1,1),0,1);return o;}float4 PS(V v):SV_Target{float2 q=abs((v.uv-0.5)*size.xy)-(size.xy*0.5-min(6,min(size.x,size.y)*0.5));float a=saturate(0.5-(length(max(q,0))+min(max(q.x,q.y),0)-min(6,min(size.x,size.y)*0.5)));return float4(image.Sample(smp,v.uv).rgb,a);}";
        ID3DBlob *v=nullptr,*p=nullptr;hr=D3DCompile(code,strlen(code),nullptr,nullptr,nullptr,"VS","vs_4_0",0,0,&v,nullptr);if(SUCCEEDED(hr))hr=D3DCompile(code,strlen(code),nullptr,nullptr,nullptr,"PS","ps_4_0",0,0,&p,nullptr);
        if(SUCCEEDED(hr))hr=device->CreateVertexShader(v->GetBufferPointer(),v->GetBufferSize(),nullptr,&vs);if(SUCCEEDED(hr))hr=device->CreatePixelShader(p->GetBufferPointer(),p->GetBufferSize(),nullptr,&ps);Drop(v);Drop(p);if(FAILED(hr))return hr;
        D3D11_SAMPLER_DESC sd={};sd.Filter=D3D11_FILTER_MIN_MAG_MIP_LINEAR;sd.AddressU=sd.AddressV=sd.AddressW=D3D11_TEXTURE_ADDRESS_CLAMP;sd.MaxLOD=D3D11_FLOAT32_MAX;hr=device->CreateSamplerState(&sd,&sampler);if(FAILED(hr))return hr;
        D3D11_BLEND_DESC bd={};auto& b=bd.RenderTarget[0];b.BlendEnable=TRUE;b.SrcBlend=D3D11_BLEND_SRC_ALPHA;b.DestBlend=D3D11_BLEND_INV_SRC_ALPHA;b.BlendOp=D3D11_BLEND_OP_ADD;b.SrcBlendAlpha=D3D11_BLEND_ONE;b.DestBlendAlpha=D3D11_BLEND_INV_SRC_ALPHA;b.BlendOpAlpha=D3D11_BLEND_OP_ADD;b.RenderTargetWriteMask=D3D11_COLOR_WRITE_ENABLE_ALL;hr=device->CreateBlendState(&bd,&blend);if(FAILED(hr))return hr;
        D3D11_BUFFER_DESC cb={};cb.ByteWidth=16;cb.Usage=D3D11_USAGE_DEFAULT;cb.BindFlags=D3D11_BIND_CONSTANT_BUFFER;return device->CreateBuffer(&cb,nullptr,&constants);
    }
    bool Texture(UINT w,UINT h,bool gpu){
        if(image&&width==w&&height==h&&rgba==gpu)return true;Drop(view);Drop(image);valid=false;sequence=0;width=w;height=h;rgba=gpu;
        D3D11_TEXTURE2D_DESC td={};td.Width=w;td.Height=h;td.MipLevels=td.ArraySize=td.SampleDesc.Count=1;td.Format=gpu?DXGI_FORMAT_R8G8B8A8_UNORM:DXGI_FORMAT_B8G8R8A8_UNORM;td.BindFlags=D3D11_BIND_SHADER_RESOURCE;
        HRESULT hr=device->CreateTexture2D(&td,nullptr,&image);if(SUCCEEDED(hr))hr=device->CreateShaderResourceView(image,nullptr,&view);if(FAILED(hr)){m->error=hr;return false;}return true;
    }
    void Upload(UINT w,UINT h){
        if(g)g->supported=1;
        if(g&&g->active&&!g->rejected){
            if(!GpuLock(g))return;GpuSlot slots[3];memcpy(slots,g->slots,sizeof(slots));UINT gen=g->generation;bool sized=g->width==w&&g->height==h;GpuUnlock(g);if(!sized)return;
            if(generation!=gen){ClearShared();generation=gen;sequence=0;}
            for(int i=0;i<3;i++)if(!shared[i]&&slots[i].handle){HRESULT hr=device->OpenSharedResource((HANDLE)(uintptr_t)slots[i].handle,__uuidof(ID3D11Texture2D),(void**)&shared[i]);if(SUCCEEDED(hr))hr=shared[i]->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&mutex[i]);if(FAILED(hr)){g->rejected=1;ClearShared();return;}}
            if(!Texture(w,h,true))return;int newest=-1;for(int i=0;i<3;i++)if(mutex[i]&&slots[i].sequence>sequence&&(newest<0||slots[i].sequence>slots[newest].sequence))newest=i;
            if(newest>=0&&mutex[newest]->AcquireSync(1,0)==S_OK){
                if(g->generation==gen){context->CopyResource(image,shared[newest]);context->Flush();sequence=slots[newest].sequence;valid=true;InterlockedIncrement(&m->uploaded);}mutex[newest]->ReleaseSync(0);
            }
            for(int i=0;i<3;i++)if(mutex[i]&&g->slots[i].sequence<=sequence&&mutex[i]->AcquireSync(1,0)==S_OK)mutex[i]->ReleaseSync(g->slots[i].sequence<=sequence?0:1);
        }else{
            ClearShared();if(!TryLock(m))return;
            if(m->width!=w||m->height!=h){Unlock(m);return;}
            if(sequence==m->sequence&&valid&&!rgba){Unlock(m);return;}
            UINT seq=m->sequence;pixels.resize((size_t)w*h*4);memcpy(pixels.data(),m->pixels,pixels.size());Unlock(m);
            if(Texture(w,h,false)){context->UpdateSubresource(image,0,nullptr,pixels.data(),w*4,0);sequence=seq;valid=true;InterlockedIncrement(&m->uploaded);}
        }
    }
    // Explicit harness-only readback. Ordinary playback never enters this path.
    void Validate(ID3D11Texture2D* back,const D3D11_VIEWPORT& vp){
        if(!m->validate||GetTickCount()-validated<500)return;validated=GetTickCount();
        D3D11_TEXTURE2D_DESC desc;back->GetDesc(&desc);if(desc.SampleDesc.Count!=1)return;
        desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=desc.MiscFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
        ID3D11Texture2D* staging=nullptr;if(FAILED(device->CreateTexture2D(&desc,nullptr,&staging)))return;
        context->CopyResource(staging,back);D3D11_MAPPED_SUBRESOURCE read={};
        if(SUCCEEDED(context->Map(staging,0,D3D11_MAP_READ,0,&read))){
            UINT x=UINT(vp.TopLeftX+vp.Width*.5f),y=UINT(vp.TopLeftY+vp.Height*.5f);
            auto actual=(unsigned char*)read.pData+y*read.RowPitch+x*4;
            unsigned char expected[4];bool locked=TryLock(m);if(locked){memcpy(expected,m->pixels+((size_t)(m->height/2)*m->width+m->width/2)*4,4);Unlock(m);}
            if(locked){bool bgra=desc.Format==DXGI_FORMAT_B8G8R8A8_UNORM;int red=bgra?2:0,blue=bgra?0:2;
                bool match=abs(int(actual[red])-expected[2])<20&&abs(int(actual[1])-expected[1])<20&&abs(int(actual[blue])-expected[0])<20;
                if(m->validate==2){auto corner=(unsigned char*)read.pData+UINT(vp.TopLeftY)*read.RowPitch+UINT(vp.TopLeftX)*4;bool filled=abs(int(corner[red])-expected[2])<20&&abs(int(corner[1])-expected[1])<20&&abs(int(corner[blue])-expected[0])<20;match=match&&!filled;}
                InterlockedIncrement(match?&m->verified:&m->mismatched);
            }context->Unmap(staging,0);
        }Drop(staging);
    }
    bool Draw(IDXGISwapChain* chain,bool desktop=false){
        if(!Open())return false;
        if(!m->active||GetTickCount()-m->heartbeat>2000){Clear();return false;}
        HRESULT hr=Init(chain);if(FAILED(hr)){m->error=hr;Clear();return false;}
        m->ready=1;
        if(TryLock(m)){layoutWidth=m->width;layoutHeight=m->height;layout[0]=m->x;layout[1]=m->y;layout[2]=m->w;layout[3]=m->h;Unlock(m);}
        // Keep the last image visible when the producer briefly owns the layout lock.
        UINT w=layoutWidth,h=layoutHeight;float rect[4];memcpy(rect,layout,sizeof(rect));
        if(!w||!h||w>PIP_WIDTH||h>PIP_HEIGHT)return false;
        ID3DDeviceContextState* previous=nullptr;if(!desktop)context->SwapDeviceContextState(state,&previous);
        Upload(w,h);
        if(desktop&&(!valid||sequence==presented))return false;
        ID3D11Texture2D* back=nullptr;ID3D11RenderTargetView* target=nullptr;
        if(desktop&&valid){
            if(!desktopBack){hr=chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&desktopBack);if(SUCCEEDED(hr))hr=device->CreateRenderTargetView(desktopBack,nullptr,&desktopTarget);if(FAILED(hr)){m->error=hr;ClearDesktopTarget();return false;}}
            back=desktopBack;back->AddRef();
        }
        if(valid&&(back||SUCCEEDED(chain->GetBuffer(0,__uuidof(ID3D11Texture2D),(void**)&back)))){
            D3D11_TEXTURE2D_DESC bd;back->GetDesc(&bd);m->renderWidth=bd.Width;m->renderHeight=bd.Height;BOOL fullscreen=FALSE;chain->GetFullscreenState(&fullscreen,nullptr);m->exclusive=fullscreen;
            if(desktop){rect[0]=rect[1]=0;rect[2]=rect[3]=1;}
            if(desktop){target=desktopTarget;target->AddRef();const float clear[4]={};context->ClearRenderTargetView(target,clear);}
            if(rect[0]>=0&&rect[1]>=0&&rect[2]>0&&rect[3]>0&&rect[0]+rect[2]<=1.001f&&rect[1]+rect[3]<=1.001f&&(target||SUCCEEDED(device->CreateRenderTargetView(back,nullptr,&target)))){
                D3D11_VIEWPORT vp={rect[0]*bd.Width,rect[1]*bd.Height,rect[2]*bd.Width,rect[3]*bd.Height,0,1};float size[4]={vp.Width,vp.Height,0,0};context->UpdateSubresource(constants,0,nullptr,size,0,0);
                context->OMSetRenderTargets(1,&target,nullptr);context->OMSetBlendState(blend,nullptr,~0u);context->RSSetViewports(1,&vp);context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);context->VSSetShader(vs,nullptr,0);context->PSSetShader(ps,nullptr,0);context->PSSetShaderResources(0,1,&view);context->PSSetSamplers(0,1,&sampler);context->PSSetConstantBuffers(0,1,&constants);context->Draw(3,0);if(!desktop)InterlockedIncrement(&m->rendered);Validate(back,vp);
            }
        }
        // Remove backbuffer references before returning to the game, so ResizeBuffers works.
        bool drawn=target!=nullptr;if(drawn&&!desktop)presented=sequence;
        if(!desktop){context->ClearState();context->SwapDeviceContextState(previous,nullptr);Drop(previous);}Drop(target);Drop(back);return drawn;
    }
};
static Renderer renderer;static std::mutex gate;
#ifdef PIP_DESKTOP
static IDXGISwapChain* surface=nullptr;static HWND desktopWindow=nullptr;
static IDCompositionDevice* composition=nullptr;static IDCompositionTarget* compositionTarget=nullptr;static IDCompositionVisual* visual=nullptr;
extern "C" __declspec(dllexport) HRESULT PipDesktopOpen(HWND window){
    ID3D11Device* device=nullptr;IDXGIDevice* dxgi=nullptr;IDXGIAdapter* adapter=nullptr;IDXGIFactory2* factory=nullptr;IDXGISwapChain1* chain=nullptr;
    HRESULT hr=D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,D3D11_CREATE_DEVICE_BGRA_SUPPORT,nullptr,0,D3D11_SDK_VERSION,&device,nullptr,nullptr);
    if(SUCCEEDED(hr))hr=device->QueryInterface(__uuidof(IDXGIDevice),(void**)&dxgi);
    if(SUCCEEDED(hr))hr=dxgi->GetAdapter(&adapter);
    if(SUCCEEDED(hr))hr=adapter->GetParent(__uuidof(IDXGIFactory2),(void**)&factory);
    if(SUCCEEDED(hr)){
        RECT bounds;GetClientRect(window,&bounds);desktopWindow=window;
        DXGI_SWAP_CHAIN_DESC1 sd={};sd.Width=bounds.right;sd.Height=bounds.bottom;sd.AlphaMode=DXGI_ALPHA_MODE_PREMULTIPLIED;sd.Format=DXGI_FORMAT_B8G8R8A8_UNORM;sd.SampleDesc.Count=1;sd.BufferCount=2;sd.BufferUsage=DXGI_USAGE_RENDER_TARGET_OUTPUT;sd.SwapEffect=DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL;
        hr=factory->CreateSwapChainForComposition(device,&sd,nullptr,&chain);
        if(SUCCEEDED(hr))surface=chain;
        if(SUCCEEDED(hr))hr=DCompositionCreateDevice(dxgi,__uuidof(IDCompositionDevice),(void**)&composition);
        if(SUCCEEDED(hr))hr=composition->CreateTargetForHwnd(window,TRUE,&compositionTarget);
        if(SUCCEEDED(hr))hr=composition->CreateVisual(&visual);
        if(SUCCEEDED(hr))hr=visual->SetContent(surface);
        if(SUCCEEDED(hr))hr=compositionTarget->SetRoot(visual);
        if(SUCCEEDED(hr))hr=composition->Commit();
    }
    Drop(factory);Drop(adapter);Drop(dxgi);Drop(device);return hr;
}
static unsigned long long presentAttempts=0,presentAccepted=0,presentBusy=0;static HRESULT presentError=S_OK;
extern "C" __declspec(dllexport) int PipDesktopDraw(){
    if(!surface)return 0;
    if(renderer.Draw(surface,true)){
        presentAttempts++;HRESULT hr=surface->Present(0,DXGI_PRESENT_DO_NOT_WAIT);
        if(hr==DXGI_ERROR_WAS_STILL_DRAWING){presentBusy++;return 2;}
        if(hr==DXGI_STATUS_OCCLUDED)return 3;
        if(FAILED(hr)){presentError=hr;if(renderer.m)renderer.m->error=hr;return (int)hr;}
        renderer.presented=renderer.sequence;presentAccepted++;if(renderer.m)InterlockedIncrement(&renderer.m->rendered);return 1;
    }
    if(renderer.g&&renderer.g->active)for(auto& slot:renderer.g->slots)if(slot.sequence>renderer.presented)return 2;
    return 0;
}
extern "C" __declspec(dllexport) void PipDesktopStats(unsigned long long* values){values[0]=presentAttempts;values[1]=presentAccepted;values[2]=presentBusy;values[3]=(unsigned long)presentError;}
extern "C" __declspec(dllexport) void PipDesktopResize(){if(surface){renderer.ClearDesktopTarget();RECT rect;GetClientRect(desktopWindow,&rect);HRESULT hr=surface->ResizeBuffers(0,rect.right,rect.bottom,DXGI_FORMAT_UNKNOWN,0);if(FAILED(hr)&&renderer.m)renderer.m->error=hr;}}
extern "C" __declspec(dllexport) void PipDesktopClose(){renderer.Clear();renderer.CloseMaps();Drop(visual);Drop(compositionTarget);Drop(composition);Drop(surface);desktopWindow=nullptr;presentAttempts=presentAccepted=presentBusy=0;presentError=S_OK;}
#else
using Present=HRESULT(WINAPI*)(IDXGISwapChain*,UINT,UINT);using Present1=HRESULT(WINAPI*)(IDXGISwapChain1*,UINT,UINT,const DXGI_PRESENT_PARAMETERS*);
static Present original=nullptr;static Present1 original1=nullptr;static thread_local bool inside=false;
static void Before(IDXGISwapChain* s,UINT flags){if(flags&DXGI_PRESENT_TEST)return;std::unique_lock<std::mutex> lock(gate,std::try_to_lock);if(lock.owns_lock())renderer.Draw(s);}
static HRESULT WINAPI Hook(IDXGISwapChain* s,UINT sync,UINT flags){if(inside)return original(s,sync,flags);inside=true;Before(s,flags);HRESULT hr=original(s,sync,flags);inside=false;return hr;}
static HRESULT WINAPI Hook1(IDXGISwapChain1* s,UINT sync,UINT flags,const DXGI_PRESENT_PARAMETERS* p){if(inside)return original1(s,sync,flags,p);inside=true;Before(s,flags);HRESULT hr=original1(s,sync,flags,p);inside=false;return hr;}
static DWORD WINAPI Install(void*){
    wchar_t name[80];Dx11HookName(name,GetCurrentProcessId());HANDLE map=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(!map)return 0;
    auto hooks=(Dx11Hooks*)MapViewOfFile(map,FILE_MAP_ALL_ACCESS,0,0,sizeof(Dx11Hooks));
    if(!hooks){CloseHandle(map);return 0;}
    bool good=hooks->magic==DX11_HOOK_MAGIC&&MH_Initialize()==MH_OK;void* addresses[2]={};
    for(int i=0;i<2&&good;i++)if(hooks->modules[i][0]){
        hooks->modules[i][MAX_PATH-1]=0;HMODULE module=GetModuleHandleW(hooks->modules[i]);
        if(!module){good=false;break;}
        auto dos=(IMAGE_DOS_HEADER*)module;auto nt=(IMAGE_NT_HEADERS*)((unsigned char*)module+dos->e_lfanew);
        if(hooks->offsets[i]>=nt->OptionalHeader.SizeOfImage){good=false;break;}
        addresses[i]=(unsigned char*)module+hooks->offsets[i];
    }
    if(good&&addresses[0])good=MH_CreateHook(addresses[0],(void*)Hook,(void**)&original)==MH_OK&&MH_EnableHook(addresses[0])==MH_OK;else good=false;
    if(good&&addresses[1]&&addresses[1]!=addresses[0])good=MH_CreateHook(addresses[1],(void*)Hook1,(void**)&original1)==MH_OK&&MH_EnableHook(addresses[1])==MH_OK;
    if(!good){for(auto address:addresses)if(address){MH_DisableHook(address);MH_RemoveHook(address);}}
    InterlockedExchange(&hooks->state,good?1:-1);UnmapViewOfFile(hooks);CloseHandle(map);return 0;
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,LPVOID){if(reason==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(module);HANDLE t=CreateThread(nullptr,0,Install,nullptr,0,nullptr);if(t)CloseHandle(t);}return TRUE;}
#endif
