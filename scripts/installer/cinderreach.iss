; Cinderreach Windows installer (Inno Setup 6). Built by scripts/build-installer.sh,
; which passes SrcDir (Windows player build), OutDir, AppVersion, Commit and IconFile.
; Per-user install (no admin) into %LOCALAPPDATA%\Programs\Cinderreach; saves in
; LocalLow\Unity Technologies\Cinderreach are left alone on uninstall.

#ifndef SrcDir
  #error SrcDir is required (pass /DSrcDir=...)
#endif
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Commit
  #define Commit "dev"
#endif
; Join argument baked into the shortcuts so a fresh client finds the live
; cluster server (unity.korczewski.de) straight away. Override with
; /DServerJoin="-join myhost:7777" when needed.
#ifndef ServerJoin
  #define ServerJoin "-join unity.korczewski.de"
#endif

[Setup]
AppId={{8CAB378D-5F92-4EC2-A01E-A965255239A3}
AppName=Cinderreach
AppVersion={#AppVersion}
AppVerName=Cinderreach {#AppVersion} ({#Commit})
AppPublisher=Patrick Korczewski
DefaultDirName={autopf}\Cinderreach
DefaultGroupName=Cinderreach
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#OutDir}
OutputBaseFilename=Cinderreach-Setup-{#AppVersion}-{#Commit}
#ifdef IconFile
SetupIconFile={#IconFile}
#endif
UninstallDisplayIcon={app}\Cinderreach.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SrcDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "*_BurstDebugInformation_DoNotShip,BUILD_INFO.json,shotcheck*,autoplay*"
#ifdef IconFile
Source: "{#IconFile}"; DestDir: "{app}"; DestName: "Cinderreach.ico"; Flags: ignoreversion
#endif

[InstallDelete]
; Old player data must not mix with a new build's serialized assets.
Type: filesandordirs; Name: "{app}\cinderreach_Data"

[Icons]
Name: "{group}\Cinderreach"; Filename: "{app}\cinderreach.exe"; Parameters: "{#ServerJoin}"; IconFilename: "{app}\Cinderreach.ico"
Name: "{group}\{cm:UninstallProgram,Cinderreach}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Cinderreach"; Filename: "{app}\cinderreach.exe"; Parameters: "{#ServerJoin}"; IconFilename: "{app}\Cinderreach.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\cinderreach.exe"; Parameters: "{#ServerJoin}"; Description: "{cm:LaunchProgram,Cinderreach}"; Flags: nowait postinstall skipifsilent
