using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace PipOverlay;

// Frames stay in memory. No recording, network access or browser configuration changes.
internal sealed class WindowCapture : IDisposable
{
    readonly CancellationTokenSource cancellation=new();
    readonly object gate=new();
    IDirect3DDevice? device;
    GraphicsCaptureItem? item;
    Direct3D11CaptureFramePool? pool;
    GraphicsCaptureSession? session;
    Bitmap? latest;
    Task? worker;
    Size sourceSize;
    long frames;
    string? error;
    bool disposed;
    readonly int interval;
    internal Size SourceSize { get { lock(gate) return sourceSize; } }
    internal long Frames => Interlocked.Read(ref frames);
    internal string? Error { get { lock(gate) return error; } }
    internal WindowCapture(nint hwnd,int framesPerSecond=30)
    {
        interval=Math.Max(1,1000/Math.Clamp(framesPerSecond,10,30));
        if(!GraphicsCaptureSession.IsSupported()) throw new NotSupportedException("A captura de janelas não está disponível neste Windows.");
        try {
            device=CaptureInterop.CreateDevice();item=CaptureInterop.CreateItem(hwnd);
            sourceSize=new Size(item.Size.Width,item.Size.Height);
            pool=Direct3D11CaptureFramePool.CreateFreeThreaded(device,DirectXPixelFormat.B8G8R8A8UIntNormalized,2,item.Size);
            session=pool.CreateCaptureSession(item);session.IsCursorCaptureEnabled=false;
            // Keep the system capture indicator; do not request borderless-capture permission.
            session.StartCapture();worker=Task.Run(RunAsync);
        } catch { Cleanup();cancellation.Dispose();throw; }
    }
    async Task RunAsync()
    {
        byte[] bytes=Array.Empty<byte>();Windows.Storage.Streams.Buffer? buffer=null;
        var poolSize=item!.Size;
        try {
            while(!cancellation.IsCancellationRequested) {
                long cycle=Environment.TickCount64;
                var nextSize=poolSize;
                using(var frame=pool!.TryGetNextFrame()) {
                    if(frame!=null && frame.ContentSize.Width>0 && frame.ContentSize.Height>0) {
                        nextSize=frame.ContentSize;
                        using var software=await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface,BitmapAlphaMode.Ignore);
                        int width=software.PixelWidth,height=software.PixelHeight,byteCount=checked(width*height*4);
                        if(bytes.Length!=byteCount) {bytes=new byte[byteCount];buffer=new Windows.Storage.Streams.Buffer((uint)byteCount);}
                        software.CopyToBuffer(buffer!);
                        using(var reader=DataReader.FromBuffer(buffer)) reader.ReadBytes(bytes);
                        var bitmap=new Bitmap(width,height,PixelFormat.Format32bppRgb);
                        try {
                            var data=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppRgb);
                            try {
                                if(data.Stride==width*4) Marshal.Copy(bytes,0,data.Scan0,bytes.Length);
                                else for(int row=0;row<height;row++) Marshal.Copy(bytes,row*width*4,data.Scan0+row*data.Stride,width*4);
                            } finally {bitmap.UnlockBits(data);}
                            lock(gate) {
                                if(disposed) bitmap.Dispose();
                                else {latest?.Dispose();latest=bitmap;sourceSize=new Size(width,height);Interlocked.Increment(ref frames);}
                            }
                        } catch {bitmap.Dispose();throw;}
                    }
                }
                // Return the current frame before resizing its pool.
                if(nextSize.Width!=poolSize.Width || nextSize.Height!=poolSize.Height) {
                    poolSize=nextSize;pool.Recreate(device,DirectXPixelFormat.B8G8R8A8UIntNormalized,2,poolSize);
                }
                await Task.Delay((int)Math.Max(1,interval-(Environment.TickCount64-cycle)),cancellation.Token).ConfigureAwait(false);
            }
        } catch(OperationCanceledException) { }
        catch(Exception e) {lock(gate) error=e.Message;}
        finally {Cleanup();}
    }
    internal Bitmap? TakeFrame() {lock(gate) {var result=latest;latest=null;return result;}}
    void Cleanup() {session?.Dispose();session=null;pool?.Dispose();pool=null;device?.Dispose();device=null;item=null;}
    public void Dispose()
    {
        lock(gate) {if(disposed) return;disposed=true;latest?.Dispose();latest=null;}
        cancellation.Cancel();
        if(worker!=null) _=worker.ContinueWith(_=>cancellation.Dispose(),TaskScheduler.Default);
        else {Cleanup();cancellation.Dispose();}
    }
}

internal static class CaptureInterop
{
    [DllImport("d3d11.dll")] static extern int D3D11CreateDevice(nint adapter,int driver,nint software,uint flags,nint levels,uint count,uint sdk,out nint device,out int feature,out nint context);
    [DllImport("d3d11.dll")] static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgi,out nint device);
    [DllImport("combase.dll",CharSet=CharSet.Unicode)] static extern int WindowsCreateString(string value,int length,out nint text);
    [DllImport("combase.dll")] static extern int WindowsDeleteString(nint text);
    [DllImport("combase.dll")] static extern int RoGetActivationFactory(nint text,ref Guid iid,out nint factory);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int CreateForWindow(nint self,nint hwnd,ref Guid iid,out nint item);
    internal static IDirect3DDevice CreateDevice()
    {
        nint native=0,context=0,dxgi=0,projected=0;
        try {
            int hr=D3D11CreateDevice(0,1,0,0x20,0,0,7,out native,out _,out context);
            if(hr<0) {
                if(native!=0) Marshal.Release(native);if(context!=0) Marshal.Release(context);native=0;context=0;
                hr=D3D11CreateDevice(0,5,0,0x20,0,0,7,out native,out _,out context);
            }
            Native.Check(hr);var iid=new Guid("54ec77fa-1377-44e6-8c32-88fd5f44c84c");
            Native.Check(Marshal.QueryInterface(native,ref iid,out dxgi));
            Native.Check(CreateDirect3D11DeviceFromDXGIDevice(dxgi,out projected));
            return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(projected);
        } finally {
            if(projected!=0) Marshal.Release(projected);if(dxgi!=0) Marshal.Release(dxgi);
            if(context!=0) Marshal.Release(context);if(native!=0) Marshal.Release(native);
        }
    }
    internal static GraphicsCaptureItem CreateItem(nint hwnd)
    {
        nint text=0,factory=0,item=0;
        try {
            const string name="Windows.Graphics.Capture.GraphicsCaptureItem";
            Native.Check(WindowsCreateString(name,name.Length,out text));
            var interop=new Guid("3628e81b-3cac-4c60-b7f4-23ce0e0c3356");Native.Check(RoGetActivationFactory(text,ref interop,out factory));
            var vtable=Marshal.ReadIntPtr(factory);
            var create=Marshal.GetDelegateForFunctionPointer<CreateForWindow>(Marshal.ReadIntPtr(vtable,3*IntPtr.Size));
            var iid=new Guid("79c3f95b-31f7-4ec2-a464-632ef5d30760");Native.Check(create(factory,hwnd,ref iid,out item));
            return WinRT.MarshalInterface<GraphicsCaptureItem>.FromAbi(item);
        } finally {if(item!=0) Marshal.Release(item);if(factory!=0) Marshal.Release(factory);if(text!=0) WindowsDeleteString(text);}
    }
}
