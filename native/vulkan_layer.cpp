#define VK_NO_PROTOTYPES
#include <stddef.h>
#include "transport.h"
#include <vulkan/vk_layer.h>
#include <map>
#include <vector>
#include <mutex>
#include <memory>
#include <algorithm>
#include <cmath>

#define EXPORT extern "C" __declspec(dllexport)
EXPORT PFN_vkVoidFunction VKAPI_CALL vkGetInstanceProcAddr(VkInstance,const char*);
EXPORT PFN_vkVoidFunction VKAPI_CALL vkGetDeviceProcAddr(VkDevice,const char*);
struct Instance{VkInstance handle;PFN_vkGetInstanceProcAddr next;};
struct Frame{
    VkCommandPool pool{};VkCommandBuffer command{};VkFence fence{};VkSemaphore done{};
    VkImage source{};VkDeviceMemory imageMemory{},uploadMemory{},readMemory{};VkBuffer upload{},read{};void* mapped{};void* readMapped{};
    uint32_t sequence=0,expected=0,width=0,height=0;float x=0,y=0,w=0,h=0;bool initialized=false,pending=false,verify=false,verifyCorner=false;
};
struct Swap{
    VkSwapchainKHR handle{};VkExtent2D size{};VkFormat format{};bool enabled=false,readable=false;
    uint32_t family=UINT32_MAX;std::vector<VkImage> images;std::vector<Frame> frames;
};
struct Device{
    VkDevice handle;VkPhysicalDevice physical;Instance instance;PFN_vkGetDeviceProcAddr next;PFN_vkSetDeviceLoaderData loaderData{};
    VkPhysicalDeviceMemoryProperties memory{};std::vector<VkQueueFamilyProperties> families;
    std::map<VkSwapchainKHR,Swap> swaps;
};
static std::recursive_mutex gate;
static std::map<VkInstance,Instance> instances;
static std::map<VkPhysicalDevice,VkInstance> physicals;
static std::map<VkDevice,std::unique_ptr<Device>> devices;
struct Queue{Device* device;uint32_t family;bool protectedQueue=false;};static std::map<VkQueue,Queue> queues;
static HANDLE mapping;static PipMemory* video;static DWORD lastOpen;
#define CALL(name) ((PFN_##name)d.next(d.handle,#name))
#define ICALL(name) ((PFN_##name)d.instance.next(d.instance.handle,#name))

static bool OpenVideo(){
    if(video)return true;
    if(DWORD(GetTickCount()-lastOpen)<1000)return false;lastOpen=GetTickCount();
    wchar_t name[80];MappingName(name,GetCurrentProcessId());mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);if(!mapping)return false;
    video=(PipMemory*)MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(PipMemory));
    if(!video||video->magic!=PIP_MAGIC||video->version!=1){if(video)UnmapViewOfFile(video);video=nullptr;CloseHandle(mapping);mapping=nullptr;return false;}return true;
}
static uint32_t MemoryType(Device& d,uint32_t bits,VkMemoryPropertyFlags flags){for(uint32_t i=0;i<d.memory.memoryTypeCount;i++)if((bits&(1u<<i))&&(d.memory.memoryTypes[i].propertyFlags&flags)==flags)return i;return UINT32_MAX;}
static bool Buffer(Device& d,VkDeviceSize size,VkBufferUsageFlags usage,VkBuffer& buffer,VkDeviceMemory& memory,void*&mapped){
    VkBufferCreateInfo create={VK_STRUCTURE_TYPE_BUFFER_CREATE_INFO};create.size=size;create.usage=usage;create.sharingMode=VK_SHARING_MODE_EXCLUSIVE;
    if(CALL(vkCreateBuffer)(d.handle,&create,nullptr,&buffer)!=VK_SUCCESS)return false;
    VkMemoryRequirements req;CALL(vkGetBufferMemoryRequirements)(d.handle,buffer,&req);uint32_t type=MemoryType(d,req.memoryTypeBits,VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT|VK_MEMORY_PROPERTY_HOST_COHERENT_BIT);if(type==UINT32_MAX)return false;
    VkMemoryAllocateInfo allocate={VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocate.allocationSize=req.size;allocate.memoryTypeIndex=type;
    return CALL(vkAllocateMemory)(d.handle,&allocate,nullptr,&memory)==VK_SUCCESS&&CALL(vkBindBufferMemory)(d.handle,buffer,memory,0)==VK_SUCCESS&&CALL(vkMapMemory)(d.handle,memory,0,VK_WHOLE_SIZE,0,&mapped)==VK_SUCCESS;
}
static void FreeFrame(Device& d,Frame& f){
    if(f.mapped)CALL(vkUnmapMemory)(d.handle,f.uploadMemory);if(f.readMapped)CALL(vkUnmapMemory)(d.handle,f.readMemory);
    if(f.pool)CALL(vkDestroyCommandPool)(d.handle,f.pool,nullptr);if(f.fence)CALL(vkDestroyFence)(d.handle,f.fence,nullptr);if(f.done)CALL(vkDestroySemaphore)(d.handle,f.done,nullptr);
    if(f.source)CALL(vkDestroyImage)(d.handle,f.source,nullptr);if(f.upload)CALL(vkDestroyBuffer)(d.handle,f.upload,nullptr);if(f.read)CALL(vkDestroyBuffer)(d.handle,f.read,nullptr);
    if(f.imageMemory)CALL(vkFreeMemory)(d.handle,f.imageMemory,nullptr);if(f.uploadMemory)CALL(vkFreeMemory)(d.handle,f.uploadMemory,nullptr);if(f.readMemory)CALL(vkFreeMemory)(d.handle,f.readMemory,nullptr);f={};
}
static bool InitFrame(Device&d,Frame&f,uint32_t family){
    VkCommandPoolCreateInfo pi={VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};pi.queueFamilyIndex=family;if(CALL(vkCreateCommandPool)(d.handle,&pi,nullptr,&f.pool)!=VK_SUCCESS)return false;
    VkCommandBufferAllocateInfo ai={VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};ai.commandPool=f.pool;ai.level=VK_COMMAND_BUFFER_LEVEL_PRIMARY;ai.commandBufferCount=1;
    if(CALL(vkAllocateCommandBuffers)(d.handle,&ai,&f.command)!=VK_SUCCESS)return false;
    if(d.loaderData&&d.loaderData(d.handle,f.command)!=VK_SUCCESS)return false;
    VkFenceCreateInfo fi={VK_STRUCTURE_TYPE_FENCE_CREATE_INFO};fi.flags=VK_FENCE_CREATE_SIGNALED_BIT;VkSemaphoreCreateInfo si={VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO};
    if(CALL(vkCreateFence)(d.handle,&fi,nullptr,&f.fence)!=VK_SUCCESS||CALL(vkCreateSemaphore)(d.handle,&si,nullptr,&f.done)!=VK_SUCCESS)return false;
    VkImageCreateInfo image={VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO};image.imageType=VK_IMAGE_TYPE_2D;image.format=VK_FORMAT_B8G8R8A8_UNORM;image.extent={PIP_WIDTH,PIP_HEIGHT,1};image.mipLevels=image.arrayLayers=1;image.samples=VK_SAMPLE_COUNT_1_BIT;image.tiling=VK_IMAGE_TILING_OPTIMAL;image.usage=VK_IMAGE_USAGE_TRANSFER_SRC_BIT|VK_IMAGE_USAGE_TRANSFER_DST_BIT;image.sharingMode=VK_SHARING_MODE_EXCLUSIVE;
    if(CALL(vkCreateImage)(d.handle,&image,nullptr,&f.source)!=VK_SUCCESS)return false;
    VkMemoryRequirements req;CALL(vkGetImageMemoryRequirements)(d.handle,f.source,&req);uint32_t type=MemoryType(d,req.memoryTypeBits,VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT);if(type==UINT32_MAX)return false;
    VkMemoryAllocateInfo allocate={VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO};allocate.allocationSize=req.size;allocate.memoryTypeIndex=type;
    if(CALL(vkAllocateMemory)(d.handle,&allocate,nullptr,&f.imageMemory)!=VK_SUCCESS||CALL(vkBindImageMemory)(d.handle,f.source,f.imageMemory,0)!=VK_SUCCESS)return false;
    return Buffer(d,PIP_WIDTH*PIP_HEIGHT*4,VK_BUFFER_USAGE_TRANSFER_SRC_BIT,f.upload,f.uploadMemory,f.mapped)&&Buffer(d,8,VK_BUFFER_USAGE_TRANSFER_DST_BIT,f.read,f.readMemory,f.readMapped);
}
static void Barrier(Device&d,VkCommandBuffer cmd,VkImage image,VkImageLayout before,VkImageLayout after,VkAccessFlags src,VkAccessFlags dst){
    VkImageMemoryBarrier b={VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER};b.srcAccessMask=src;b.dstAccessMask=dst;b.oldLayout=before;b.newLayout=after;b.srcQueueFamilyIndex=b.dstQueueFamilyIndex=VK_QUEUE_FAMILY_IGNORED;b.image=image;b.subresourceRange={VK_IMAGE_ASPECT_COLOR_BIT,0,1,0,1};
    CALL(vkCmdPipelineBarrier)(cmd,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,1,&b);
}
static bool Record(Device&d,Swap&s,Frame&f,uint32_t index){
    if(CALL(vkGetFenceStatus)(d.handle,f.fence)!=VK_SUCCESS)return false;
    if(f.verify&&video){uint32_t actual=*(uint32_t*)f.readMapped;bool rgba=s.format==VK_FORMAT_R8G8B8A8_UNORM;if(rgba)actual=(actual&0xff00ff00)|((actual&255)<<16)|((actual>>16)&255);bool match=true;for(int c=0;c<3;c++)if(abs(int((actual>>(c*8))&255)-int((f.expected>>(c*8))&255))>20)match=false;if(f.verifyCorner){uint32_t corner=((uint32_t*)f.readMapped)[1];if(rgba)corner=(corner&0xff00ff00)|((corner&255)<<16)|((corner>>16)&255);bool filled=true;for(int c=0;c<3;c++)if(abs(int((corner>>(c*8))&255)-int((f.expected>>(c*8))&255))>20)filled=false;match=match&&!filled;}InterlockedIncrement(match?&video->verified:&video->mismatched);f.verify=false;}
    uint32_t w=f.width,h=f.height,seq=f.sequence;float x=f.x,y=f.y,bw=f.w,bh=f.h;
    bool valid=f.initialized,upload=false;bool verify=video->validate&&s.readable;
    // Contention skips the update, never the previously uploaded picture.
    if(TryLock(video)){
        w=video->width;h=video->height;seq=video->sequence;x=video->x;y=video->y;bw=video->w;bh=video->h;
        valid=w&&h&&w<=PIP_WIDTH&&h<=PIP_HEIGHT&&x>=0&&y>=0&&bw>0&&bh>0&&x+bw<=1.001f&&y+bh<=1.001f;
        upload=valid&&(!f.initialized||f.sequence!=seq);
        if(upload)memcpy(f.mapped,video->pixels,size_t(w)*h*4);
        if(valid){f.expected=*(uint32_t*)(video->pixels+(size_t(h/2)*w+w/2)*4);f.width=w;f.height=h;f.x=x;f.y=y;f.w=bw;f.h=bh;}
        Unlock(video);
    }
    if(!valid)return false;
    if(CALL(vkResetCommandPool)(d.handle,f.pool,0)!=VK_SUCCESS)return false;VkCommandBufferBeginInfo begin={VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};begin.flags=VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;if(CALL(vkBeginCommandBuffer)(f.command,&begin)!=VK_SUCCESS)return false;
    if(upload){
        Barrier(d,f.command,f.source,f.initialized?VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL:VK_IMAGE_LAYOUT_UNDEFINED,VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,f.initialized?VK_ACCESS_TRANSFER_READ_BIT:0,VK_ACCESS_TRANSFER_WRITE_BIT);
        VkBufferImageCopy copy={};copy.imageSubresource={VK_IMAGE_ASPECT_COLOR_BIT,0,0,1};copy.imageExtent={w,h,1};CALL(vkCmdCopyBufferToImage)(f.command,f.upload,f.source,VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,1,&copy);
        Barrier(d,f.command,f.source,VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,VK_ACCESS_TRANSFER_WRITE_BIT,VK_ACCESS_TRANSFER_READ_BIT);
    }
    Barrier(d,f.command,s.images[index],VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,0,VK_ACCESS_TRANSFER_WRITE_BIT);
    VkImageBlit blit={};blit.srcSubresource=blit.dstSubresource={VK_IMAGE_ASPECT_COLOR_BIT,0,0,1};blit.srcOffsets[1]={(int)w,(int)h,1};blit.dstOffsets[0]={(int)(x*s.size.width),(int)(y*s.size.height),0};blit.dstOffsets[1]={std::min((int)s.size.width,(int)((x+bw)*s.size.width)),std::min((int)s.size.height,(int)((y+bh)*s.size.height)),1};
    // Small rounded corners without introducing a shader pipeline into the Vulkan renderer.
    VkImageBlit regions[13];uint32_t regionCount=0;
    int dw=blit.dstOffsets[1].x-blit.dstOffsets[0].x,dh=blit.dstOffsets[1].y-blit.dstOffsets[0].y;
    int radius=std::min(6,std::min(dw,dh)/2);
    auto strip=[&](int top,int bottom,int inset){
        if(bottom<=top||dw<=2*inset)return;
        auto part=blit;part.dstOffsets[0].x+=inset;part.dstOffsets[1].x-=inset;
        part.dstOffsets[0].y=blit.dstOffsets[0].y+top;part.dstOffsets[1].y=blit.dstOffsets[0].y+bottom;
        part.srcOffsets[0].x=(int)std::lround(double(inset)*w/dw);part.srcOffsets[1].x=(int)std::lround(double(dw-inset)*w/dw);
        part.srcOffsets[0].y=(int)std::lround(double(top)*h/dh);part.srcOffsets[1].y=(int)std::lround(double(bottom)*h/dh);
        regions[regionCount++]=part;
    };
    for(int row=0;row<radius;row++){double dy=radius-row-.5;int inset=(int)std::ceil(radius-std::sqrt(radius*radius-dy*dy));strip(row,row+1,inset);strip(dh-row-1,dh-row,inset);}
    strip(radius,dh-radius,0);
    if(regionCount)CALL(vkCmdBlitImage)(f.command,f.source,VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,regionCount,regions,VK_FILTER_LINEAR);
    if(verify){
        Barrier(d,f.command,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,VK_ACCESS_TRANSFER_WRITE_BIT,VK_ACCESS_TRANSFER_READ_BIT);
        VkBufferImageCopy copy={};copy.imageSubresource={VK_IMAGE_ASPECT_COLOR_BIT,0,0,1};copy.imageOffset={(blit.dstOffsets[0].x+blit.dstOffsets[1].x)/2,(blit.dstOffsets[0].y+blit.dstOffsets[1].y)/2,0};copy.imageExtent={1,1,1};CALL(vkCmdCopyImageToBuffer)(f.command,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,f.read,1,&copy);
        f.verifyCorner=video->validate==2;if(f.verifyCorner){copy.bufferOffset=4;copy.imageOffset={blit.dstOffsets[0].x,blit.dstOffsets[0].y,0};CALL(vkCmdCopyImageToBuffer)(f.command,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,f.read,1,&copy);}
        Barrier(d,f.command,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,VK_ACCESS_TRANSFER_READ_BIT,0);
    }else Barrier(d,f.command,s.images[index],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,VK_IMAGE_LAYOUT_PRESENT_SRC_KHR,VK_ACCESS_TRANSFER_WRITE_BIT,0);
    if(CALL(vkEndCommandBuffer)(f.command)!=VK_SUCCESS)return false;
    f.verify=verify;f.initialized=true;f.sequence=seq;if(upload)InterlockedIncrement(&video->uploaded);return true;
}
EXPORT VkResult VKAPI_CALL vkCreateInstance(const VkInstanceCreateInfo* info,const VkAllocationCallbacks* alloc,VkInstance*out){
    auto chain=(VkLayerInstanceCreateInfo*)info->pNext;while(chain&&(chain->sType!=VK_STRUCTURE_TYPE_LOADER_INSTANCE_CREATE_INFO||chain->function!=VK_LAYER_LINK_INFO))chain=(VkLayerInstanceCreateInfo*)chain->pNext;if(!chain)return VK_ERROR_INITIALIZATION_FAILED;
    auto next=chain->u.pLayerInfo->pfnNextGetInstanceProcAddr;auto create=(PFN_vkCreateInstance)next(VK_NULL_HANDLE,"vkCreateInstance");chain->u.pLayerInfo=chain->u.pLayerInfo->pNext;
    VkResult result=create(info,alloc,out);if(result==VK_SUCCESS){std::lock_guard<std::recursive_mutex> lock(gate);instances[*out]={*out,next};}return result;
}
EXPORT VkResult VKAPI_CALL vkEnumeratePhysicalDevices(VkInstance handle,uint32_t*count,VkPhysicalDevice*out){
    std::lock_guard<std::recursive_mutex> lock(gate);auto i=instances.at(handle);auto result=((PFN_vkEnumeratePhysicalDevices)i.next(handle,"vkEnumeratePhysicalDevices"))(handle,count,out);if(out&&(result==VK_SUCCESS||result==VK_INCOMPLETE))for(uint32_t n=0;n<*count;n++)physicals[out[n]]=handle;return result;
}
static VkResult EnumerateGroups(VkInstance handle,uint32_t*count,VkPhysicalDeviceGroupProperties*out,const char*name){
    std::lock_guard<std::recursive_mutex> lock(gate);auto i=instances.at(handle);
    auto next=(PFN_vkEnumeratePhysicalDeviceGroups)i.next(handle,name);if(!next)return VK_ERROR_EXTENSION_NOT_PRESENT;
    auto result=next(handle,count,out);
    if(out&&(result==VK_SUCCESS||result==VK_INCOMPLETE))for(uint32_t n=0;n<*count;n++)for(uint32_t p=0;p<out[n].physicalDeviceCount;p++)physicals[out[n].physicalDevices[p]]=handle;
    return result;
}
EXPORT VkResult VKAPI_CALL vkEnumeratePhysicalDeviceGroups(VkInstance handle,uint32_t*count,VkPhysicalDeviceGroupProperties*out){return EnumerateGroups(handle,count,out,"vkEnumeratePhysicalDeviceGroups");}
EXPORT VkResult VKAPI_CALL vkEnumeratePhysicalDeviceGroupsKHR(VkInstance handle,uint32_t*count,VkPhysicalDeviceGroupProperties*out){return EnumerateGroups(handle,count,out,"vkEnumeratePhysicalDeviceGroupsKHR");}
EXPORT VkResult VKAPI_CALL vkCreateDevice(VkPhysicalDevice physical,const VkDeviceCreateInfo*info,const VkAllocationCallbacks*alloc,VkDevice*out){
    std::lock_guard<std::recursive_mutex> lock(gate);
    auto chain=(VkLayerDeviceCreateInfo*)info->pNext;PFN_vkSetDeviceLoaderData loaderData=nullptr;
    for(auto node=chain;node;node=(VkLayerDeviceCreateInfo*)node->pNext)if(node->sType==VK_STRUCTURE_TYPE_LOADER_DEVICE_CREATE_INFO&&node->function==VK_LOADER_DATA_CALLBACK)loaderData=node->u.pfnSetDeviceLoaderData;
    while(chain&&(chain->sType!=VK_STRUCTURE_TYPE_LOADER_DEVICE_CREATE_INFO||chain->function!=VK_LAYER_LINK_INFO))chain=(VkLayerDeviceCreateInfo*)chain->pNext;if(!chain)return VK_ERROR_INITIALIZATION_FAILED;
    auto gi=chain->u.pLayerInfo->pfnNextGetInstanceProcAddr;auto gd=chain->u.pLayerInfo->pfnNextGetDeviceProcAddr;chain->u.pLayerInfo=chain->u.pLayerInfo->pNext;
    auto found=physicals.find(physical);if(found==physicals.end())return VK_ERROR_INITIALIZATION_FAILED;VkInstance instance=found->second;
    auto result=((PFN_vkCreateDevice)gi(instance,"vkCreateDevice"))(physical,info,alloc,out);
    if(result==VK_SUCCESS){auto state=std::make_unique<Device>();auto &d=*state;d.handle=*out;d.physical=physical;d.instance=instances.at(instance);d.next=gd;d.loaderData=loaderData;ICALL(vkGetPhysicalDeviceMemoryProperties)(physical,&d.memory);uint32_t count=0;ICALL(vkGetPhysicalDeviceQueueFamilyProperties)(physical,&count,nullptr);d.families.resize(count);ICALL(vkGetPhysicalDeviceQueueFamilyProperties)(physical,&count,d.families.data());devices[*out]=std::move(state);}return result;
}
EXPORT void VKAPI_CALL vkGetDeviceQueue(VkDevice handle,uint32_t family,uint32_t index,VkQueue*out){std::lock_guard<std::recursive_mutex>lock(gate);auto&d=*devices.at(handle);CALL(vkGetDeviceQueue)(handle,family,index,out);queues[*out]={&d,family};}
EXPORT void VKAPI_CALL vkGetDeviceQueue2(VkDevice handle,const VkDeviceQueueInfo2*info,VkQueue*out){std::lock_guard<std::recursive_mutex>lock(gate);auto&d=*devices.at(handle);CALL(vkGetDeviceQueue2)(handle,info,out);if(*out)queues[*out]={&d,info->queueFamilyIndex,(info->flags&VK_DEVICE_QUEUE_CREATE_PROTECTED_BIT)!=0};}
EXPORT VkResult VKAPI_CALL vkCreateSwapchainKHR(VkDevice handle,const VkSwapchainCreateInfoKHR*info,const VkAllocationCallbacks*alloc,VkSwapchainKHR*out){
    std::lock_guard<std::recursive_mutex>lock(gate);auto&d=*devices.at(handle);auto copy=*info;VkSurfaceCapabilitiesKHR caps={};VkFormatProperties source={},dest={};
    bool enabled=ICALL(vkGetPhysicalDeviceSurfaceCapabilitiesKHR)(d.physical,info->surface,&caps)==VK_SUCCESS&&(caps.supportedUsageFlags&VK_IMAGE_USAGE_TRANSFER_DST_BIT)&&!(info->flags&VK_SWAPCHAIN_CREATE_PROTECTED_BIT_KHR)&&info->imageArrayLayers==1;
    ICALL(vkGetPhysicalDeviceFormatProperties)(d.physical,VK_FORMAT_B8G8R8A8_UNORM,&source);ICALL(vkGetPhysicalDeviceFormatProperties)(d.physical,info->imageFormat,&dest);
    enabled=enabled&&(source.optimalTilingFeatures&VK_FORMAT_FEATURE_BLIT_SRC_BIT)&&(source.optimalTilingFeatures&VK_FORMAT_FEATURE_SAMPLED_IMAGE_FILTER_LINEAR_BIT)&&(dest.optimalTilingFeatures&VK_FORMAT_FEATURE_BLIT_DST_BIT);
    // SDR only: do not reinterpret HDR/PQ images as ordinary video RGB.
    enabled=enabled&&info->imageColorSpace==VK_COLOR_SPACE_SRGB_NONLINEAR_KHR&&(info->imageFormat==VK_FORMAT_B8G8R8A8_UNORM||info->imageFormat==VK_FORMAT_R8G8B8A8_UNORM);
    bool readable=enabled&&(caps.supportedUsageFlags&VK_IMAGE_USAGE_TRANSFER_SRC_BIT);if(enabled)copy.imageUsage|=VK_IMAGE_USAGE_TRANSFER_DST_BIT;if(readable)copy.imageUsage|=VK_IMAGE_USAGE_TRANSFER_SRC_BIT;
    auto result=CALL(vkCreateSwapchainKHR)(handle,&copy,alloc,out);if(result!=VK_SUCCESS&&enabled){enabled=readable=false;result=CALL(vkCreateSwapchainKHR)(handle,info,alloc,out);}
    if(result==VK_SUCCESS){Swap s;s.handle=*out;s.size=info->imageExtent;s.format=info->imageFormat;s.enabled=enabled;s.readable=readable;uint32_t count=0;if(CALL(vkGetSwapchainImagesKHR)(handle,*out,&count,nullptr)==VK_SUCCESS){s.images.resize(count);CALL(vkGetSwapchainImagesKHR)(handle,*out,&count,s.images.data());s.frames.resize(count);}d.swaps[*out]=std::move(s);}return result;
}
EXPORT VkResult VKAPI_CALL vkQueuePresentKHR(VkQueue queue,const VkPresentInfoKHR*info){
    std::lock_guard<std::recursive_mutex>lock(gate);auto entry=queues.at(queue);auto&d=*entry.device;auto original=CALL(vkQueuePresentKHR);
    // Preserve extension chains and multi-swapchain presentations unchanged.
    if(entry.protectedQueue||info->swapchainCount!=1||info->pNext||!OpenVideo())return original(queue,info);
    auto found=d.swaps.find(info->pSwapchains[0]);if(found==d.swaps.end())return original(queue,info);auto&s=found->second;
    if(!s.enabled||entry.family>=d.families.size()||!(d.families[entry.family].queueFlags&VK_QUEUE_GRAPHICS_BIT))return original(queue,info);
    video->ready=1;video->renderWidth=s.size.width;video->renderHeight=s.size.height;
    if(!video->active||DWORD(GetTickCount()-video->heartbeat)>2000)return original(queue,info);
    uint32_t index=info->pImageIndices[0];if(index>=s.frames.size()||(s.family!=UINT32_MAX&&s.family!=entry.family))return original(queue,info);s.family=entry.family;auto&f=s.frames[index];
    if(!f.pool&&!InitFrame(d,f,s.family)){FreeFrame(d,f);s.enabled=false;return original(queue,info);}
    if(!Record(d,s,f,index))return original(queue,info);
    std::vector<VkPipelineStageFlags> stages(info->waitSemaphoreCount,VK_PIPELINE_STAGE_TRANSFER_BIT);VkSubmitInfo submit={VK_STRUCTURE_TYPE_SUBMIT_INFO};submit.waitSemaphoreCount=info->waitSemaphoreCount;submit.pWaitSemaphores=info->pWaitSemaphores;submit.pWaitDstStageMask=stages.data();submit.commandBufferCount=1;submit.pCommandBuffers=&f.command;submit.signalSemaphoreCount=1;submit.pSignalSemaphores=&f.done;
    CALL(vkResetFences)(d.handle,1,&f.fence);auto result=CALL(vkQueueSubmit)(queue,1,&submit,f.fence);if(result!=VK_SUCCESS){s.enabled=false;return result;}
    InterlockedIncrement(&video->rendered);auto present=*info;present.waitSemaphoreCount=1;present.pWaitSemaphores=&f.done;return original(queue,&present);
}
EXPORT void VKAPI_CALL vkDestroySwapchainKHR(VkDevice handle,VkSwapchainKHR swap,const VkAllocationCallbacks*alloc){std::lock_guard<std::recursive_mutex>lock(gate);auto&d=*devices.at(handle);auto found=d.swaps.find(swap);if(found!=d.swaps.end()){CALL(vkDeviceWaitIdle)(handle);for(auto&f:found->second.frames)FreeFrame(d,f);d.swaps.erase(found);}CALL(vkDestroySwapchainKHR)(handle,swap,alloc);}
EXPORT void VKAPI_CALL vkDestroyDevice(VkDevice handle,const VkAllocationCallbacks*alloc){std::lock_guard<std::recursive_mutex>lock(gate);auto&d=*devices.at(handle);CALL(vkDeviceWaitIdle)(handle);for(auto&s:d.swaps)for(auto&f:s.second.frames)FreeFrame(d,f);for(auto it=queues.begin();it!=queues.end();)if(it->second.device==&d)it=queues.erase(it);else ++it;auto destroy=CALL(vkDestroyDevice);devices.erase(handle);destroy(handle,alloc);}
EXPORT void VKAPI_CALL vkDestroyInstance(VkInstance handle,const VkAllocationCallbacks*alloc){std::lock_guard<std::recursive_mutex>lock(gate);auto instance=instances.at(handle);for(auto it=physicals.begin();it!=physicals.end();)if(it->second==handle)it=physicals.erase(it);else ++it;instances.erase(handle);((PFN_vkDestroyInstance)instance.next(handle,"vkDestroyInstance"))(handle,alloc);}
#define MATCH(name) if(!strcmp(nameString,#name))return (PFN_vkVoidFunction)name
EXPORT PFN_vkVoidFunction VKAPI_CALL vkGetDeviceProcAddr(VkDevice handle,const char*nameString){
    MATCH(vkGetDeviceProcAddr);MATCH(vkGetDeviceQueue);MATCH(vkGetDeviceQueue2);MATCH(vkCreateSwapchainKHR);MATCH(vkQueuePresentKHR);MATCH(vkDestroySwapchainKHR);MATCH(vkDestroyDevice);
    std::lock_guard<std::recursive_mutex>lock(gate);auto it=devices.find(handle);return it==devices.end()?nullptr:it->second->next(handle,nameString);
}
EXPORT PFN_vkVoidFunction VKAPI_CALL vkGetInstanceProcAddr(VkInstance handle,const char*nameString){
    MATCH(vkGetInstanceProcAddr);MATCH(vkCreateInstance);MATCH(vkEnumeratePhysicalDevices);MATCH(vkEnumeratePhysicalDeviceGroups);MATCH(vkEnumeratePhysicalDeviceGroupsKHR);MATCH(vkCreateDevice);MATCH(vkDestroyInstance);
    MATCH(vkGetDeviceProcAddr);MATCH(vkGetDeviceQueue);MATCH(vkGetDeviceQueue2);MATCH(vkCreateSwapchainKHR);MATCH(vkQueuePresentKHR);MATCH(vkDestroySwapchainKHR);MATCH(vkDestroyDevice);
    std::lock_guard<std::recursive_mutex>lock(gate);auto it=instances.find(handle);return it==instances.end()?nullptr:it->second.next(handle,nameString);
}
EXPORT VkResult VKAPI_CALL vkNegotiateLoaderLayerInterfaceVersion(VkNegotiateLayerInterface*version){version->loaderLayerInterfaceVersion=std::min(version->loaderLayerInterfaceVersion,2u);version->pfnGetInstanceProcAddr=vkGetInstanceProcAddr;version->pfnGetDeviceProcAddr=vkGetDeviceProcAddr;version->pfnGetPhysicalDeviceProcAddr=nullptr;return VK_SUCCESS;}
