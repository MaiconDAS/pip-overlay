#ifndef Stage
  #error Stage is required
#endif
#define AppVersion "0.14.1"
[Setup]
AppId={{BFAFEE61-6519-4912-B741-389AC78FA812}
AppName=PiP Overlay
AppVersion={#AppVersion}
AppPublisher=MaiconDAS
AppPublisherURL=https://github.com/MaiconDAS/pip-overlay
AppSupportURL=https://github.com/MaiconDAS/pip-overlay/issues
AppUpdatesURL=https://github.com/MaiconDAS/pip-overlay/releases
DefaultDirName={localappdata}\Programs\PiP Overlay
DefaultGroupName=PiP Overlay
UsePreviousAppDir=yes
DirExistsWarning=no
AppendDefaultDirName=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
WizardStyle=modern
DisableProgramGroupPage=yes
DisableWelcomePage=no
LicenseFile=..\LICENSE
SetupIconFile=..\src\Assets\App.ico
UninstallDisplayIcon={app}\PipOverlay.exe
UninstallDisplayName=PiP Overlay
OutputDir=..\dist\installer
OutputBaseFilename=Instalar-PiP-Overlay
Compression=lzma2
SolidCompression=yes
AppMutex=Local\PipOverlay-Portable-0.1
CloseApplications=yes
RestartApplications=no
Uninstallable=yes
CreateUninstallRegKey=yes
VersionInfoVersion={#AppVersion}
VersionInfoDescription=Instalador do PiP Overlay

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: desktopicon; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Files]
Source: "{#Stage}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\PiP Overlay\PiP Overlay"; Filename: "{app}\PipOverlay.exe"; WorkingDir: "{app}"; IconFilename: "{app}\PipOverlay.exe"; AppUserModelID: "MaiconDAS.PipOverlay"
Name: "{userprograms}\PiP Overlay\Desinstalar PiP Overlay"; Filename: "{uninstallexe}"
Name: "{userdesktop}\PiP Overlay"; Filename: "{app}\PipOverlay.exe"; WorkingDir: "{app}"; IconFilename: "{app}\PipOverlay.exe"; Tasks: desktopicon; AppUserModelID: "MaiconDAS.PipOverlay"
Name: "{app}\Desinstalar PiP Overlay"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\PipOverlay.exe"; WorkingDir: "{app}"; Description: "Abrir PiP Overlay"; Flags: nowait postinstall skipifsilent

[Code]
var PreviousDirectory: String;

function InitializeSetup: Boolean;
begin
  PreviousDirectory := '';
  RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{BFAFEE61-6519-4912-B741-389AC78FA812}_is1', 'InstallLocation', PreviousDirectory);
  if not FileExists(AddBackslash(PreviousDirectory) + 'PipOverlay.exe') then PreviousDirectory := '';
  Result := True;
end;
procedure CopyPreference(Source, Destination: String);
begin
  if FileExists(Source) and not FileExists(Destination) then begin
    ForceDirectories(ExtractFileDir(Destination));
    if not CopyFile(Source, Destination, False) then
      RaiseException('Não foi possível importar as preferências. A pasta antiga permanece intacta.');
  end;
end;

function IsProfileName(Name: String): Boolean;
var I: Integer;
begin
  Result := False;
  if (Length(Name) <> 69) or (Lowercase(Copy(Name,65,5)) <> '.json') then Exit;
  for I := 1 to 64 do
    if Pos(Lowercase(Copy(Name,I,1)), '0123456789abcdef') = 0 then Exit;
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Previous, Target: String; Find: TFindRec;
begin
  if (CurStep = ssPostInstall) and (PreviousDirectory <> '') then begin
    Previous := AddBackslash(PreviousDirectory) + 'data\';
    Target := ExpandConstant('{app}\data\');
    CopyPreference(Previous + 'settings.json', Target + 'settings.json');
    if FindFirst(Previous + 'profiles\*.json', Find) then begin
      try
        repeat
          if ((Find.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0) and IsProfileName(Find.Name) then
            CopyPreference(Previous + 'profiles\' + Find.Name, Target + 'profiles\' + Find.Name);
        until not FindNext(Find);
      finally FindClose(Find); end;
    end;
  end;
end;

function InitializeUninstall: Boolean;
begin
  Result := not CheckForMutexes('Local\PipOverlay-Portable-0.1');
  if not Result and not UninstallSilent then MsgBox('Encerre o PiP Overlay pelo ícone perto do relógio antes de desinstalar.', mbInformation, MB_OK);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if (CurUninstallStep = usPostUninstall) and not UninstallSilent then
    MsgBox('PiP Overlay removido. Preferências e arquivos pessoais, se existirem, foram preservados na pasta de instalação.', mbInformation, MB_OK);
end;
