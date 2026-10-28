$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
Add-Type -ReferencedAssemblies System.Drawing.Common,System.Drawing.Primitives,System.Private.Windows.GdiPlus,System.Private.Windows.Core -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
public static class PipBrand {
 static GraphicsPath Round(float x,float y,float w,float h,float r){var p=new GraphicsPath();p.AddArc(x,y,r*2,r*2,180,90);p.AddArc(x+w-r*2,y,r*2,r*2,270,90);p.AddArc(x+w-r*2,y+h-r*2,r*2,r*2,0,90);p.AddArc(x,y+h-r*2,r*2,r*2,90,90);p.CloseFigure();return p;}
 public static Bitmap Render(int size){
  var image=new Bitmap(size,size);using(var g=Graphics.FromImage(image)){
   g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/256f,size/256f);
   using(var shadow=Round(12,18,232,232,56))using(var b=new SolidBrush(Color.FromArgb(45,0,0,0)))g.FillPath(b,shadow);
   using(var tile=Round(12,8,232,232,56))using(var b=new LinearGradientBrush(new Rectangle(12,8,232,232),Color.FromArgb(45,62,105),Color.FromArgb(12,20,40),65))g.FillPath(b,tile);
   using(var frame=Round(47,56,155,112,21))using(var p=new Pen(Color.FromArgb(215,232,250),10))g.DrawPath(p,frame);
   using(var shadow=Round(113,113,112,83,20))using(var b=new SolidBrush(Color.FromArgb(70,0,0,0)))g.FillPath(b,shadow);
   using(var pip=Round(108,104,112,82,18))using(var b=new LinearGradientBrush(new Rectangle(108,104,112,82),Color.FromArgb(129,255,219),Color.FromArgb(40,195,184),55))g.FillPath(b,pip);
   using(var b=new SolidBrush(Color.FromArgb(15,48,62)))g.FillPolygon(b,new[]{new PointF(153,125),new PointF(181,145),new PointF(153,165)});
  }return image;
 }
}
'@
$sizes=@(16,20,24,32,40,48,64,128,256)
$frames=@()
foreach($size in $sizes){
    $bitmap=[PipBrand]::Render($size)
    $stream=[IO.MemoryStream]::new()
    $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $frames+=,@{Size=$size;Bytes=$stream.ToArray()}
    $bitmap.Dispose();$stream.Dispose()
}
$file=[IO.File]::Create((Join-Path $root 'src/Assets/App.ico'))
$writer=[IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
    $offset=6+16*$frames.Count
    foreach($frame in $frames){
        $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset)
        $offset+=$frame.Bytes.Length
    }
    foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
}finally{$writer.Dispose()}
$preview=[PipBrand]::Render(512)
$preview.Save((Join-Path $root 'docs/assets/app-icon.png'),[Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
