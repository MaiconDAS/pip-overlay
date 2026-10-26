$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$bitmap=[Drawing.Bitmap]::new(1280,640)
$g=[Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint=[Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$bg=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0,0,1280,640),[Drawing.ColorTranslator]::FromHtml('#0F141E'),[Drawing.ColorTranslator]::FromHtml('#142B31'),25)
$g.FillRectangle($bg,0,0,1280,640)
$mint=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#6CE7C4'))
$white=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#F3F8FA'))
$muted=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#AFBFCE'))
$dark=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#0B111B'))
$panel=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#24594F'))
$pen=[Drawing.Pen]::new($mint,4)
$line=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#29434E'),2)
$large=[Drawing.Font]::new('Segoe UI',57,[Drawing.FontStyle]::Bold)
$medium=[Drawing.Font]::new('Segoe UI',23)
$small=[Drawing.Font]::new('Segoe UI',13)
$g.DrawRectangle($pen,68,82,74,55);$g.FillRectangle($mint,103,110,31,20)
$g.DrawString('PiP Overlay',$large,$white,60,220)
$g.DrawString('Seu vídeo. Seu jogo. Seu espaço.',$medium,$muted,66,322)
$g.DrawString('WINDOWS  /  DIRECT3D 9',$small,$mint,70,426)
$g.DrawString('github.com/MaiconDAS/pip-overlay',$small,$muted,70,558)
$g.FillRectangle($dark,803,175,398,278);$g.DrawRectangle($line,803,175,398,278)
$points=[Drawing.Point[]]@([Drawing.Point]::new(827,382),[Drawing.Point]::new(900,303),[Drawing.Point]::new(955,350),[Drawing.Point]::new(1040,260),[Drawing.Point]::new(1178,382))
$g.DrawLines($line,$points)
$g.FillRectangle($panel,1007,199,170,106);$g.DrawRectangle($pen,1007,199,170,106)
$g.FillPolygon($mint,[Drawing.Point[]]@([Drawing.Point]::new(1077,230),[Drawing.Point]::new(1111,251),[Drawing.Point]::new(1077,272)))
$g.FillEllipse($mint,826,421,8,8);$g.DrawString('Vídeo por perto. Jogo em foco.',$small,$muted,843,413)
$bitmap.Save((Join-Path $root 'docs/assets/social-preview.png'),[Drawing.Imaging.ImageFormat]::Png)
foreach($item in @($large,$medium,$small,$pen,$line,$mint,$white,$muted,$dark,$panel,$bg,$g,$bitmap)){$item.Dispose()}
