using System.Text.Json;
using System.Text.RegularExpressions;
namespace PipOverlay;

internal sealed record PreferencesBundle(int Format,Settings Settings,Dictionary<string,GameProfile> Profiles);
internal static class PreferencesArchive
{
    const int MaximumBytes=4*1024*1024;
    static readonly JsonSerializerOptions Json=new(){WriteIndented=true};
    internal static PreferencesBundle Read(string file){
        using var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
        if(stream.Length>MaximumBytes)throw new InvalidDataException(L.T("Arquivo de preferências inválido."));
        var bundle=JsonSerializer.Deserialize<PreferencesBundle>(stream);
        if(bundle is not {Format:1,Settings:not null,Profiles:not null}||bundle.Profiles.Count>500||bundle.Profiles.Any(x=>!Regex.IsMatch(x.Key,"^[A-F0-9]{64}$")||x.Value==null))throw new InvalidDataException(L.T("Arquivo de preferências inválido."));
        // Executable paths are deliberately not portable or executable via an archive.
        return bundle with{Settings=bundle.Settings.Validated() with{GameExecutable=""}};
    }
    internal static void Export(string root,string file){
        var profiles=new Dictionary<string,GameProfile>();string folder=Path.Combine(root,"profiles");
        if(Directory.Exists(folder))foreach(string path in Directory.EnumerateFiles(folder,"*.json")){
            string key=Path.GetFileNameWithoutExtension(path);if(!Regex.IsMatch(key,"^[A-F0-9]{64}$"))continue;
            var profile=new ProfileStore(folder).Load(key);if(profile!=null)profiles.Add(key,profile);
        }
        var settings=Settings.Load(Path.Combine(root,"settings.json")) with{GameExecutable=""};
        string json=JsonSerializer.Serialize(new PreferencesBundle(1,settings,profiles),Json);
        if(profiles.Count>500||System.Text.Encoding.UTF8.GetByteCount(json)>MaximumBytes)throw new InvalidDataException(L.T("Arquivo de preferências inválido."));
        File.WriteAllText(file+".tmp",json);File.Move(file+".tmp",file,true);
    }
    internal static string Import(string root,string file){
        var bundle=Read(file);Directory.CreateDirectory(root);
        string backup=Path.Combine(root,"backups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json");Directory.CreateDirectory(Path.GetDirectoryName(backup)!);Export(root,backup);
        var original=Settings.Load(Path.Combine(root,"settings.json"));
        var paths=new Dictionary<string,byte[]?>();
        void Write(string target,Action action){paths.TryAdd(target,File.Exists(target)?File.ReadAllBytes(target):null);action();}
        try{
            var store=new ProfileStore(Path.Combine(root,"profiles"));
            foreach(var pair in bundle.Profiles){string target=Path.Combine(root,"profiles",pair.Key+".json");Write(target,()=>store.Save(pair.Key,pair.Value.Apply(new Settings())));}
            string settings=Path.Combine(root,"settings.json");Write(settings,()=>(bundle.Settings with{GameExecutable=original.GameExecutable,AutomaticEnabled=false}).Save(settings));
            return backup;
        }catch{foreach(var pair in paths){if(pair.Value==null)File.Delete(pair.Key);else File.WriteAllBytes(pair.Key,pair.Value);}throw;}
    }
}
