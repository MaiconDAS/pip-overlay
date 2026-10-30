$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$files=@(Get-ChildItem -LiteralPath $root -File -Filter '*.md')+@(Get-ChildItem -LiteralPath "$root/docs","$root/.github" -Recurse -File -Filter '*.md')
$errors=@();$links=0
foreach($file in $files){
    $text=Get-Content -LiteralPath $file.FullName -Raw
    foreach($match in [regex]::Matches($text,'\]\(([^\s)]+)(?:\s+"[^"]*")?\)')){
        $target=$match.Groups[1].Value.Trim('<','>')
        if($target -match '^(https?:|mailto:|#|codex:)'){continue}
        $target=[Uri]::UnescapeDataString(($target -split '#')[0])
        if(!$target){continue}
        $links++
        if(!(Test-Path -LiteralPath (Join-Path $file.DirectoryName $target))){$errors+="$($file.FullName): $target"}
    }
}
foreach($required in @('README.md','README.en.md','PRIVACY.md','SECURITY.md','docs/pt-BR/PRIVACY.md','docs/pt-BR/SECURITY.md','docs/en/USAGE.md','docs/en/COMPATIBILITY.md','docs/en/DEVELOPMENT.md','docs/en/ARCHITECTURE.md','docs/en/CONTRIBUTING.md')){
    if(!(Test-Path -LiteralPath "$root/$required")){$errors+="Missing $required"}
}
[xml]$project=Get-Content "$root/src/PipOverlay.csproj"
$version=[string]($project.Project.PropertyGroup.Version | Where-Object {$_} | Select-Object -First 1)
if(!$version){throw 'Missing project version'}
$pairs=Get-Content "$root/docs/languages.json" -Raw | ConvertFrom-Json
$covered=@()
foreach($pair in $pairs){
    foreach($language in @('pt','en')){
        $path=$pair.$language
        $covered+=$path
        if(!(Test-Path -LiteralPath "$root/$path")){$errors+="Missing language edition: $path";continue}
        $other=if($language -eq 'pt'){$pair.en}else{$pair.pt}
        $relative=[IO.Path]::GetRelativePath((Split-Path "$root/$path"),"$root/$other").Replace('\','/')
        if(!(Get-Content "$root/$path" -Raw).Contains("]($relative)")){$errors+="Missing language switch: $path -> $other"}
    }
}
foreach($file in Get-ChildItem "$root/docs" -Recurse -File -Filter '*.md'){
    $relative=[IO.Path]::GetRelativePath($root,$file.FullName).Replace('\','/')
    if($relative -notin $covered){$errors+="Document without a language pair: $relative"}
}
$installerName=& "$PSScriptRoot/Get-InstallerName.ps1"
if($installerName -ne "PiP-Overlay v$version.exe"){$errors+='Installer filename does not match app version'}
if(!(Get-Content "$root/installer/PipOverlay.iss" -Raw).Contains('OutputBaseFilename=PiP-Overlay v{#AppVersion}')){$errors+='Inno output filename is not versioned'}
foreach($path in @('installer/PipOverlay.iss','scripts/Test-InstallerIntegration.ps1','.github/workflows/installer.yml')){
    if(!(Get-Content "$root/$path" -Raw).Contains($version)){$errors+="Version mismatch: $path vs $version"}
}
if($errors.Count){throw ($errors -join "`n")}
Write-Output "PASS documentation: $($files.Count) Markdown files, $links local links, $($pairs.Count) language pairs and version $version"
