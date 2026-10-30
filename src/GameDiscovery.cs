using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PipOverlay;

// Discovery is read-only. A graphics module is evidence of a renderer, not a
// guarantee that a game allows an overlay. Never load a renderer during scanning.
internal static class GameDiscovery
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool IsWow64Process(nint process,out bool wow);
    static long scanned;
    static string configured="";
    static RenderTarget[] cached=[];
    internal static string? Renderer(IEnumerable<string> modules){
        var names=new HashSet<string>(modules,StringComparer.OrdinalIgnoreCase);
        if(names.Contains("vulkan-1.dll"))return "Vulkan";
        if(names.Contains("d3d12.dll"))return "DirectX 12";
        if(names.Contains("d3d11.dll"))return "DirectX 11";
        if(names.Contains("opengl32.dll"))return "OpenGL";
        return names.Contains("d3d9.dll")?"DirectX 9":null;
    }
    internal static RenderTarget[] Find(string executable="",bool force=false){
        if(!force&&configured==executable&&Environment.TickCount64-scanned<5000)return cached;
        configured=executable;scanned=Environment.TickCount64;
        var pids=new HashSet<int>();
        Native.EnumWindows((window,_)=>{
            if(Native.IsWindowVisible(window)&&Native.GetWindow(window,4)==0){
                int pid=(int)Native.ProcessId(window);
                if(pid!=Environment.ProcessId)pids.Add(pid);
            }
            return true;
        },0);
        var found=new List<RenderTarget>();
        foreach(int pid in pids)try{
            using var process=Process.GetProcessById(pid);
            string name=process.ProcessName;
            if(new[]{"msedge","chrome","explorer","ApplicationFrameHost","ShellExperienceHost","StartMenuExperienceHost","SearchHost","dwm"}.Contains(name,StringComparer.OrdinalIgnoreCase))continue;
            bool wow=false;string? renderer=null;string? unavailable=null;
            try{
                if(!IsWow64Process(process.Handle,out wow))throw new System.ComponentModel.Win32Exception();
                renderer=Renderer(process.Modules.Cast<ProcessModule>().Select(m=>m.ModuleName));
                bool selected=!string.IsNullOrEmpty(executable)&&string.Equals(process.MainModule?.FileName,executable,StringComparison.OrdinalIgnoreCase);
                if(renderer==null&&!selected)continue;
                if(renderer!="DirectX 9")unavailable=$"{renderer??"Renderizador não identificado"} · integração não iniciada. Configure o executável e use Iniciar jogo configurado no ícone perto do relógio. Jogos com proteção podem impedir a integração.";
            }catch(System.ComponentModel.Win32Exception){
                unavailable="O Windows ou a proteção do jogo não permitiu consultar este processo. A detecção não confirma compatibilidade com a overlay.";
            }
            found.Add(new(pid,name,wow,false,unavailable));
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){}
        return cached=found.OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.Pid).ToArray();
    }
}
