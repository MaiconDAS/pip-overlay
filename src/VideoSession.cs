namespace PipOverlay;

// A single worker owns the encoder and mapping. The UI never waits for an image write.
internal sealed class VideoSession : IDisposable
{
    readonly object gate=new();
    readonly WindowCapture capture;
    readonly CancellationTokenSource cancel=new();
    readonly Task worker;
    Settings settings;
    Bitmap? preview;
    string? error;
    long published;
    double milliseconds;
    internal long Published=>Interlocked.Read(ref published);
    internal string? Error {get {lock(gate)return error??capture.Error;}}
    internal Size SourceSize=>capture.SourceSize;
    internal double PublishMilliseconds {get{lock(gate)return milliseconds;}}
    internal bool WantPreview {get;set;}=true;
    internal VideoSession(WindowSource source,Settings initial)
    {
        settings=initial.Validated();
        var output=new RtssFrameOutput();
        try {capture=new WindowCapture(source.Handle,settings.FramesPerSecond);}
        catch {output.Dispose();throw;}
        worker=Task.Run(async()=> {
            long lastPreview=0;
            try {
                using(output) while(!cancel.IsCancellationRequested) {
                    long started=Environment.TickCount64;Settings current;lock(gate)current=settings;
                    using var image=capture.TakeFrame();
                    if(image!=null) {
                        if(output.Publish(image,current)) Interlocked.Increment(ref published);
                        lock(gate)milliseconds=output.LastPublishMilliseconds;
                        if(WantPreview && started-lastPreview>=200) {
                            var thumb=new Bitmap(image,Settings.Fit(image.Size,new Rectangle(0,0,480,270)).Size);
                            lock(gate){preview?.Dispose();preview=thumb;}lastPreview=started;
                        }
                    }
                    if(capture.Error is string message) throw new InvalidOperationException(message);
                    await Task.Delay((int)Math.Max(1,1000/current.FramesPerSecond-(Environment.TickCount64-started)),cancel.Token).ConfigureAwait(false);
                }
            }catch(OperationCanceledException){}
            catch(Exception e){lock(gate)error=e.Message;}
            finally {capture.Dispose();}
        });
    }
    internal void Apply(Settings next) {lock(gate)settings=next.Validated();}
    internal Bitmap? TakePreview(){lock(gate){var result=preview;preview=null;return result;}}
    public void Dispose()
    {
        cancel.Cancel();worker.GetAwaiter().GetResult();
        lock(gate){preview?.Dispose();preview=null;}cancel.Dispose();
    }
}
