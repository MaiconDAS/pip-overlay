using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace PipOverlay;

internal static class GameLauncher
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    static extern uint GetShortPathName(string path,StringBuilder result,uint capacity);
    static string LayerPath(string path){var value=new StringBuilder(32768);uint count=GetShortPathName(path,value,(uint)value.Capacity);return count>0&&count<value.Capacity?value.ToString():path;}
    static readonly Dictionary<int,(long Started,bool Is32)> launched=new();
    sealed record ActiveGame(int Pid,long Started,bool Is32,string Executable);
    static bool restored;
    internal static RenderTarget[] Targets(){
        if(!restored){restored=true;string folder=Path.Combine(AppContext.BaseDirectory,"data","games");
            if(Directory.Exists(folder))foreach(string file in Directory.EnumerateFiles(folder,"active.json",SearchOption.AllDirectories))try{
                var saved=System.Text.Json.JsonSerializer.Deserialize<ActiveGame>(File.ReadAllText(file));if(saved==null)continue;
                using var game=Process.GetProcessById(saved.Pid);
                if(game.StartTime.ToUniversalTime().Ticks==saved.Started&&string.Equals(game.MainModule?.FileName,saved.Executable,StringComparison.OrdinalIgnoreCase))launched[saved.Pid]=(saved.Started,saved.Is32);
            }catch(Exception ex)when(ex is IOException or ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException){}
        }
        var found=new List<RenderTarget>();
        foreach(var pair in launched.ToArray())try{
            using var process=Process.GetProcessById(pair.Key);
            if(process.HasExited||process.StartTime.ToUniversalTime().Ticks!=pair.Value.Started){launched.Remove(pair.Key);continue;}
            found.Add(new(process.Id,process.ProcessName,pair.Value.Is32,true));
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){launched.Remove(pair.Key);}
        return found.ToArray();
    }
    internal static bool Is32Bit(string path){
        using var stream=File.OpenRead(path);using var reader=new BinaryReader(stream);
        if(reader.ReadUInt16()!=0x5a4d)throw new InvalidDataException("Escolha um executável Windows.");
        stream.Position=0x3c;int pe=reader.ReadInt32();
        if(pe<0||pe>stream.Length-6)throw new InvalidDataException("Executável inválido.");
        stream.Position=pe;if(reader.ReadUInt32()!=0x4550)throw new InvalidDataException("Executável inválido.");
        return reader.ReadUInt16() switch{0x14c=>true,0x8664=>false,_=>throw new NotSupportedException("Arquitetura não suportada.")};
    }
    internal static string Prepare(string executable){
        bool x86=Is32Bit(executable);int bits=x86?32:64;
        string root=AppContext.BaseDirectory;
        string id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())))[..16];
        string session=Path.Combine(root,"data","games",id);
        Directory.CreateDirectory(session);
        foreach(string file in new[]{$"ReShade{bits}.dll",$"PipOverlay.addon{bits}",$"PipVulkan{bits}.dll","LICENSE-RESHADE.md"})
            File.Copy(Path.Combine(root,"graphics",file),Path.Combine(session,file),true);
        File.Copy(Path.Combine(root,"effects","PipOverlay.fx"),Path.Combine(session,"PipOverlay.fx"),true);
        File.Copy(Path.Combine(root,"graphics",$"ReShade{bits}.json"),Path.Combine(session,$"ReShade{bits}.json"),true);
        File.WriteAllText(Path.Combine(session,"PiPVulkan.json"),System.Text.Json.JsonSerializer.Serialize(new{
            file_format_version="1.2.0",layer=new{name="VK_LAYER_PIP_overlay",type="GLOBAL",library_path=$".\\PipVulkan{bits}.dll",api_version="1.3.0",implementation_version="1",description="PiP Overlay video compositor"}
        }));
        File.WriteAllText(Path.Combine(session,"ReShade.ini"),"""
            [ADDON]
            AddonPath=.
            DisabledAddons=Generic Depth
            [GENERAL]
            EffectSearchPaths=.
            IntermediateCachePath=cache
            PresetPath=PiP.ini
            PerformanceMode=0
            SkipLoadingDisabledEffects=0
            [INPUT]
            KeyOverlay=0,0,0,0
            KeyEffects=0,0,0,0
            KeyScreenshot=0,0,0,0
            InputProcessing=0
            GamepadNavigation=0
            [OVERLAY]
            TutorialProgress=4
            ShowFPS=0
            ShowClock=0
            ShowFrameTime=0
            """);
        File.WriteAllText(Path.Combine(session,"PiP.ini"),"Techniques=PiPOverlay@PipOverlay.fx\nTechniqueSorting=PiPOverlay@PipOverlay.fx\n");
        return session;
    }
    internal static async Task<RenderTarget> Launch(string executable,string arguments=""){
        executable=Path.GetFullPath(executable);
        foreach(var existing in Targets())try{
            using var running=Process.GetProcessById(existing.Pid);
            if(string.Equals(running.MainModule?.FileName,executable,StringComparison.OrdinalIgnoreCase))return existing;
        }catch(Exception ex)when(ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception){}
        string session=Prepare(executable);
        bool x86=Is32Bit(executable);int bits=x86?32:64;
        string pidFile=Path.Combine(session,"active.pid");
        var start=new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory,"graphics",$"PipLaunch{bits}.exe")){UseShellExecute=false,CreateNoWindow=true};
        start.ArgumentList.Add(executable);start.ArgumentList.Add(Path.Combine(session,$"ReShade{bits}.dll"));start.ArgumentList.Add(pidFile);start.ArgumentList.Add(arguments);
        start.Environment["RESHADE_BASE_PATH_OVERRIDE"]=session;
        start.Environment["RESHADE_DISABLE_INPUT_HOOK"]="1";
        // Vulkan discovery is private to this child process; no registry or machine variables.
        start.Environment.TryGetValue("VK_LAYER_PATH",out string? previous);
        start.Environment["VK_LAYER_PATH"]=LayerPath(session)+(string.IsNullOrEmpty(previous)?"":";"+previous);
        start.Environment.TryGetValue("VK_INSTANCE_LAYERS",out string? layers);
        start.Environment["VK_INSTANCE_LAYERS"]="VK_LAYER_PIP_overlay"+(string.IsNullOrEmpty(layers)?"":";"+layers);
        using var helper=Process.Start(start)??throw new IOException("Não foi possível abrir o jogo.");
        await helper.WaitForExitAsync();
        if(helper.ExitCode!=0)throw new IOException($"Não foi possível preparar seu jogo (código {helper.ExitCode}).");
        int pid=int.Parse(File.ReadAllText(pidFile));using var game=Process.GetProcessById(pid);
        launched[pid]=(game.StartTime.ToUniversalTime().Ticks,x86);
        File.WriteAllText(Path.Combine(session,"active.json"),System.Text.Json.JsonSerializer.Serialize(new ActiveGame(pid,launched[pid].Started,x86,executable)));
        return new(pid,game.ProcessName,x86,true);
    }
}
