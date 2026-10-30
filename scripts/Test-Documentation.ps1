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
foreach($path in @('installer/PipOverlay.iss','scripts/Test-InstallerIntegration.ps1','.github/workflows/installer.yml')){
    if(!(Get-Content "$root/$path" -Raw).Contains($version)){$errors+="Version mismatch: $path vs $version"}
}
if($errors.Count){throw ($errors -join "`n")}
Write-Output "PASS documentation: $($files.Count) Markdown files, $links local links, required translations and version $version"
