using System.IO.MemoryMappedFiles;
using System.Text;

namespace PipOverlay;

// Wire layout from RTSS SDK RTSSSharedMemory.h. No hook DLL is loaded by this client.
// Only the named mapping created by RTSS is opened; this never creates a fake RTSS server.
internal sealed unsafe class RtssClient : IDisposable
{
    internal const uint Signature=0x52545353;
    readonly MemoryMappedFile mapping;
    readonly MemoryMappedViewAccessor view;
    readonly byte[] owner;
    byte* pointer;
    bool disposed;
    static readonly Encoding WireEncoding=CreateEncoding();
    [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern uint GetACP();
    static Encoding CreateEncoding() {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding((int)GetACP(),EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
    }
    internal RtssClient(string name="RTSSSharedMemoryV2")
    {
        owner=Encoding.ASCII.GetBytes("PipOverlay-"+Guid.NewGuid().ToString("N"));
        try {mapping=MemoryMappedFile.OpenExisting(name,MemoryMappedFileRights.ReadWrite);}
        catch(FileNotFoundException e) when(name=="RTSSSharedMemoryV2") {
            throw new InvalidOperationException("Abra o RivaTuner Statistics Server (RTSS) e tente iniciar novamente.",e);
        }
        try {
            view=mapping.CreateViewAccessor(0,0,MemoryMappedFileAccess.ReadWrite);
            try {view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);Validate();}
            catch {if(pointer!=null) view.SafeMemoryMappedViewHandle.ReleasePointer();pointer=null;view.Dispose();throw;}
        } catch {mapping.Dispose();throw;}
    }
    uint U(int offset)=>view.ReadUInt32(offset);
    (long Start,int Stride,int Count) Validate()
    {
        ObjectDisposedException.ThrowIf(disposed,this);
        if(view.Capacity<40 || U(0)!=Signature) throw new InvalidDataException("Memória RTSS não inicializada.");
        uint version=U(4);
        if((version>>16)!=2 || version<0x2000e) throw new InvalidDataException("RTSS sem protocolo de bloqueio v2.14 ou posterior.");
        long stride=U(20),start=U(24),count=U(28),end=start+stride*count;
        int headerSize=version>=0x20013 ? 96 : version>=0x20012 ? 80 : version>=0x20010 ? 72 : version>=0x2000f ? 64 : 40;
        if(start<headerSize || stride<4608 || stride>1048576 || count<2 || count>1024 || end>view.Capacity)
            throw new InvalidDataException("Layout RTSS inválido ou fora dos limites.");
        ulong appStart=U(12),appEnd=appStart+(ulong)U(8)*U(16);
        if(U(16)>0 && (U(8)==0 || appStart<(ulong)headerSize || appEnd>(ulong)view.Capacity || (appStart<(ulong)end && appEnd>(ulong)start)))
            throw new InvalidDataException("Tabela de aplicações RTSS inválida ou sobreposta.");
        return(start,(int)stride,(int)count);
    }
    bool Lock()
    {
        // Never wait while RTSS renders. Drop this update and retry on the next frame.
        return Interlocked.CompareExchange(ref *(int*)(pointer+view.PointerOffset+36),1,0)==0;
    }
    void Unlock()=>Interlocked.Exchange(ref *(int*)(pointer+view.PointerOffset+36),0);
    bool Owns(long slot)
    {
        for(int i=0;i<owner.Length;i++) if(view.ReadByte(slot+256+i)!=owner[i]) return false;
        return view.ReadByte(slot+256+owner.Length)==0;
    }
    internal bool Publish(string text)
    {
        if(text.Contains('\0')) throw new ArgumentException("Texto RTSS não pode conter NUL.");
        byte[] bytes=WireEncoding.GetBytes(text);
        if(bytes.Length>=4096) throw new ArgumentException("Texto excede o limite de 4095 bytes.");
        Validate();
        if(!Lock()) return false;
        try {
            var layout=Validate();long selected=-1,free=-1;
            for(int i=1;i<layout.Count;i++) {
                long slot=layout.Start+(long)i*layout.Stride;
                if(Owns(slot)) {selected=slot;break;}
                if(free<0 && view.ReadByte(slot+256)==0) free=slot;
            }
            if(selected<0) selected=free;
            if(selected<0) throw new InvalidOperationException("Não há slot OSD livre no RTSS.");
            if(!Owns(selected)) {
                // Clear only a free slot, preserving reserved slot zero and other clients.
                view.WriteArray(selected,new byte[layout.Stride],0,layout.Stride);
                view.WriteArray(selected+256,owner,0,owner.Length);
            }
            // RTSS concatenates the fields; write exactly one or the overlay is duplicated.
            bool extended=U(4)>=0x20014 && layout.Stride>=299520;
            long textOffset=extended?266752:512;
            view.WriteArray(selected+textOffset,bytes,0,bytes.Length);
            view.Write(selected+textOffset+bytes.Length,(byte)0);
            if(extended) view.Write(selected+512,(byte)0);
            view.Write(32,unchecked(U(32)+1));return true;
        } finally {Unlock();}
    }
    internal bool Release()
    {
        Validate();if(!Lock()) return false;
        try {
            var layout=Validate();
            for(int i=1;i<layout.Count;i++) {
                long slot=layout.Start+(long)i*layout.Stride;
                if(!Owns(slot)) continue;
                view.WriteArray(slot,new byte[layout.Stride],0,layout.Stride);
                view.Write(32,unchecked(U(32)+1));
            }
            return true;
        } finally {Unlock();}
    }
    public void Dispose()
    {
        if(disposed) return;
        try {
            try { for(int i=0;i<20;i++) {if(Release()) break;Thread.Sleep(5);} }
            catch(InvalidDataException) { /* Server shutdown: don't touch invalid memory. */ }
        } finally {
            disposed=true;view.SafeMemoryMappedViewHandle.ReleasePointer();pointer=null;view.Dispose();mapping.Dispose();
        }
    }
}
