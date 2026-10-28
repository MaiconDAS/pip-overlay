using System.Net.Http;
using System.Text.Json;
namespace PipOverlay;
internal sealed record AppRelease(Version Version,string Url);
internal static class UpdateService
{
    internal static Version Current=>Version.Parse(typeof(UpdateService).Assembly.GetName().Version!.ToString(3));
    internal static AppRelease? Parse(string json,Version current){
        using var doc=JsonDocument.Parse(json);AppRelease? newest=null;
        if(doc.RootElement.ValueKind!=JsonValueKind.Array)throw new JsonException("Invalid release list");
        foreach(var item in doc.RootElement.EnumerateArray()){
            if(item.ValueKind!=JsonValueKind.Object||!item.TryGetProperty("draft",out var draft)||draft.ValueKind!=JsonValueKind.False)continue;
            if(!item.TryGetProperty("tag_name",out var tagElement)||tagElement.ValueKind!=JsonValueKind.String||!item.TryGetProperty("html_url",out var urlElement)||urlElement.ValueKind!=JsonValueKind.String)continue;
            string tag=tagElement.GetString()??"",url=urlElement.GetString()??"";
            if(!Version.TryParse(tag.TrimStart('v'),out var version)||version<=current)continue;
            if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!="github.com"||!uri.AbsolutePath.StartsWith("/MaiconDAS/pip-overlay/releases/tag/",StringComparison.Ordinal))continue;
            if(newest==null||version>newest.Version)newest=new(version,url);
        }return newest;
    }
    internal static async Task<AppRelease?> Check(){
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(12)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PiP-Overlay/"+Current);
        return Parse(await client.GetStringAsync("https://api.github.com/repos/MaiconDAS/pip-overlay/releases?per_page=10"),Current);
    }
}
