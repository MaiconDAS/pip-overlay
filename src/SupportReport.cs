using System.Text.Json;
namespace PipOverlay;
internal static class SupportReport
{
    // Explicit allowlist: no settings object, exceptions, source titles or processes.
    internal static string Create(Settings settings,GpuVideoSession? session)=>JsonSerializer.Serialize(new{
        appVersion=UpdateService.Current.ToString(),windowsVersion=Environment.OSVersion.Version.ToString(),
        architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        resolution=settings.Validated().Resolution,requestedFps=settings.FramesPerSecond,
        adaptive=settings.AdaptiveFps,minimumFps=settings.MinimumFps,
        session=session==null?null:new {ready=session.Ready,effectiveFps=session.EffectiveFps,
            captureWidth=session.TransferSize.Width,captureHeight=session.TransferSize.Height,
            rendered=session.Rendered,uploaded=session.Uploaded,published=session.Published,
            lastSubmitMs=session.PublishMilliseconds,hasError=session.Error!=null}
    },new JsonSerializerOptions{WriteIndented=true});
}
