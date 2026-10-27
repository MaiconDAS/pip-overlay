#include <stddef.h>
#include "transport.h"
#include <d3d11.h>
#include <stdio.h>
#include <vector>
int main(){
    HMODULE module=LoadLibraryW(L"PipGpu.dll");if(!module)return 1;
    auto open=(void*(__cdecl*)(DWORD))GetProcAddress(module,"PipOpen");
    auto resolution=(void(__cdecl*)(void*,UINT,UINT))GetProcAddress(module,"PipResolution");
    auto submit=(HRESULT(__cdecl*)(void*,ID3D11Texture2D*))GetProcAddress(module,"PipSubmit");
    auto close=(void(__cdecl*)(void*))GetProcAddress(module,"PipClose");
    if(!open||!resolution||!submit||!close)return 2;
    ID3D11Device* device=nullptr;ID3D11DeviceContext* context=nullptr;D3D_FEATURE_LEVEL level;
    HRESULT hr=D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&device,&level,&context);if(FAILED(hr))return 3;
    void* producer=open(GetCurrentProcessId());if(!producer)return 4;
    wchar_t name[80];MappingName(name,GetCurrentProcessId());HANDLE mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);auto memory=(PipMemory*)MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(PipMemory));if(!memory)return 5;
    int result=0;
    for(auto size: {SIZE{1280,720},SIZE{1920,1080}}){
        UINT w=size.cx,h=size.cy;std::vector<unsigned char> pixels(size_t(w)*h*4);
        // Single-pixel edges, gradients and alternating detail expose filtering/blur.
        for(UINT y=0;y<h;y++)for(UINT x=0;x<w;x++){auto p=&pixels[(size_t(y)*w+x)*4];p[0]=x%256;p[1]=((x+y)&1)?255:0;p[2]=y%256;p[3]=255;}
        D3D11_TEXTURE2D_DESC desc={};desc.Width=w;desc.Height=h;desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE;
        D3D11_SUBRESOURCE_DATA data={pixels.data(),w*4,0};ID3D11Texture2D* texture=nullptr;if(FAILED(device->CreateTexture2D(&desc,&data,&texture))){result=6;break;}
        resolution(producer,1920,1080);uint32_t before=memory->sequence;
        for(int i=0;i<20;i++){if(FAILED(submit(producer,texture))){result=7;break;}Sleep(10);if(memory->sequence>before+2)break;}
        bool match=memory->sequence>before&&memory->width==w&&memory->height==h&&memcmp(memory->pixels,pixels.data(),pixels.size())==0;
        printf("%s %ux%u: native-size capture is pixel-exact\n",match?"PASS":"FAIL",w,h);if(!match)result=8;
        texture->Release();
    }
    UnmapViewOfFile(memory);CloseHandle(mapping);close(producer);context->Release();device->Release();FreeLibrary(module);return result;
}
