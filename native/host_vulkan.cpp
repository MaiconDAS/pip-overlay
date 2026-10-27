#define VK_USE_PLATFORM_WIN32_KHR
#define VK_NO_PROTOTYPES
#include <windows.h>
#include <vulkan/vulkan.h>
#include <vector>
#include <algorithm>
#include <stdio.h>
static bool running=true;
static LRESULT CALLBACK Proc(HWND h,UINT m,WPARAM w,LPARAM l){if(m==WM_CLOSE){running=false;return 0;}return DefWindowProcW(h,m,w,l);}
#define GLOBAL(name) auto name=(PFN_##name)GetProcAddress(loader,#name)
#define INSTANCE(name) auto name=(PFN_##name)vkGetInstanceProcAddr(instance,#name)
#define DEVICE(name) auto name=(PFN_##name)vkGetDeviceProcAddr(device,#name)
#define CHECK(call) if((call)!=VK_SUCCESS)return __LINE__
int WINAPI WinMain(HINSTANCE app,HINSTANCE,LPSTR,int){
    WNDCLASSW wc={};wc.hInstance=app;wc.lpfnWndProc=Proc;wc.lpszClassName=L"PiPVulkanTest";RegisterClassW(&wc);
    HWND window=CreateWindowW(wc.lpszClassName,L"PiP Vulkan — validação isolada",WS_OVERLAPPEDWINDOW,80,80,960,600,nullptr,nullptr,app,nullptr);
    HMODULE loader=LoadLibraryW(L"vulkan-1.dll");if(!loader)return 1;
    GLOBAL(vkGetInstanceProcAddr);GLOBAL(vkCreateInstance);
    GLOBAL(vkEnumerateInstanceLayerProperties);
    wchar_t logPath[32768];GetModuleFileNameW(nullptr,logPath,32768);wcscpy(wcsrchr(logPath,L'\\')+1,L"vulkan-loader.txt");FILE* log=_wfopen(logPath,L"w");
    wchar_t env[32768];GetEnvironmentVariableW(L"VK_LAYER_PATH",env,32768);if(log)fwprintf(log,L"VK_LAYER_PATH=%s\n",env);
    uint32_t layerCount=0;vkEnumerateInstanceLayerProperties(&layerCount,nullptr);std::vector<VkLayerProperties> available(layerCount);vkEnumerateInstanceLayerProperties(&layerCount,available.data());
    for(auto &layer:available)if(log)fprintf(log,"layer=%s\n",layer.layerName);if(log)fclose(log);
    const char* extensions[]={VK_KHR_SURFACE_EXTENSION_NAME,VK_KHR_WIN32_SURFACE_EXTENSION_NAME};
    VkApplicationInfo application={VK_STRUCTURE_TYPE_APPLICATION_INFO};application.pApplicationName="PiP test";application.apiVersion=VK_API_VERSION_1_3;
    VkInstanceCreateInfo ic={VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO};ic.pApplicationInfo=&application;ic.enabledExtensionCount=2;ic.ppEnabledExtensionNames=extensions;
    VkInstance instance;CHECK(vkCreateInstance(&ic,nullptr,&instance));
    INSTANCE(vkCreateWin32SurfaceKHR);INSTANCE(vkEnumeratePhysicalDevices);INSTANCE(vkGetPhysicalDeviceQueueFamilyProperties);INSTANCE(vkGetPhysicalDeviceSurfaceSupportKHR);INSTANCE(vkGetPhysicalDeviceSurfaceCapabilitiesKHR);INSTANCE(vkGetPhysicalDeviceSurfaceFormatsKHR);INSTANCE(vkCreateDevice);INSTANCE(vkGetDeviceProcAddr);INSTANCE(vkDestroySurfaceKHR);INSTANCE(vkDestroyInstance);
    VkWin32SurfaceCreateInfoKHR sc={VK_STRUCTURE_TYPE_WIN32_SURFACE_CREATE_INFO_KHR};sc.hinstance=app;sc.hwnd=window;
    VkSurfaceKHR surface;CHECK(vkCreateWin32SurfaceKHR(instance,&sc,nullptr,&surface));
    uint32_t count=0;CHECK(vkEnumeratePhysicalDevices(instance,&count,nullptr));if(!count)return 2;
    std::vector<VkPhysicalDevice> devices(count);CHECK(vkEnumeratePhysicalDevices(instance,&count,devices.data()));
    VkPhysicalDevice physical=VK_NULL_HANDLE;uint32_t family=0;
    for(auto candidate:devices){uint32_t n=0;vkGetPhysicalDeviceQueueFamilyProperties(candidate,&n,nullptr);std::vector<VkQueueFamilyProperties> families(n);vkGetPhysicalDeviceQueueFamilyProperties(candidate,&n,families.data());for(uint32_t i=0;i<n;i++){VkBool32 present=0;vkGetPhysicalDeviceSurfaceSupportKHR(candidate,i,surface,&present);if(present&&(families[i].queueFlags&VK_QUEUE_GRAPHICS_BIT)){physical=candidate;family=i;break;}}if(physical)break;}
    if(!physical)return 3;
    INSTANCE(vkGetPhysicalDeviceFeatures);INSTANCE(vkGetPhysicalDeviceProperties);
    VkPhysicalDeviceFeatures features;vkGetPhysicalDeviceFeatures(physical,&features);VkPhysicalDeviceProperties properties;vkGetPhysicalDeviceProperties(physical,&properties);
    log=_wfopen(logPath,L"a");if(log){fprintf(log,"GPU=%s readWithoutFormat=%u writeWithoutFormat=%u gather=%u anisotropy=%u\n",properties.deviceName,features.shaderStorageImageReadWithoutFormat,features.shaderStorageImageWriteWithoutFormat,features.shaderImageGatherExtended,features.samplerAnisotropy);fclose(log);}
    float priority=1;VkDeviceQueueCreateInfo qi={VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO};qi.queueFamilyIndex=family;qi.queueCount=1;qi.pQueuePriorities=&priority;
    const char* swapExtension=VK_KHR_SWAPCHAIN_EXTENSION_NAME;VkDeviceCreateInfo di={VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO};di.queueCreateInfoCount=1;di.pQueueCreateInfos=&qi;di.enabledExtensionCount=1;di.ppEnabledExtensionNames=&swapExtension;
    VkDevice device;CHECK(vkCreateDevice(physical,&di,nullptr,&device));
    DEVICE(vkGetDeviceQueue);DEVICE(vkCreateSwapchainKHR);DEVICE(vkGetSwapchainImagesKHR);DEVICE(vkCreateCommandPool);DEVICE(vkAllocateCommandBuffers);DEVICE(vkCreateSemaphore);DEVICE(vkCreateFence);DEVICE(vkWaitForFences);DEVICE(vkResetFences);DEVICE(vkResetCommandPool);DEVICE(vkAcquireNextImageKHR);DEVICE(vkBeginCommandBuffer);DEVICE(vkCmdPipelineBarrier);DEVICE(vkCmdClearColorImage);DEVICE(vkEndCommandBuffer);DEVICE(vkQueueSubmit);DEVICE(vkQueuePresentKHR);DEVICE(vkDeviceWaitIdle);DEVICE(vkDestroyFence);DEVICE(vkDestroySemaphore);DEVICE(vkDestroyCommandPool);DEVICE(vkDestroySwapchainKHR);DEVICE(vkDestroyDevice);
    VkQueue queue;vkGetDeviceQueue(device,family,0,&queue);
    VkSurfaceCapabilitiesKHR caps;CHECK(vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physical,surface,&caps));
    CHECK(vkGetPhysicalDeviceSurfaceFormatsKHR(physical,surface,&count,nullptr));std::vector<VkSurfaceFormatKHR> formats(count);CHECK(vkGetPhysicalDeviceSurfaceFormatsKHR(physical,surface,&count,formats.data()));
    auto format=formats[0];for(auto f:formats)if(f.format==VK_FORMAT_B8G8R8A8_UNORM)format=f;
    VkSwapchainCreateInfoKHR si={VK_STRUCTURE_TYPE_SWAPCHAIN_CREATE_INFO_KHR};si.surface=surface;si.minImageCount=std::max(2u,caps.minImageCount);if(caps.maxImageCount)si.minImageCount=std::min(si.minImageCount,caps.maxImageCount);si.imageFormat=format.format;si.imageColorSpace=format.colorSpace;si.imageExtent=caps.currentExtent.width==UINT32_MAX?VkExtent2D{960,600}:caps.currentExtent;si.imageArrayLayers=1;si.imageUsage=VK_IMAGE_USAGE_TRANSFER_DST_BIT|VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT;si.imageSharingMode=VK_SHARING_MODE_EXCLUSIVE;si.preTransform=caps.currentTransform;si.compositeAlpha=VK_COMPOSITE_ALPHA_OPAQUE_BIT_KHR;si.presentMode=VK_PRESENT_MODE_FIFO_KHR;si.clipped=VK_TRUE;
    if((caps.supportedUsageFlags&si.imageUsage)!=si.imageUsage)return 4;
    VkSwapchainKHR swap;CHECK(vkCreateSwapchainKHR(device,&si,nullptr,&swap));CHECK(vkGetSwapchainImagesKHR(device,swap,&count,nullptr));std::vector<VkImage> images(count);CHECK(vkGetSwapchainImagesKHR(device,swap,&count,images.data()));
    VkCommandPoolCreateInfo pi={VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO};pi.queueFamilyIndex=family;VkCommandPool pool;CHECK(vkCreateCommandPool(device,&pi,nullptr,&pool));
    VkCommandBufferAllocateInfo ai={VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO};ai.commandPool=pool;ai.level=VK_COMMAND_BUFFER_LEVEL_PRIMARY;ai.commandBufferCount=1;VkCommandBuffer command;CHECK(vkAllocateCommandBuffers(device,&ai,&command));
    VkSemaphoreCreateInfo sm={VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO};VkSemaphore acquired;CHECK(vkCreateSemaphore(device,&sm,nullptr,&acquired));std::vector<VkSemaphore> complete(count);for(auto &s:complete){CHECK(vkCreateSemaphore(device,&sm,nullptr,&s));}
    VkFenceCreateInfo fi={VK_STRUCTURE_TYPE_FENCE_CREATE_INFO};fi.flags=VK_FENCE_CREATE_SIGNALED_BIT;VkFence fence;CHECK(vkCreateFence(device,&fi,nullptr,&fence));
    ShowWindow(window,SW_SHOWNOACTIVATE);DWORD start=GetTickCount();MSG msg={};
    while(running&&GetTickCount()-start<35000){
        while(PeekMessageW(&msg,nullptr,0,0,PM_REMOVE)){TranslateMessage(&msg);DispatchMessageW(&msg);}if(!running)break;
        CHECK(vkWaitForFences(device,1,&fence,VK_TRUE,5000000000));uint32_t index;VkResult acquiredResult=vkAcquireNextImageKHR(device,swap,5000000000,acquired,VK_NULL_HANDLE,&index);if(acquiredResult!=VK_SUCCESS&&acquiredResult!=VK_SUBOPTIMAL_KHR)break;
        CHECK(vkResetFences(device,1,&fence));CHECK(vkResetCommandPool(device,pool,0));VkCommandBufferBeginInfo begin={VK_STRUCTURE_TYPE_COMMAND_BUFFER_BEGIN_INFO};begin.flags=VK_COMMAND_BUFFER_USAGE_ONE_TIME_SUBMIT_BIT;CHECK(vkBeginCommandBuffer(command,&begin));
        VkImageMemoryBarrier barrier={VK_STRUCTURE_TYPE_IMAGE_MEMORY_BARRIER};barrier.oldLayout=VK_IMAGE_LAYOUT_UNDEFINED;barrier.newLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;barrier.srcQueueFamilyIndex=barrier.dstQueueFamilyIndex=VK_QUEUE_FAMILY_IGNORED;barrier.image=images[index];barrier.subresourceRange={VK_IMAGE_ASPECT_COLOR_BIT,0,1,0,1};barrier.dstAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;
        vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT,VK_PIPELINE_STAGE_TRANSFER_BIT,0,0,nullptr,0,nullptr,1,&barrier);VkClearColorValue color={{0.035f,0.06f,0.12f,1}};vkCmdClearColorImage(command,images[index],VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,&color,1,&barrier.subresourceRange);
        barrier.oldLayout=VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL;barrier.newLayout=VK_IMAGE_LAYOUT_PRESENT_SRC_KHR;barrier.srcAccessMask=VK_ACCESS_TRANSFER_WRITE_BIT;barrier.dstAccessMask=0;vkCmdPipelineBarrier(command,VK_PIPELINE_STAGE_TRANSFER_BIT,VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT,0,0,nullptr,0,nullptr,1,&barrier);CHECK(vkEndCommandBuffer(command));
        VkPipelineStageFlags wait=VK_PIPELINE_STAGE_TRANSFER_BIT;VkSubmitInfo submit={VK_STRUCTURE_TYPE_SUBMIT_INFO};submit.waitSemaphoreCount=1;submit.pWaitSemaphores=&acquired;submit.pWaitDstStageMask=&wait;submit.commandBufferCount=1;submit.pCommandBuffers=&command;submit.signalSemaphoreCount=1;submit.pSignalSemaphores=&complete[index];CHECK(vkQueueSubmit(queue,1,&submit,fence));
        VkPresentInfoKHR present={VK_STRUCTURE_TYPE_PRESENT_INFO_KHR};present.waitSemaphoreCount=1;present.pWaitSemaphores=&complete[index];present.swapchainCount=1;present.pSwapchains=&swap;present.pImageIndices=&index;auto result=vkQueuePresentKHR(queue,&present);if(result!=VK_SUCCESS&&result!=VK_SUBOPTIMAL_KHR)break;
    }
    vkDeviceWaitIdle(device);vkDestroyFence(device,fence,nullptr);vkDestroySemaphore(device,acquired,nullptr);for(auto s:complete)vkDestroySemaphore(device,s,nullptr);vkDestroyCommandPool(device,pool,nullptr);vkDestroySwapchainKHR(device,swap,nullptr);vkDestroyDevice(device,nullptr);vkDestroySurfaceKHR(instance,surface,nullptr);vkDestroyInstance(instance,nullptr);DestroyWindow(window);FreeLibrary(loader);return 0;
}
