param([Parameter(Mandatory)][string]$Installer)
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted'){throw 'Interactive installer test requires a disposable GitHub-hosted runner.'}
Add-Type @'
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class WizardProbe {
 public delegate bool EnumProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc c,IntPtr p);
 [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h,EnumProc c,IntPtr p);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowEnabled(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr h,StringBuilder t,int n);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 public static string Text(IntPtr h){var s=new StringBuilder(4096);GetWindowText(h,s,s.Capacity);return s.ToString();}
 public static IntPtr Find(){IntPtr found=IntPtr.Zero;EnumWindows((h,p)=>{if(IsWindowVisible(h)&&Text(h).Contains("PiP Overlay")&&Text(h).Contains("Instalador"))found=h;return true;},IntPtr.Zero);return found;}
 public static IntPtr[] Children(IntPtr h){var list=new List<IntPtr>();EnumChildWindows(h,(c,p)=>{if(IsWindowVisible(c))list.Add(c);return true;},IntPtr.Zero);return list.ToArray();}
}
'@
$target=Join-Path $env:RUNNER_TEMP ('pip-wizard-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $target | Out-Null
# Visible UI is intentional: this tests the actual interactive wizard, stopping before installation.
$process=Start-Process -FilePath $Installer -ArgumentList @('/NORESTART',('/DIR="'+$target+'"')) -PassThru
$deadline=(Get-Date).AddSeconds(45)
try {
    do {Start-Sleep -Milliseconds 250;$window=[WizardProbe]::Find()}while($window -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline)
    if($window -eq [IntPtr]::Zero){throw 'Installer wizard did not appear'}
    $ready=$false
    for($step=0;$step -lt 10;$step++){
        $children=[WizardProbe]::Children($window)
        $texts=@($children | ForEach-Object {[WizardProbe]::Text($_)})
        Write-Output ('Wizard step '+$step+': '+($texts -join ' | '))
        if(($texts -join ' ') -match 'Importar preferências|Pasta da versão anterior'){throw 'Removed import page is still present'}
        $install=$children | Where-Object {([WizardProbe]::Text($_) -replace '&','') -eq 'Instalar'} | Select-Object -First 1
        if($install){$ready=$true;break}
        $accept=$children | Where-Object {([WizardProbe]::Text($_) -replace '&','') -match '^Eu aceito|^Aceito'} | Select-Object -First 1
        if($accept){[WizardProbe]::SendMessage($accept,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null;Start-Sleep -Milliseconds 100}
        $next=$children | Where-Object {[WizardProbe]::Text($_) -match 'Avançar' -and [WizardProbe]::IsWindowEnabled($_)} | Select-Object -First 1
        if(!$next){throw 'Wizard cannot advance without extra input'}
        [WizardProbe]::SendMessage($next,0xF5,[IntPtr]::Zero,[IntPtr]::Zero)|Out-Null
        Start-Sleep -Milliseconds 300
    }
    if(!$ready){throw 'Wizard did not reach the install confirmation'}
    Write-Output 'PASS: interactive wizard reached Install in an existing directory without an import page or required extra path.'
}finally{
    # Stop only our installer process tree before it can write system integration.
    & taskkill.exe /PID $process.Id /T /F | Out-Null
}
