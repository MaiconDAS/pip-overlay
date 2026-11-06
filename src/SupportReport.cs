using System.Text.Json;
namespace PipOverlay;
internal static class SupportReport
{
    // Explicit allowlist: no settings object, exceptions, source titles or processes.
    internal static string Create(Settings settings,GpuVideoSession? session,double sessionSeconds=0,bool paused=false,DesktopOverlay? desktop=null)=>JsonSerializer.Serialize(new{
        appVersion=UpdateService.Current.ToString(),windowsVersion=Environment.OSVersion.Version.ToString(),
        architecture=System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
        resolution=settings.Validated().Resolution,requestedFps=settings.FramesPerSecond,
        adaptive=settings.AdaptiveFps,minimumFps=settings.MinimumFps,
        language=settings.Language,
        outputMode=settings.PreferDesktop?"desktop":"direct",
        state=paused?"paused":session==null?"waiting-source":session.Error!=null?"failed":session.Rendered==0?"awaiting-renderer":session.Backend=="desktop"?"desktop-active":"renderer-confirmed",
        sessionSeconds=Math.Round(Math.Max(0,sessionSeconds),1),
        publishedPerSecond=session!=null&&sessionSeconds>0?Math.Round(session.Published/sessionSeconds,2):(double?)null,
        uploadedPerSecond=session!=null&&sessionSeconds>0?Math.Round(session.Uploaded/sessionSeconds,2):(double?)null,
        renderedPerSecond=session!=null&&sessionSeconds>0?Math.Round(session.Rendered/sessionSeconds,2):(double?)null,
        desktop=desktop?.Diagnostics(),
        session=session==null?null:new {backend=session.Backend,ready=session.Ready,effectiveFps=session.EffectiveFps,
            transport=session.SharedTransport==1?"shared-gpu-d3d11":session.SharedTransport<0?"cpu-fallback":"cpu",
            captureWidth=session.TransferSize.Width,captureHeight=session.TransferSize.Height,
            rendered=session.Rendered,uploaded=session.Uploaded,published=session.Published,
            backpressure=session.BackpressureFrames,processed=session.ProcessedFrames,discarded=session.DiscardedFrames,workerWakeups=session.WorkerWakeups,lastSubmitMs=session.PublishMilliseconds,hasError=session.Error!=null}
    },new JsonSerializerOptions{WriteIndented=true});
}
