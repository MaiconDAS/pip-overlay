#include <stddef.h>
#include "transport.h"
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
    owner = runtime;
    memory->ready = 1;
    uint32_t outputW = 0, outputH = 0;
    runtime->get_screenshot_width_and_height(&outputW, &outputH);
    memory->renderWidth = outputW; memory->renderHeight = outputH;
    if (!memory->active || DWORD(GetTickCount() - memory->heartbeat) > 2000) {
        runtime->set_technique_state(technique, false); valid = false; return;
    }
    if (!TryLock(memory)) return;
    uint32_t w = memory->width, h = memory->height, next = memory->sequence;
    float box[4] = {memory->x, memory->y, memory->w, memory->h};
    bool bounds = w && h && w <= PIP_WIDTH && h <= PIP_HEIGHT && box[0] >= 0 && box[1] >= 0 && box[2] > 0 && box[3] > 0 && box[0]+box[2] <= 1.001f && box[1]+box[3] <= 1.001f;
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
                InterlockedIncrement(match?&memory->verified:&memory->mismatched);
            }
        }
    }
    if (upload) { pixels.resize(size_t(w)*h*4); memcpy(pixels.data(), memory->pixels, pixels.size()); }
    Unlock(memory);
    if (!bounds) { runtime->set_technique_state(technique, false); return; }
    LARGE_INTEGER start, end; QueryPerformanceCounter(&start);
    if (upload) {
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
        sequence = next; valid = true; InterlockedIncrement(&memory->uploaded);
    }
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
    if (runtime == owner) { owner = nullptr; valid = false; sequence = 0; if (memory) memory->ready = 0; }
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
        reshade::unregister_addon(module);
        if (memory) UnmapViewOfFile(memory);
        if (mapping) CloseHandle(mapping);
    }
    return TRUE;
}
