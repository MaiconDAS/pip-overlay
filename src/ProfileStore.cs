using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace PipOverlay;
internal sealed record GameProfile(string Monitor,int X,int Y,int Width,int Height,string? Resolution,int Fps,bool Adaptive,int MinimumFps,bool Snap=true,int Margin=24)
{
    internal static GameProfile From(Settings s)=>new(s.Monitor,s.X,s.Y,s.Width,s.Height,s.Validated().Resolution,s.FramesPerSecond,s.AdaptiveFps,s.MinimumFps,s.SnapEnabled,s.SnapMargin);
    internal Settings Apply(Settings s)=>(s with {Monitor=Monitor,X=X,Y=Y,Width=Width,Height=Height,Resolution=Resolution,FramesPerSecond=Fps,AdaptiveFps=Adaptive,MinimumFps=MinimumFps,SnapEnabled=Snap,SnapMargin=Margin}).Validated();
}
internal sealed class ProfileStore(string folder)
{
    internal static string Key(string executable)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(executable).ToUpperInvariant())));
    internal GameProfile? Load(string key){try{return JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(Path.Combine(folder,key+".json")));}catch(Exception e)when(e is IOException or UnauthorizedAccessException or JsonException){return null;}}
    internal void Save(string key,Settings settings){Directory.CreateDirectory(folder);string file=Path.Combine(folder,key+".json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(GameProfile.From(settings)));File.Move(file+".tmp",file,true);}
}
