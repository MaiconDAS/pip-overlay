#include <windows.h>
#include <d3d11.h>
#include <dxgi.h>
#include <stdio.h>
#include <vector>
#include <string>
static unsigned Color(unsigned x,unsigned y){return 0xff000000u|((y&255)<<16)|(((x+y)&1)?0xff00:0)|(x&255);}
static double Now(){LARGE_INTEGER t,f;QueryPerformanceCounter(&t);QueryPerformanceFrequency(&f);return double(t.QuadPart)/f.QuadPart;}
#define CHECK(x) if(FAILED(x)){fprintf(stderr,"failed at line %d\n",__LINE__);return 1;}
int wmain(int argc,wchar_t**argv){
    ID3D11Device*d=nullptr;ID3D11DeviceContext*c=nullptr;D3D_FEATURE_LEVEL level;
    CHECK(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&d,&level,&c));
    const unsigned width=1920,height=1080;
    D3D11_TEXTURE2D_DESC desc={};desc.Width=width;desc.Height=height;desc.MipLevels=desc.ArraySize=desc.SampleDesc.Count=1;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.Usage=D3D11_USAGE_DEFAULT;desc.BindFlags=D3D11_BIND_SHADER_RESOURCE|D3D11_BIND_RENDER_TARGET;
    if(argc>2&&!wcscmp(argv[1],L"--read")){
        auto handle=(HANDLE)(uintptr_t)_wcstoui64(argv[2],nullptr,10);ID3D11Texture2D*shared=nullptr;IDXGIKeyedMutex*mutex=nullptr;
        CHECK(d->OpenSharedResource(handle,__uuidof(ID3D11Texture2D),(void**)&shared));CHECK(shared->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&mutex));
        desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;ID3D11Texture2D*read=nullptr;CHECK(d->CreateTexture2D(&desc,nullptr,&read));
        if(mutex->AcquireSync(1,5000)!=S_OK)return 2;c->CopyResource(read,shared);c->Flush();CHECK(mutex->ReleaseSync(0));
        D3D11_MAPPED_SUBRESOURCE mapped;CHECK(c->Map(read,0,D3D11_MAP_READ,0,&mapped));bool good=true;
        for(unsigned y=0;y<height;y++)for(unsigned x=0;x<width;x++)if(((unsigned*)((char*)mapped.pData+y*mapped.RowPitch))[x]!=Color(x,y))good=false;
        c->Unmap(read,0);read->Release();mutex->Release();shared->Release();c->Release();d->Release();return good?0:3;
    }
    std::vector<unsigned> pixels(width*height);for(unsigned y=0;y<height;y++)for(unsigned x=0;x<width;x++)pixels[y*width+x]=Color(x,y);
    D3D11_SUBRESOURCE_DATA initial={pixels.data(),width*4,0};ID3D11Texture2D*source=nullptr,*shared=nullptr;CHECK(d->CreateTexture2D(&desc,&initial,&source));
    desc.MiscFlags=D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;CHECK(d->CreateTexture2D(&desc,nullptr,&shared));IDXGIResource*resource=nullptr;IDXGIKeyedMutex*producer=nullptr;
    CHECK(shared->QueryInterface(__uuidof(IDXGIResource),(void**)&resource));HANDLE handle=nullptr;CHECK(resource->GetSharedHandle(&handle));resource->Release();CHECK(shared->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&producer));
    if(producer->AcquireSync(0,5000)!=S_OK)return 4;c->CopyResource(shared,source);c->Flush();CHECK(producer->ReleaseSync(1));
    wchar_t exe[32768];GetModuleFileNameW(nullptr,exe,32768);std::wstring command=L"\""+std::wstring(exe)+L"\" --read "+std::to_wstring((uintptr_t)handle);
    STARTUPINFOW si={};si.cb=sizeof(si);PROCESS_INFORMATION child={};if(!CreateProcessW(exe,command.data(),nullptr,nullptr,FALSE,CREATE_NO_WINDOW,nullptr,nullptr,&si,&child))return 5;
    if(WaitForSingleObject(child.hProcess,10000)!=WAIT_OBJECT_0){TerminateProcess(child.hProcess,6);return 6;}DWORD childResult;GetExitCodeProcess(child.hProcess,&childResult);CloseHandle(child.hProcess);CloseHandle(child.hThread);if(childResult)return 7;
    ID3D11Device*d2=nullptr;ID3D11DeviceContext*c2=nullptr;CHECK(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_HARDWARE,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&d2,&level,&c2));
    ID3D11Texture2D*opened=nullptr,*destination=nullptr,*staging=nullptr;IDXGIKeyedMutex*consumer=nullptr;CHECK(d2->OpenSharedResource(handle,__uuidof(ID3D11Texture2D),(void**)&opened));CHECK(opened->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&consumer));
    desc.MiscFlags=0;CHECK(d2->CreateTexture2D(&desc,nullptr,&destination));desc.Usage=D3D11_USAGE_STAGING;desc.BindFlags=0;desc.CPUAccessFlags=D3D11_CPU_ACCESS_READ;CHECK(d->CreateTexture2D(&desc,nullptr,&staging));
    const int count=240;double begin=Now();
    for(int i=0;i<count;i++){if(producer->AcquireSync(0,5000)!=S_OK)return 8;c->CopyResource(shared,source);c->Flush();CHECK(producer->ReleaseSync(1));if(consumer->AcquireSync(1,5000)!=S_OK)return 9;c2->CopyResource(destination,opened);c2->Flush();CHECK(consumer->ReleaseSync(0));}
    double gpuMs=(Now()-begin)*1000/count;begin=Now();
    for(int i=0;i<count;i++){c->CopyResource(staging,source);D3D11_MAPPED_SUBRESOURCE mapped;CHECK(c->Map(staging,0,D3D11_MAP_READ,0,&mapped));c2->UpdateSubresource(destination,0,nullptr,mapped.pData,mapped.RowPitch,0);c2->Flush();c->Unmap(staging,0);}
    double cpuMs=(Now()-begin)*1000/count;
    printf("{\"crossProcessPixelsExact\":true,\"width\":1920,\"height\":1080,\"iterations\":%d,\"sharedGpuMsPerIteration\":%.4f,\"synchronousCpuRoundtripMsPerIteration\":%.4f}\n",count,gpuMs,cpuMs);
    staging->Release();destination->Release();consumer->Release();opened->Release();c2->Release();d2->Release();producer->Release();shared->Release();source->Release();c->Release();d->Release();return 0;
}
