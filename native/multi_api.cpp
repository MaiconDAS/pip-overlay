#include <stddef.h>
#include "transport.h"
#include "gpu_transport.h"
#include <d3d11.h>
#include "reshade.hpp"
#include <vector>
#include <mutex>

// ReShade owns API-specific synchronization, swapchain lifetimes and state restoration.
// Our add-on never registers an input handler or an interactive overlay.
extern "C" __declspec(dllexport) const char *NAME = "PiP Overlay";
extern "C" __declspec(dllexport) const char *DESCRIPTION = "Video rectangle controlled by the external PiP editor.";
static HANDLE mapping;
static PipMemory *memory;
static std::mutex gate;
static reshade::api::effect_runtime *owner;
static uint32_t sequence;
static bool valid;
static std::vector<unsigned char> pixels;
static DWORD lastOpen,lastValidation;
static bool BlockOverlay(reshade::api::effect_runtime*,bool open,reshade::api::input_source){return open;}

static HANDLE gpuMapping;
static GpuChannel* gpu;
static ID3D11Texture2D* gpuTexture[3];
static IDXGIKeyedMutex* gpuMutex[3];
static uint32_t gpuGeneration;
static bool gpuImage;
static DWORD gpuOpen;
static void ReleaseGpuTexture(){for(int i=0;i<3;i++){if(gpuMutex[i])gpuMutex[i]->Release();if(gpuTexture[i])gpuTexture[i]->Release();gpuMutex[i]=nullptr;gpuTexture[i]=nullptr;}gpuGeneration=0;}
static void PrepareGpu(reshade::api::effect_runtime* runtime){
    if(runtime->get_device()->get_api()!=reshade::api::device_api::d3d11)return;
    if(!gpu&&DWORD(GetTickCount()-gpuOpen)>500){
        gpuOpen=GetTickCount();wchar_t name[80];GpuName(name,GetCurrentProcessId());gpuMapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);
        if(gpuMapping)gpu=(GpuChannel*)MapViewOfFile(gpuMapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(GpuChannel));
        if(!gpu&&gpuMapping){CloseHandle(gpuMapping);gpuMapping=nullptr;}
        if(gpu&&(gpu->magic!=GPU_MAGIC||gpu->version!=1)){UnmapViewOfFile(gpu);gpu=nullptr;CloseHandle(gpuMapping);gpuMapping=nullptr;}
    }
    if(gpu)InterlockedExchange(&gpu->supported,1);
}
static bool UploadGpu(reshade::api::effect_runtime* runtime,reshade::api::effect_texture_variable image,UINT w,UINT h){
    if(!gpu||!gpu->active||gpu->rejected||!GpuLock(gpu))return false;
    GpuSlot slots[3];memcpy(slots,gpu->slots,sizeof(slots));uint32_t generation=gpu->generation;bool size=gpu->width==w&&gpu->height==h;GpuUnlock(gpu);
    if(!size)return false;
    auto device=reinterpret_cast<ID3D11Device*>(runtime->get_device()->get_native());
    if(gpuGeneration!=generation){ReleaseGpuTexture();gpuGeneration=generation;}
    for(int i=0;i<3;i++)if(!gpuTexture[i]&&slots[i].handle){
        HRESULT hr=device->OpenSharedResource((HANDLE)(uintptr_t)slots[i].handle,__uuidof(ID3D11Texture2D),(void**)&gpuTexture[i]);
        if(SUCCEEDED(hr))hr=gpuTexture[i]->QueryInterface(__uuidof(IDXGIKeyedMutex),(void**)&gpuMutex[i]);
        if(FAILED(hr)){gpu->rejected=1;ReleaseGpuTexture();return false;}
    }
    bool tried[3]={};int selected=-1;
    for(int n=0;n<3;n++){
        int index=-1;for(int i=0;i<3;i++)if(!tried[i]&&gpuMutex[i]&&slots[i].sequence>sequence&&(index<0||slots[i].sequence>slots[index].sequence))index=i;
        if(index<0)break;tried[index]=true;HRESULT hr=gpuMutex[index]->AcquireSync(1,0);
        if(hr==S_OK){selected=index;break;}if(hr!=WAIT_TIMEOUT){gpu->rejected=1;return false;}
    }
    if(selected<0){for(int i=0;i<3;i++)if(gpuMutex[i]&&gpu->slots[i].sequence<=sequence&&gpuMutex[i]->AcquireSync(1,0)==S_OK)gpuMutex[i]->ReleaseSync(gpu->slots[i].sequence<=sequence?0:1);return false;}
    if(gpu->generation!=generation){gpuMutex[selected]->ReleaseSync(0);return false;}
    reshade::api::resource_view view={};runtime->get_texture_binding(image,&view,nullptr);
    if(!view.handle){gpuMutex[selected]->ReleaseSync(0);return false;}
    auto resource=runtime->get_device()->get_resource_from_view(view);
    ID3D11DeviceContext* context=nullptr;device->GetImmediateContext(&context);
    D3D11_BOX box={0,0,0,w,h,1};context->CopySubresourceRegion(reinterpret_cast<ID3D11Resource*>(resource.handle),0,0,0,0,gpuTexture[selected],0,&box);context->Flush();context->Release();
    sequence=slots[selected].sequence;HRESULT hr=gpuMutex[selected]->ReleaseSync(0);
    if(FAILED(hr)){gpu->rejected=1;return false;}
    // Retire older completed frames so they cannot fill the ring indefinitely.
    for(int i=0;i<3;i++)if(i!=selected&&gpuMutex[i]&&gpu->slots[i].sequence<=sequence){
        HRESULT lock=gpuMutex[i]->AcquireSync(1,0);
        if(lock==S_OK)gpuMutex[i]->ReleaseSync(gpu->slots[i].sequence<=sequence?0:1);
    }
    valid=true;gpuImage=true;InterlockedIncrement(&memory->uploaded);return true;
}
static void Present(reshade::api::effect_runtime *runtime)
{
    std::unique_lock<std::mutex> guard(gate, std::try_to_lock);
    if (!guard.owns_lock()) return;
    if (!memory) {
        if(DWORD(GetTickCount()-lastOpen)<1000)return;
        lastOpen=GetTickCount();
        wchar_t name[80]; MappingName(name, GetCurrentProcessId());
        mapping = OpenFileMappingW(FILE_MAP_ALL_ACCESS, FALSE, name);
        if (!mapping) return;
        memory = (PipMemory *)MapViewOfFile(mapping, FILE_MAP_ALL_ACCESS, 0, 0, sizeof(PipMemory));
        if (!memory) { CloseHandle(mapping); mapping = nullptr; return; }
        if (memory->magic != PIP_MAGIC || memory->version != 1) {
            UnmapViewOfFile(memory); memory = nullptr; CloseHandle(mapping); mapping = nullptr; return;
        }
    }
    if (owner && owner != runtime) return;
    auto technique = runtime->find_technique("PipOverlay.fx", "PiPOverlay");
    auto image = runtime->find_texture_variable("PipOverlay.fx", "PipVideo");
    auto rect = runtime->find_uniform_variable("PipOverlay.fx", "PipRect");
    auto uv = runtime->find_uniform_variable("PipOverlay.fx", "PipUV");
    if (!technique.handle || !image.handle || !rect.handle || !uv.handle) return;
    owner = runtime;PrepareGpu(runtime);
    memory->ready = 1;
    uint32_t outputW = 0, outputH = 0;
    runtime->get_screenshot_width_and_height(&outputW, &outputH);
    memory->renderWidth = outputW; memory->renderHeight = outputH;
    if (!memory->active || DWORD(GetTickCount() - memory->heartbeat) > 2000) {
        runtime->set_technique_state(technique, false);ReleaseGpuTexture();gpuImage=false; valid = false; return;
    }
    if (!TryLock(memory)) return;
    uint32_t w = memory->width, h = memory->height, next = memory->sequence;
    float box[4] = {memory->x, memory->y, memory->w, memory->h};
    bool bounds = w && h && w <= PIP_WIDTH && h <= PIP_HEIGHT && box[0] >= 0 && box[1] >= 0 && box[2] > 0 && box[3] > 0 && box[0]+box[2] <= 1.001f && box[1]+box[3] <= 1.001f;
    bool gpuMode=gpu&&gpu->active&&!gpu->rejected;
    if(!gpuMode&&gpuTexture[0])ReleaseGpuTexture();
    bool upload = bounds && (!valid || sequence != next);
    // Readback is strictly a test harness option, never enabled by the user interface.
    if(bounds && valid && !upload && memory->validate && DWORD(GetTickCount()-lastValidation)>500){
        lastValidation=GetTickCount();
        std::vector<unsigned char> shot(size_t(outputW)*outputH*4);
        bool captured=false;
        if(runtime->get_device()->get_api()==reshade::api::device_api::opengl){
            HMODULE gl=GetModuleHandleW(L"opengl32.dll");
            auto read=(void(WINAPI*)(int,int,int,int,unsigned,unsigned,void*))GetProcAddress(gl,"glReadPixels");
            if(read){read(0,0,outputW,outputH,0x1908,0x1401,shot.data());
                // OpenGL readback has its origin at the bottom-left.
                std::vector<unsigned char> row(size_t(outputW)*4);
                for(unsigned y=0;y<outputH/2;y++){auto a=shot.data()+size_t(y)*outputW*4;auto b=shot.data()+size_t(outputH-1-y)*outputW*4;memcpy(row.data(),a,row.size());memcpy(a,b,row.size());memcpy(b,row.data(),row.size());}captured=true;}
        }else captured=runtime->capture_screenshot(shot.data());
        if(captured){
            unsigned px=unsigned((box[0]+box[2]*0.5f)*outputW),py=unsigned((box[1]+box[3]*0.5f)*outputH);
            if(px<outputW && py<outputH){
                auto actual=shot.data()+(size_t(py)*outputW+px)*4;
                auto expected=memory->pixels+(size_t(h/2)*w+w/2)*4;
                bool match=abs(int(actual[0])-expected[2])<20&&abs(int(actual[1])-expected[1])<20&&abs(int(actual[2])-expected[0])<20;
                if(memory->validate==2){unsigned cx=unsigned(box[0]*outputW),cy=unsigned(box[1]*outputH);auto corner=shot.data()+(size_t(cy)*outputW+cx)*4;bool filled=abs(int(corner[0])-expected[2])<20&&abs(int(corner[1])-expected[1])<20&&abs(int(corner[2])-expected[0])<20;match=match&&!filled;}
                InterlockedIncrement(match?&memory->verified:&memory->mismatched);
            }
        }
    }
    if (upload&&!gpuMode) { pixels.resize(size_t(w)*h*4); memcpy(pixels.data(), memory->pixels, pixels.size()); }
    Unlock(memory);
    if (!bounds) { runtime->set_technique_state(technique, false); return; }
    LARGE_INTEGER start, end; QueryPerformanceCounter(&start);
    if(gpuMode){UploadGpu(runtime,image,w,h);}
    else if (upload) {
        reshade::api::resource_view view = {};
        runtime->get_texture_binding(image, &view, nullptr);
        if (!view.handle) return;
        auto resource = runtime->get_device()->get_resource_from_view(view);
        auto cmd = runtime->get_command_queue()->get_immediate_command_list();
        // Upload only the current frame dimensions; never rescale to the allocation size.
        reshade::api::subresource_box region = {0,0,0,w,h,1};
        cmd->barrier(resource, reshade::api::resource_usage::shader_resource, reshade::api::resource_usage::copy_dest);
        cmd->update_texture_region({pixels.data(),w*4,w*h*4}, resource, 0, &region);
        cmd->barrier(resource, reshade::api::resource_usage::copy_dest, reshade::api::resource_usage::shader_resource);
        sequence = next; valid = true;gpuImage=false; InterlockedIncrement(&memory->uploaded);
    }
    auto nativeColor=runtime->find_uniform_variable("PipOverlay.fx","PipNativeColor");
    if(nativeColor.handle){float native=gpuImage?1.f:0.f;runtime->set_uniform_value_float(nativeColor,&native,1);}
    runtime->set_uniform_value_float(rect, box, 4);
    float scale[2] = {float(w)/PIP_WIDTH,float(h)/PIP_HEIGHT};
    runtime->set_uniform_value_float(uv, scale, 2);
    runtime->set_technique_state(technique, valid);
    QueryPerformanceCounter(&end); memory->renderTicks = end.QuadPart - start.QuadPart;
}
static void Rendered(reshade::api::effect_runtime *runtime, reshade::api::effect_technique technique, reshade::api::command_list *, reshade::api::resource_view, reshade::api::resource_view)
{
    if (runtime == owner && memory && valid && technique == runtime->find_technique("PipOverlay.fx", "PiPOverlay"))
        InterlockedIncrement(&memory->rendered);
}
static void Destroy(reshade::api::effect_runtime *runtime)
{
    std::lock_guard<std::mutex> guard(gate);
    if (runtime == owner) { ReleaseGpuTexture();gpuImage=false;if(gpu){gpu->supported=0;gpu->rejected=1;}owner = nullptr; valid = false; sequence = 0; if (memory) memory->ready = 0; }
}
BOOL WINAPI DllMain(HMODULE module, DWORD reason, LPVOID)
{
    if (reason == DLL_PROCESS_ATTACH) {
        if (!reshade::register_addon(module)) return FALSE;
        reshade::register_event<reshade::addon_event::reshade_present>(Present);
        reshade::register_event<reshade::addon_event::reshade_render_technique>(Rendered);
        reshade::register_event<reshade::addon_event::destroy_effect_runtime>(Destroy);
        reshade::register_event<reshade::addon_event::reshade_open_overlay>(BlockOverlay);
    } else if (reason == DLL_PROCESS_DETACH) {
        reshade::unregister_addon(module);ReleaseGpuTexture();if(gpu)UnmapViewOfFile(gpu);if(gpuMapping)CloseHandle(gpuMapping);
        if (memory) UnmapViewOfFile(memory);
        if (mapping) CloseHandle(mapping);
    }
    return TRUE;
}
