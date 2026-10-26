#include <stddef.h>
#include "transport.h"
#include <d3d9.h>
#include "MinHook.h"
#include "addresses.h"
static HMODULE selfModule;

static PipMemory* memory;
static HANDLE mapping;
static IDirect3DTexture9* texture;
static IDirect3DStateBlock9* state;
static IDirect3DDevice9* owner;
static UINT tw,th,sequence;
static bool valid;
static DWORD centerColor,lastValidation;
static volatile LONG rendering;
typedef HRESULT (WINAPI *PresentFn)(IDirect3DDevice9*,const RECT*,const RECT*,HWND,const RGNDATA*);
typedef HRESULT (WINAPI *ResetFn)(IDirect3DDevice9*,D3DPRESENT_PARAMETERS*);
static PresentFn originalPresent;
static ResetFn originalReset;
static void ReleaseResources() {
    if(state)state->Release(); if(texture)texture->Release();
    state=nullptr;texture=nullptr;owner=nullptr;valid=false;sequence=0;
}
struct Vertex {float x,y,z,rhw;float u,v;};
static void Draw(IDirect3DDevice9* device) {
    if(!memory || !memory->active || (DWORD)(GetTickCount()-memory->heartbeat)>2000)return;
    if(InterlockedCompareExchange(&rendering,1,0))return;
    LARGE_INTEGER begin,end;QueryPerformanceCounter(&begin);
    if(owner && owner!=device){InterlockedExchange(&rendering,0);return;}
    owner=device;
    float x=0,y=0,w=0,h=0;
    bool draw=false;
    if(TryLock(memory)) {
        x=memory->x;y=memory->y;w=memory->w;h=memory->h;
        UINT width=memory->width,height=memory->height;
        if(width>0&&height>0&&width<=PIP_WIDTH&&height<=PIP_HEIGHT && w>0&&h>0&&x>=0&&y>=0&&x+w<=1.001f&&y+h<=1.001f) {
            if(!texture||width!=tw||height!=th) {
                if(texture)texture->Release();texture=nullptr;valid=false;
                HRESULT hr=device->CreateTexture(width,height,1,D3DUSAGE_DYNAMIC,D3DFMT_X8R8G8B8,D3DPOOL_DEFAULT,&texture,nullptr);
                if(FAILED(hr)){if(hr!=D3DERR_DEVICELOST&&hr!=D3DERR_DEVICENOTRESET)memory->error=hr;}else {tw=width;th=height;sequence=0;}
            }
            if(texture && sequence!=memory->sequence) {
                D3DLOCKED_RECT rect;
                HRESULT hr=texture->LockRect(0,&rect,nullptr,D3DLOCK_DISCARD);
                if(SUCCEEDED(hr)) {
                    for(UINT row=0;row<height;row++)memcpy((char*)rect.pBits+row*rect.Pitch,memory->pixels+row*width*4,width*4);
                    centerColor=*(DWORD*)(memory->pixels+((height/2)*width+width/2)*4);
                    texture->UnlockRect(0);sequence=memory->sequence;valid=true;InterlockedIncrement(&memory->uploaded);
                }else if(hr!=D3DERR_DEVICELOST&&hr!=D3DERR_DEVICENOTRESET)memory->error=hr;
            }
            draw=valid;
        }
        Unlock(memory);
    }
    if(draw) {
        if(!state)device->CreateStateBlock(D3DSBT_ALL,&state);
        IDirect3DSurface9 *oldTarget=nullptr,*back=nullptr,*depth=nullptr;
        D3DVIEWPORT9 viewport={};
        if(state&&SUCCEEDED(state->Capture())&&SUCCEEDED(device->GetRenderTarget(0,&oldTarget))&&SUCCEEDED(device->GetBackBuffer(0,0,D3DBACKBUFFER_TYPE_MONO,&back))) {
            device->GetDepthStencilSurface(&depth);device->GetViewport(&viewport);
            D3DSURFACE_DESC desc;back->GetDesc(&desc);
            memory->renderWidth=desc.Width;memory->renderHeight=desc.Height;
            if(memory->rendered%60==0){IDirect3DSwapChain9* chain=nullptr;if(SUCCEEDED(device->GetSwapChain(0,&chain))){D3DPRESENT_PARAMETERS params={};if(SUCCEEDED(chain->GetPresentParameters(&params)))memory->exclusive=!params.Windowed;chain->Release();}}
            device->SetDepthStencilSurface(nullptr);device->SetRenderTarget(0,back);
            D3DVIEWPORT9 vp={0,0,desc.Width,desc.Height,0,1};device->SetViewport(&vp);
            if(SUCCEEDED(device->BeginScene())) {
                device->SetVertexShader(nullptr);device->SetPixelShader(nullptr);
                device->SetFVF(D3DFVF_XYZRHW|D3DFVF_TEX1);
                device->SetRenderState(D3DRS_ZENABLE,FALSE);device->SetRenderState(D3DRS_ZWRITEENABLE,FALSE);
                device->SetRenderState(D3DRS_LIGHTING,FALSE);device->SetRenderState(D3DRS_CULLMODE,D3DCULL_NONE);
                device->SetRenderState(D3DRS_ALPHABLENDENABLE,FALSE);device->SetRenderState(D3DRS_ALPHATESTENABLE,FALSE);
                device->SetRenderState(D3DRS_SCISSORTESTENABLE,FALSE);device->SetRenderState(D3DRS_STENCILENABLE,FALSE);
                device->SetRenderState(D3DRS_FOGENABLE,FALSE);device->SetRenderState(D3DRS_SRGBWRITEENABLE,FALSE);
                device->SetRenderState(D3DRS_COLORWRITEENABLE,15);device->SetRenderState(D3DRS_FILLMODE,D3DFILL_SOLID);
                device->SetTexture(0,texture);
                device->SetTextureStageState(0,D3DTSS_COLOROP,D3DTOP_SELECTARG1);device->SetTextureStageState(0,D3DTSS_COLORARG1,D3DTA_TEXTURE);
                device->SetTextureStageState(0,D3DTSS_ALPHAOP,D3DTOP_DISABLE);device->SetTextureStageState(1,D3DTSS_COLOROP,D3DTOP_DISABLE);
                device->SetTextureStageState(0,D3DTSS_TEXCOORDINDEX,0);device->SetTextureStageState(0,D3DTSS_TEXTURETRANSFORMFLAGS,D3DTTFF_DISABLE);
                device->SetSamplerState(0,D3DSAMP_MINFILTER,D3DTEXF_LINEAR);device->SetSamplerState(0,D3DSAMP_MAGFILTER,D3DTEXF_LINEAR);
                device->SetSamplerState(0,D3DSAMP_ADDRESSU,D3DTADDRESS_CLAMP);device->SetSamplerState(0,D3DSAMP_ADDRESSV,D3DTADDRESS_CLAMP);
                device->SetSamplerState(0,D3DSAMP_SRGBTEXTURE,FALSE);
                float l=x*desc.Width-.5f,t=y*desc.Height-.5f,r=(x+w)*desc.Width-.5f,b=(y+h)*desc.Height-.5f;
                Vertex vertices[]={{l,t,0,1,0,0},{r,t,0,1,1,0},{l,b,0,1,0,1},{r,b,0,1,1,1}};
                HRESULT hr=device->DrawPrimitiveUP(D3DPT_TRIANGLESTRIP,2,vertices,sizeof(Vertex));
                device->EndScene();if(SUCCEEDED(hr))InterlockedIncrement(&memory->rendered);else memory->error=hr;
                // Expensive readback is enabled only by the test harness, never by the editor.
                if(memory->validate && GetTickCount()-lastValidation>500) {
                    lastValidation=GetTickCount();IDirect3DSurface9* system=nullptr;
                    if(SUCCEEDED(device->CreateOffscreenPlainSurface(desc.Width,desc.Height,desc.Format,D3DPOOL_SYSTEMMEM,&system,nullptr))) {
                        if(SUCCEEDED(device->GetRenderTargetData(back,system))) {
                            D3DLOCKED_RECT read;if(SUCCEEDED(system->LockRect(&read,nullptr,D3DLOCK_READONLY))) {
                                UINT px=(UINT)((x+w*.5f)*desc.Width),py=(UINT)((y+h*.5f)*desc.Height);
                                DWORD actual=*(DWORD*)((char*)read.pBits+py*read.Pitch+px*4);bool match=true;
                                for(int c=0;c<3;c++){int a=(actual>>(c*8))&255,b=(centerColor>>(c*8))&255;if(abs(a-b)>40)match=false;}
                                InterlockedIncrement(match?&memory->verified:&memory->mismatched);system->UnlockRect();
                            }
                        }system->Release();
                    }
                }
            }
            device->SetRenderTarget(0,oldTarget);device->SetDepthStencilSurface(depth);state->Apply();device->SetViewport(&viewport);
        }
        if(oldTarget)oldTarget->Release();if(back)back->Release();if(depth)depth->Release();
    }
    QueryPerformanceCounter(&end);memory->renderTicks=(uint64_t)(end.QuadPart-begin.QuadPart);
    InterlockedExchange(&rendering,0);
}
static HRESULT WINAPI Present(IDirect3DDevice9* device,const RECT* a,const RECT* b,HWND c,const RGNDATA* d) {
    Draw(device);return originalPresent(device,a,b,c,d);
}
static HRESULT WINAPI Reset(IDirect3DDevice9* device,D3DPRESENT_PARAMETERS* pp) {
    if(owner==device)ReleaseResources();return originalReset(device,pp);
}
static DWORD WINAPI Initialize(void*) {
    wchar_t name[80];MappingName(name,GetCurrentProcessId());
    mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(!mapping)return 1;
    memory=(PipMemory*)MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(PipMemory));
    if(!memory||memory->magic!=PIP_MAGIC||memory->version!=1)return 2;
    wchar_t path[32768];AddressPath(selfModule,path);Addresses a={};DWORD read=0;
    HANDLE file=CreateFileW(path,GENERIC_READ,FILE_SHARE_READ,nullptr,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(file!=INVALID_HANDLE_VALUE){ReadFile(file,&a,sizeof(a),&read,nullptr);CloseHandle(file);}
    HMODULE d3d=GetModuleHandleW(L"d3d9.dll");if(!d3d){memory->error=0x8007007E;return 3;}auto headers=Headers(d3d);
    a.resetModule[63]=0;
    HMODULE resetOwner=GetModuleHandleW(a.resetModule);
    auto resetHeaders=resetOwner?Headers(resetOwner):nullptr;
    if(read==sizeof(a)&&a.magic==0x36414450&&a.machine==headers->FileHeader.Machine&&a.timestamp==headers->FileHeader.TimeDateStamp&&a.imageSize==headers->OptionalHeader.SizeOfImage&&a.present<a.imageSize&&resetHeaders&&a.resetTimestamp==resetHeaders->FileHeader.TimeDateStamp&&a.resetImageSize==resetHeaders->OptionalHeader.SizeOfImage&&a.reset<a.resetImageSize) {
        MH_STATUS status=MH_Initialize();
        if(status==MH_OK) status=MH_CreateHook((char*)d3d+a.present,(void*)Present,(void**)&originalPresent);
        if(status==MH_OK) status=MH_CreateHook((char*)resetOwner+a.reset,(void*)Reset,(void**)&originalReset);
        if(status==MH_OK)status=MH_EnableHook(MH_ALL_HOOKS);
        if(status!=MH_OK){MH_Uninitialize();memory->error=0x80000000|status;}else memory->ready=1;
    }else memory->error=0x8007000D;
    return 0;
}
BOOL WINAPI DllMain(HINSTANCE module,DWORD reason,void*) {
    if(reason==DLL_PROCESS_ATTACH){selfModule=module;DisableThreadLibraryCalls(module);HANDLE thread=CreateThread(nullptr,0,Initialize,nullptr,0,nullptr);if(thread)CloseHandle(thread);}
    // Hooks stay loaded and become passive when producer stops. No unsafe unload during Present.
    return TRUE;
}
