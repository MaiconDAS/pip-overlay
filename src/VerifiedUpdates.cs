using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace PipOverlay;

internal sealed record InstallerRelease(Version Version,string Url,string Name,string Sha256,long Size){public override string ToString()=>"v"+Version;}
internal static class VerifiedUpdates
{
    const string Repository="https://github.com/MaiconDAS/pip-overlay/releases/";
    internal static InstallerRelease[] Parse(string json){
        using var doc=JsonDocument.Parse(json);var releases=new List<InstallerRelease>();
        if(doc.RootElement.ValueKind!=JsonValueKind.Array)throw new JsonException("Invalid release list");
        foreach(var item in doc.RootElement.EnumerateArray()){
            if(item.ValueKind!=JsonValueKind.Object||!item.TryGetProperty("draft",out var draft)||draft.ValueKind!=JsonValueKind.False)continue;
            if(item.TryGetProperty("prerelease",out var pre)&&pre.ValueKind!=JsonValueKind.False)continue;
            string? Text(JsonElement obj,string name)=>obj.TryGetProperty(name,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString():null;
            string tag=Text(item,"tag_name")??"";
            if(!Regex.IsMatch(tag,"^v[0-9]+\\.[0-9]+\\.[0-9]+$")||!Version.TryParse(tag[1..],out var version)||Text(item,"html_url")!=Repository+"tag/"+tag)continue;
            string name="PiP-Overlay-"+tag+".exe";
            if(item.TryGetProperty("assets",out var assets)&&assets.ValueKind==JsonValueKind.Array)foreach(var asset in assets.EnumerateArray()){
                if(asset.ValueKind!=JsonValueKind.Object)continue;string? digest=Text(asset,"digest");
                if(Text(asset,"name")!=name||Text(asset,"browser_download_url")!=Repository+"download/"+tag+"/"+name||digest==null||!Regex.IsMatch(digest,"^sha256:[a-fA-F0-9]{64}$"))continue;
                if(!asset.TryGetProperty("size",out var size)||size.ValueKind!=JsonValueKind.Number||!size.TryGetInt64(out long bytes)||bytes<=0||bytes>256*1024*1024)continue;
                releases.Add(new(version,Repository+"download/"+tag+"/"+name,name,digest[7..],bytes));
            }
        }
        return releases.DistinctBy(x=>x.Version).OrderByDescending(x=>x.Version).ToArray();
    }
    internal static HttpClient Client(){var client=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};client.DefaultRequestHeaders.UserAgent.ParseAdd("PiP-Overlay/"+UpdateService.Current);return client;}
    internal static async Task<InstallerRelease[]> List(CancellationToken token){
        using var client=Client();using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(TimeSpan.FromSeconds(12));
        return Parse(await client.GetStringAsync("https://api.github.com/repos/MaiconDAS/pip-overlay/releases?per_page=30",deadline.Token).ConfigureAwait(false));
    }
    internal static async Task<string> Download(HttpClient client,InstallerRelease asset,string directory,IProgress<int>? progress,CancellationToken token){
        string name="PiP-Overlay-v"+asset.Version+".exe";
        if(asset.Name!=name||asset.Url!=Repository+"download/v"+asset.Version+"/"+name||!Regex.IsMatch(asset.Sha256,"^[a-fA-F0-9]{64}$")||asset.Size<=0||asset.Size>256*1024*1024)throw new InvalidDataException("Invalid asset");
        Directory.CreateDirectory(directory);string target=Path.Combine(directory,name),temporary=Path.Combine(directory,Guid.NewGuid().ToString("N")+".partial");
        try{
            using var response=await client.GetAsync(asset.Url,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false);response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength is long length&&length!=asset.Size)throw new InvalidDataException("Size mismatch");
            using var input=await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using(var output=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true)){
                using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[65536];long total=0;int last=-1;
                while(true){int read=await input.ReadAsync(buffer,token).ConfigureAwait(false);if(read==0)break;total+=read;if(total>asset.Size)throw new InvalidDataException("Size mismatch");hash.AppendData(buffer,0,read);await output.WriteAsync(buffer.AsMemory(0,read),token).ConfigureAwait(false);int percent=(int)(total*100/asset.Size);if(percent!=last){progress?.Report(percent);last=percent;}}
                if(total!=asset.Size||!CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(),Convert.FromHexString(asset.Sha256)))throw new InvalidDataException("SHA256 mismatch");
                await output.FlushAsync(token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();File.Move(temporary,target,true);return target;
        }finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
