#include <stddef.h>
#include "transport.h"
#include "gpu_transport.h"
#include <d3d11.h>
#include <d3dcompiler.h>
#include <algorithm>

template<class T> static void Release(T*& p){if(p)p->Release();p=nullptr;}
struct Producer {
    HANDLE mapping=nullptr;PipMemory* memory=nullptr;
    HANDLE gpuMapping=nullptr;GpuChannel* gpu=nullptr;ID3D11Texture2D* shared[3]={};ID3D11RenderTargetView* sharedRtv[3]={};IDXGIKeyedMutex* sharedMutex[3]={};UINT sharedIndex=0;bool allowShared=true;DWORD lastGpu=0;
    ID3D11Device* device=nullptr;ID3D11DeviceContext* context=nullptr;
    ID3D11Texture2D *source=nullptr,*target=nullptr,*staging[3]={};
    ID3D11ShaderResourceView* srv=nullptr;ID3D11RenderTargetView* rtv=nullptr;
    ID3D11VertexShader* vs=nullptr;ID3D11PixelShader* ps=nullptr;ID3D11SamplerState* sampler=nullptr;
    UINT sw=0,sh=0,w=0,h=0,index=0,maxW=640,maxH=360;DXGI_FORMAT format=DXGI_FORMAT_UNKNOWN;bool pending[3]={};uint64_t order[3]={},serial=0,delivered=0;
    ~Producer(){ClearShared();if(gpu){gpu->active=0;UnmapViewOfFile(gpu);gpu=nullptr;}if(gpuMapping)CloseHandle(gpuMapping);if(memory){InterlockedExchange(&memory->active,0);UnmapViewOfFile(memory);}if(mapping)CloseHandle(mapping);ClearTextures();Release(sampler);Release(ps);Release(vs);Release(context);Release(device);}
    void ClearShared(){if(gpu){gpu->active=0;for(auto& slot:gpu->slots)slot.handle=0;}for(int i=0;i<3;i++){Release(sharedMutex[i]);Release(sharedRtv[i]);Release(shared[i]);}sharedIndex=0;}
    void ClearTextures(){ClearShared();Release(srv);Release(rtv);Release(source);Release(target);for(auto& p:staging)Release(p);for(auto& p:pending)p=false;index=0;}
    HRESULT Setup(ID3D11Texture2D* input) {
        if(!device){
            input->GetDevice(&device);device->GetImmediateContext(&context);
            const char* code="Texture2D image:register(t0);SamplerState smp:register(s0);struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};V vertex(uint id:SV_VertexID){V o;o.uv=float2((id<<1)&2,id&2);o.p=float4(o.uv*float2(2,-2)+float2(-1,1),0,1);return o;}float4 pixel(V i):SV_TARGET{return float4(image.Sample(smp,i.uv).rgb,1);}";
            ID3DBlob *v=nullptr,*p=nullptr,*error=nullptr;
            HRESULT hr=D3DCompile(code,strlen(code),nullptr,nullptr,nullptr,"vertex","vs_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&v,&error);Release(error);if(FAILED(hr))return hr;
            hr=D3DCompile(code,strlen(code),nullptr,nullptr,nullptr,"pixel","ps_4_0",D3DCOMPILE_OPTIMIZATION_LEVEL3,0,&p,&error);Release(error);if(FAILED(hr)){Release(v);return hr;}
            hr=device->CreateVertexShader(v->GetBufferPointer(),v->GetBufferSize(),nullptr,&vs);Release(v);
            if(SUCCEEDED(hr))hr=device->CreatePixelShader(p->GetBufferPointer(),p->GetBufferSize(),nullptr,&ps);Release(p);if(FAILED(hr))return hr;
            D3D11_SAMPLER_DESC sd={};sd.Filter=D3D11_FILTER_MIN_MAG_MIP_LINEAR;sd.AddressU=sd.AddressV=sd.AddressW=D3D11_TEXTURE_ADDRESS_CLAMP;sd.MaxLOD=D3D11_FLOAT32_MAX;
            hr=device->CreateSamplerState(&sd,&sampler);if(FAILED(hr))return hr;
        }
        D3D11_TEXTURE2D_DESC desc;input->GetDesc(&desc);
        double scale=std::min(1.,std::min((double)maxW/desc.Width,(double)maxH/desc.Height));
        UINT nextW=std::max(1u,(UINT)(desc.Width*scale)),nextH=std::max(1u,(UINT)(desc.Height*scale));
        if((staging[0]||(shared[0]&&allowShared&&gpu&&!gpu->rejected))&&sw==desc.Width&&sh==desc.Height&&format==desc.Format&&w==nextW&&h==nextH)return S_OK;
        ClearTextures();sw=desc.Width;sh=desc.Height;format=desc.Format;
        w=nextW;h=nextH;
        desc.MipLevels=1;desc.ArraySize=1;desc.SampleDesc.Count=1;desc.SampleDesc.Quality=0;desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;desc.CPUAccessFlags=0;desc.MiscFlags=0;
        HRESULT hr=S_OK;
        // Native BGRA frames go straight to staging: no redundant source/target allocations.
        if(w!=sw||h!=sh||format!=DXGI_FORMAT_B8G8R8A8_UNORM){
            hr=device->CreateTexture2D(&desc,nullptr,&source);if(FAILED(hr))return hr;
            hr=device->CreateShaderResourceView(source,nullptr,&srv);if(FAILED(hr))return hr;
            desc.Width=w;desc.Height=h;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.BindFlags=D3D11_BIND_RENDER_TARGET;
            if(!(allowShared&&gpu&&gpu->supported&&!gpu->rejected)){
                hr=device->CreateTexture2D(&desc,nullptr,&target);if(FAILED(hr))return hr;
                hr=device->CreateRenderTargetView(target,nullptr,&rtv);if(FAILED(hr))return hr;
            }
        }
        desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;
        if(!(allowShared&&gpu&&gpu->supported&&!gpu->rejected))for(auto& p:staging){hr=device->CreateTexture2D(&desc,nullptr,&p);if(FAILED(hr))return hr;}return S_OK;
    }
    HRESULT Poll() {
        if(gpu&&gpu->active&&!gpu->rejected)return S_OK;
        // Prefer the newest completed readback. Never wait for the GPU or the game.
        bool tried[3]={};
        for(int attempt=0;attempt<3;attempt++) {
            int read=-1;
            for(int n=0;n<3;n++)if(!tried[n]&&pending[n]&&order[n]>delivered&&(read<0||order[n]>order[read]))read=n;
            if(read<0)return S_OK;tried[read]=true;
            D3D11_MAPPED_SUBRESOURCE mapped={};
            HRESULT hr=context->Map(staging[read],0,D3D11_MAP_READ,D3D11_MAP_FLAG_DO_NOT_WAIT,&mapped);
            if(hr==DXGI_ERROR_WAS_STILL_DRAWING)continue;
            if(FAILED(hr))return hr;
            if(TryLock(memory)) {
                if(mapped.RowPitch==w*4)memcpy(memory->pixels,mapped.pData,(size_t)w*h*4);
                else for(UINT row=0;row<h;row++)memcpy(memory->pixels+row*w*4,(char*)mapped.pData+row*mapped.RowPitch,w*4);
                memory->width=w;memory->height=h;memory->sequence++;memory->heartbeat=GetTickCount();memory->active=1;Unlock(memory);
                delivered=order[read];
                for(int n=0;n<3;n++)if(order[n]<=delivered)pending[n]=false;
            }
            context->Unmap(staging[read],0);
            return S_OK;
        }
        return S_OK;
    }
    HRESULT SharedSubmit(ID3D11Texture2D* input) {
        if(!shared[0]){
            D3D11_TEXTURE2D_DESC desc={};input->GetDesc(&desc);desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;desc.SampleDesc.Quality=0;desc.Usage=D3D11_USAGE_DEFAULT;desc.CPUAccessFlags=0;desc.MiscFlags=0;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
            if(!source){HRESULT hr=device->CreateTexture2D(&desc,nullptr,&source);if(FAILED(hr))return hr;hr=device->CreateShaderResourceView(source,nullptr,&srv);if(FAILED(hr))return hr;}
            desc.Width=w;desc.Height=h;desc.Format=DXGI_FORMAT_R8G8B8A8_UNORM;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_RENDER_TARGET;desc.MiscFlags=D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;
            HANDLE handles[3]={};
            for(int i=0;i<3;i++){
                HRESULT hr=device->CreateTexture2D(&desc,nullptr,&shared[i]);if(FAILED(hr))return hr;
                hr=device->CreateRenderTargetView(shared[i],nullptr,&sharedRtv[i]);if(FAILED(hr))return hr;
                hr=shared[i]->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&sharedMutex[i]);if(FAILED(hr))return hr;
                IDXGIResource* resource=nullptr;hr=shared[i]->QueryInterface(__uuidof(IDXGIResource),(void**)&resource);if(FAILED(hr))return hr;
                hr=resource->GetSharedHandle(&handles[i]);resource->Release();if(FAILED(hr))return hr;
            }
            if(!GpuLock(gpu)){ClearShared();return S_FALSE;}
            for(int i=0;i<3;i++){gpu->slots[i].handle=(uint64_t)(uintptr_t)handles[i];gpu->slots[i].sequence=0;}
            gpu->generation++;gpu->width=w;gpu->height=h;GpuUnlock(gpu);lastGpu=GetTickCount();
        }
        int slot=-1;HRESULT hr=S_OK;
        for(int n=0;n<3;n++){int candidate=(sharedIndex+n)%3;hr=sharedMutex[candidate]->AcquireSync(0,0);if(hr==S_OK){slot=candidate;sharedIndex=(candidate+1)%3;break;}if(hr!=WAIT_TIMEOUT)return E_FAIL;}
        if(slot<0){if(DWORD(GetTickCount()-lastGpu)>1000)return E_FAIL;return S_FALSE;}
        if(source!=input)context->CopyResource(source,input);
        context->IASetInputLayout(nullptr);context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        context->VSSetShader(vs,nullptr,0);context->PSSetShader(ps,nullptr,0);context->PSSetShaderResources(0,1,&srv);context->PSSetSamplers(0,1,&sampler);
        context->OMSetRenderTargets(1,&sharedRtv[slot],nullptr);D3D11_VIEWPORT vp={0,0,(float)w,(float)h,0,1};context->RSSetViewports(1,&vp);context->Draw(3,0);
        context->OMSetRenderTargets(0,nullptr,nullptr);ID3D11ShaderResourceView* empty=nullptr;context->PSSetShaderResources(0,1,&empty);context->Flush();
        // Never wait for the game or hold the CPU transport lock across GPU work.
        if(!TryLock(memory)){sharedMutex[slot]->ReleaseSync(0);return S_FALSE;}
        for(auto& p:pending)p=false;for(auto& p:staging)Release(p);
        memory->width=w;memory->height=h;memory->sequence++;memory->heartbeat=GetTickCount();memory->active=1;
        gpu->slots[slot].sequence=memory->sequence;InterlockedExchange(&gpu->active,1);Unlock(memory);
        hr=sharedMutex[slot]->ReleaseSync(1);lastGpu=GetTickCount();return hr;
    }
    HRESULT Submit(ID3D11Texture2D* input) {
        HRESULT hr=Setup(input);if(FAILED(hr))return hr;
        if(allowShared&&gpu&&gpu->supported&&!gpu->rejected){hr=SharedSubmit(input);if(SUCCEEDED(hr))return hr;gpu->rejected=1;ClearShared();hr=Setup(input);if(FAILED(hr))return hr;}
        if(gpu)gpu->active=0;hr=Poll();if(FAILED(hr))return hr;
        if(pending[index]) {index=(index+1)%3;return S_FALSE;}
        // Native-size BGRA frames need no sampling pass or intermediate source copy.
        if(w==sw&&h==sh&&format==DXGI_FORMAT_B8G8R8A8_UNORM){
            context->CopyResource(staging[index],input);context->Flush();pending[index]=true;order[index]=++serial;index=(index+1)%3;return S_OK;
        }
        if(source!=input)context->CopyResource(source,input);
        context->IASetInputLayout(nullptr);context->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
        context->VSSetShader(vs,nullptr,0);context->PSSetShader(ps,nullptr,0);
        context->PSSetShaderResources(0,1,&srv);context->PSSetSamplers(0,1,&sampler);
        context->OMSetRenderTargets(1,&rtv,nullptr);
        D3D11_VIEWPORT vp={0,0,(float)w,(float)h,0,1};context->RSSetViewports(1,&vp);context->Draw(3,0);
        context->OMSetRenderTargets(0,nullptr,nullptr);ID3D11ShaderResourceView* empty=nullptr;context->PSSetShaderResources(0,1,&empty);
        context->CopyResource(staging[index],target);context->Flush();pending[index]=true;order[index]=++serial;index=(index+1)%3;return S_OK;
    }
};
extern "C" __declspec(dllexport) Producer* __cdecl PipOpen(DWORD pid) {
    auto p=new Producer();wchar_t name[80];MappingName(name,pid);
    p->mapping=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(PipMemory),name);
    bool fresh=GetLastError()!=ERROR_ALREADY_EXISTS;
    if(p->mapping)p->memory=(PipMemory*)MapViewOfFile(p->mapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(PipMemory));
    if(!p->memory){delete p;return nullptr;}
    if(fresh){memset(p->memory,0,sizeof(PipMemory));p->memory->magic=PIP_MAGIC;p->memory->version=1;}
    else if(p->memory->magic!=PIP_MAGIC||p->memory->version!=1){delete p;return nullptr;}
        p->memory->active=0;
    GpuName(name,pid);p->gpuMapping=CreateFileMappingW(INVALID_HANDLE_VALUE,nullptr,PAGE_READWRITE,0,sizeof(GpuChannel),name);
    bool newGpu=GetLastError()!=ERROR_ALREADY_EXISTS;
    if(p->gpuMapping)p->gpu=(GpuChannel*)MapViewOfFile(p->gpuMapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(GpuChannel));
    if(p->gpu){if(newGpu){memset(p->gpu,0,sizeof(GpuChannel));p->gpu->magic=GPU_MAGIC;p->gpu->version=1;}p->gpu->active=0;p->gpu->rejected=0;for(auto& slot:p->gpu->slots){slot.handle=0;slot.sequence=0;}}
    return p;
}
extern "C" __declspec(dllexport) HRESULT __cdecl PipPoll(Producer* p) {
    if(p->gpu&&p->gpu->active&&p->gpu->rejected&&p->source){auto frame=p->source;frame->AddRef();auto hr=p->Submit(frame);frame->Release();return hr;}
    return p->device?p->Poll():S_OK;
}
extern "C" __declspec(dllexport) HRESULT __cdecl PipSubmit(Producer* p,ID3D11Texture2D* texture) {return p->Submit(texture);}
extern "C" __declspec(dllexport) void __cdecl PipResolution(Producer* p,UINT width,UINT height) {p->maxW=std::max(1u,std::min((UINT)PIP_WIDTH,width));p->maxH=std::max(1u,std::min((UINT)PIP_HEIGHT,height));}
extern "C" __declspec(dllexport) void __cdecl PipFrameSize(Producer* p,uint32_t* size) {size[0]=p->memory->width;size[1]=p->memory->height;}
extern "C" __declspec(dllexport) void __cdecl PipLayout(Producer* p,float x,float y,float w,float h) {
    p->memory->heartbeat=GetTickCount(); // Liveness is independent of whether a paused video produces frames.
    if(TryLock(p->memory)){p->memory->x=x;p->memory->y=y;p->memory->w=w;p->memory->h=h;Unlock(p->memory);}
}
extern "C" __declspec(dllexport) void __cdecl PipStats(Producer* p,uint32_t* stats) {
    stats[0]=p->memory->sequence;stats[1]=p->memory->rendered;stats[2]=p->memory->uploaded;stats[3]=p->memory->error;stats[4]=p->memory->ready;
    stats[5]=p->memory->exclusive;stats[6]=p->memory->renderWidth;stats[7]=p->memory->renderHeight;
}
extern "C" __declspec(dllexport) void __cdecl PipClose(Producer* p) {delete p;}
extern "C" __declspec(dllexport) void __cdecl PipValidation(Producer* p,uint32_t* stats) {p->memory->validate=1;stats[0]=p->memory->verified;stats[1]=p->memory->mismatched;}

extern "C" __declspec(dllexport) void __cdecl PipAllowShared(Producer* p,int enabled){p->allowShared=enabled!=0;}
extern "C" __declspec(dllexport) int __cdecl PipSharedState(Producer* p){return p->gpu?(p->gpu->active?1:p->gpu->rejected?-1:0):0;}
