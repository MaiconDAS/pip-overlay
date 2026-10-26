using System.Diagnostics;
using System.Reflection.PortableExecutable;
namespace PipOverlay;
internal static class PortableSetup
{
    internal static bool IsCurrent(string catalog,string library){
        try {
            using var reader=new BinaryReader(File.OpenRead(catalog));
            using var pe=new PEReader(File.OpenRead(library));var headers=pe.PEHeaders;
            if(reader.BaseStream.Length!=160||headers.PEHeader==null)return false;
            if(!(reader.ReadUInt32()==0x36414450&&reader.ReadUInt32()==(uint)headers.CoffHeader.Machine&&reader.ReadUInt32()==unchecked((uint)headers.CoffHeader.TimeDateStamp)&&reader.ReadUInt32()==headers.PEHeader.SizeOfImage&&reader.ReadUInt32()<headers.PEHeader.SizeOfImage))return false;
            uint reset=reader.ReadUInt32(),stamp=reader.ReadUInt32(),size=reader.ReadUInt32();
            string module=System.Text.Encoding.Unicode.GetString(reader.ReadBytes(128)).TrimEnd('\0');
            if(module!="d3d9.dll"&&module!="apphelp.dll")return false;
            using var resetPe=new PEReader(File.OpenRead(Path.Combine(Path.GetDirectoryName(library)!,module)));
            return reset<size&&stamp==unchecked((uint)resetPe.PEHeaders.CoffHeader.TimeDateStamp)&&size==resetPe.PEHeaders.PEHeader?.SizeOfImage;
        }catch(Exception e)when(e is IOException or UnauthorizedAccessException or BadImageFormatException){return false;}
    }
    internal static void Ensure(int bits){
        {
            string catalog=Path.Combine(AppContext.BaseDirectory,$"PipD3D9-{bits}.bin");
            string library=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),bits==32?"SysWOW64":"System32","d3d9.dll");
            if(IsCurrent(catalog,library))return;
            string helper=Path.Combine(AppContext.BaseDirectory,$"PipProbe{bits}.exe");
            if(!File.Exists(helper))throw new InvalidOperationException("A pasta do aplicativo está incompleta. Extraia todos os arquivos do pacote novamente.");
            using var process=Process.Start(new ProcessStartInfo(helper){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=AppContext.BaseDirectory})!;
            if(!process.WaitForExit(15000)){process.Kill();throw new InvalidOperationException("A descoberta do Direct3D excedeu o tempo limite.");}
            if(process.ExitCode!=0||!IsCurrent(catalog,library))throw new InvalidOperationException($"Falha na descoberta Direct3D {bits} bits (código {process.ExitCode}). Verifique se a pasta do aplicativo permite gravar arquivos.");
        }
    }
}
