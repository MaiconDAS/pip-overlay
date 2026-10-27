using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace PipOverlay;

internal sealed record RenderTarget(int Pid,string Name,bool Is32Bit,bool MultiApi=false)
{
    public override string ToString()=>$"{Name} · PID {Pid}";
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool IsWow64Process(nint process,out bool wow);
    internal static RenderTarget[] Find()=>Process.GetProcesses().Select(p=>{
        using(p)try{
            if(p.ProcessName is not ("DigimonMasters" or "PipTest32" or "PipTest64"))return null;
            if(!IsWow64Process(p.Handle,out var wow))return null;
            return new RenderTarget(p.Id,p.ProcessName,wow);
        }catch{return null;}
    }).OfType<RenderTarget>().ToArray();
}

internal sealed class GpuVideoSession : IDisposable
{
    readonly object gate=new();
    readonly CancellationTokenSource cancel=new();
    readonly Task worker;
    readonly IDirect3DDevice device;
    readonly GraphicsCaptureItem item;
    readonly Direct3D11CaptureFramePool pool;
    readonly GraphicsCaptureSession capture;
    nint producer;
    Settings settings;
    Size monitor;
    string? error;
    bool disposed;
    nint timer;
    long sent,rendered,uploaded;
    uint firstSent,firstRendered,firstUploaded;
    bool ready;
    double milliseconds;
    internal Size SourceSize {get;private set;}
    internal bool ActualFullscreen {get;private set;}
    internal Size TargetSize {get;private set;}
    internal Size TransferSize {get;private set;}
    internal long Published=>Interlocked.Read(ref sent);
    internal long Rendered=>Interlocked.Read(ref rendered);
    internal long Uploaded=>Interlocked.Read(ref uploaded);
    internal bool Ready {get{lock(gate)return ready;}}
    internal double PublishMilliseconds {get{lock(gate)return milliseconds;}}
    internal string? Error {get{lock(gate)return error;}}
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern nint PipOpen(uint pid);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern int PipSubmit(nint p,nint texture);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipResolution(nint p,uint width,uint height);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipFrameSize(nint p,[Out]uint[] size);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipLayout(nint p,float x,float y,float w,float h);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipStats(nint p,[Out]uint[] stats);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipClose(nint p);
    [DllImport("PipGpu.dll",CallingConvention=CallingConvention.Cdecl)] static extern void PipValidation(nint p,[Out]uint[] stats);
    internal uint[] ValidatePixels(){lock(gate){var stats=new uint[2];if(producer!=0)PipValidation(producer,stats);return stats;}}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern nint CreateWaitableTimerExW(nint attributes,string? name,uint flags,uint access);
    [DllImport("kernel32.dll")] static extern bool SetWaitableTimer(nint handle,ref long due,int period,nint completion,nint argument,bool resume);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(nint handle,uint milliseconds);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(nint handle);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetInterface(nint self,ref Guid iid,out nint result);
    internal GpuVideoSession(WindowSource source,Settings initial,Size screen,RenderTarget target)
    {
        if(!target.MultiApi)PortableSetup.Ensure(target.Is32Bit?32:64);
        settings=initial.Validated();monitor=screen;
        producer=PipOpen((uint)target.Pid);if(producer==0)throw new InvalidOperationException("Não foi possível abrir o transporte em memória.");
        var initialStats=new uint[8];PipStats(producer,initialStats);firstSent=initialStats[0];firstRendered=initialStats[1];firstUploaded=initialStats[2];
        IDirect3DDevice? d=null;Direct3D11CaptureFramePool? f=null;GraphicsCaptureSession? s=null;
        try {
            string attach=Path.Combine(AppContext.BaseDirectory,$"PipAttach{(target.Is32Bit?32:64)}.exe");
            if(!target.MultiApi)using(var helper=Process.Start(new ProcessStartInfo(attach,target.Pid.ToString()){UseShellExecute=false,CreateNoWindow=true})!) {
                if(!helper.WaitForExit(12000))throw new InvalidOperationException("Tempo de conexão excedido. O alvo não foi confirmado.");
                if(helper.ExitCode!=0)throw new InvalidOperationException($"Conexão Direct3D recusada (código {helper.ExitCode}). Nenhuma proteção foi alterada.");
            }
            d=CaptureInterop.CreateDevice();item=CaptureInterop.CreateItem(source.Handle);
            SourceSize=new(item.Size.Width,item.Size.Height);
            f=Direct3D11CaptureFramePool.CreateFreeThreaded(d,DirectXPixelFormat.B8G8R8A8UIntNormalized,2,item.Size);
            s=f.CreateCaptureSession(item);s.IsCursorCaptureEnabled=false;
            device=d;pool=f;capture=s;s.StartCapture();timer=CreateWaitableTimerExW(0,null,2,0x1F0003);worker=Task.Run(Run);
        }catch{s?.Dispose();f?.Dispose();d?.Dispose();PipClose(producer);producer=0;cancel.Dispose();throw;}
    }
    async Task Run()
    {
        var poolSize=item.Size;var stats=new uint[8];var size=new uint[2];var textureIid=new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
        try {
            while(!cancel.IsCancellationRequested) {
                long started=Environment.TickCount64;Settings current;Size screen;lock(gate){current=settings;screen=monitor;}
                // Quantize resize requests to avoid reallocating textures for each mouse pixel.
                var resolution=current.CaptureResolution;PipResolution(producer,(uint)resolution.Width,(uint)resolution.Height);
                PipLayout(producer,(float)current.X/screen.Width,(float)current.Y/screen.Height,(float)current.Width/screen.Width,(float)current.Height/screen.Height);
                var next=poolSize;
                using(var frame=pool.TryGetNextFrame())if(frame!=null) {
                    next=frame.ContentSize;nint surface=0,access=0,texture=0;
                    try {
                        surface=WinRT.MarshalInterface<IDirect3DSurface>.FromManaged(frame.Surface);
                        var accessIid=new Guid("a9b3d012-3df2-4ee3-b8d1-8695f457d3c1");Native.Check(Marshal.QueryInterface(surface,ref accessIid,out access));
                        var get=Marshal.GetDelegateForFunctionPointer<GetInterface>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(access),3*IntPtr.Size));
                        Native.Check(get(access,ref textureIid,out texture));
                        var clock=Stopwatch.StartNew();Native.Check(PipSubmit(producer,texture));lock(gate)milliseconds=clock.Elapsed.TotalMilliseconds;
                    }finally{if(texture!=0)Marshal.Release(texture);if(access!=0)Marshal.Release(access);if(surface!=0)Marshal.Release(surface);}
                }
                if(next.Width>0&&next.Height>0&&(next.Width!=poolSize.Width||next.Height!=poolSize.Height)) {
                    poolSize=next;SourceSize=new(next.Width,next.Height);pool.Recreate(device,DirectXPixelFormat.B8G8R8A8UIntNormalized,2,poolSize);
                }
                PipStats(producer,stats);Interlocked.Exchange(ref sent,unchecked(stats[0]-firstSent));Interlocked.Exchange(ref rendered,unchecked(stats[1]-firstRendered));Interlocked.Exchange(ref uploaded,unchecked(stats[2]-firstUploaded));
                lock(gate)ready=stats[4]!=0;
                ActualFullscreen=stats[5]!=0;TargetSize=new((int)stats[6],(int)stats[7]);
                PipFrameSize(producer,size);TransferSize=new((int)size[0],(int)size[1]);
                if(stats[3]!=0)throw new InvalidOperationException($"Direct3D retornou 0x{stats[3]:X8}.");
                int delay=(int)Math.Max(1,1000/current.FramesPerSecond-(Environment.TickCount64-started));
                if(timer!=0){long due=-delay*10000L;if(SetWaitableTimer(timer,ref due,0,0,0,false))WaitForSingleObject(timer,(uint)delay+100);else await Task.Delay(delay,cancel.Token);}
                else await Task.Delay(delay,cancel.Token);
            }
        }catch(OperationCanceledException){}
        catch(Exception ex){lock(gate)error=ex.Message;}
        finally{capture.Dispose();pool.Dispose();device.Dispose();lock(gate){PipClose(producer);producer=0;}if(timer!=0){CloseHandle(timer);timer=0;}}
    }
    internal void Apply(Settings value){lock(gate)settings=value.Validated();}
    public void Dispose(){if(disposed)return;disposed=true;cancel.Cancel();worker.GetAwaiter().GetResult();cancel.Dispose();}
}
