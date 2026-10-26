#ifndef AppSource
  #define AppSource "..\QuizDesk-5.14.0-win-x64"
#endif
#ifndef SetupOutput
  #define SetupOutput ".."
#endif
#ifndef AppVersion
  #define AppVersion "5.14.0"
#endif
[Setup]
ChangesAssociations=yes
AppId={{B5D71729-73E8-4A5C-A9F8-7685D6781E60}
AppName=QuizDesk
AppVersion={#AppVersion}
AppVerName=QuizDesk {#AppVersion}
AppPublisher=QuizDesk
DefaultDirName={localappdata}\Programs\QuizDesk
DefaultGroupName=QuizDesk
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#SetupOutput}
#ifdef ValidationBuild
OutputBaseFilename=QuizDesk-Setup-Validation
CreateUninstallRegKey=no
UsePreviousAppDir=no
UsePreviousGroup=no
UsePreviousTasks=no
#else
OutputBaseFilename=QuizDesk-Setup-{#AppVersion}-x64
#endif
Compression=lzma2/normal
SolidCompression=yes
WizardStyle=modern
SetupIconFile=QuizDesk.ico
UninstallDisplayIcon={app}\QuizDesk.ico
UninstallDisplayName=QuizDesk
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName=QuizDesk
VersionInfoProductVersion={#AppVersion}
VersionInfoDescription=QuizDesk Installer
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
DisableWelcomePage=no
[Languages]
Name: "vietnamese"; MessagesFile: "compiler:Default.isl,Vietnamese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
[Files]
Source: "{#AppSource}\*"; DestDir: "{app}"; Excludes: "*.log,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "QuizDesk.ico"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
#ifndef ValidationBuild
Name: "{autoprograms}\QuizDesk"; Filename: "{app}\QuizDesk.exe"; WorkingDir: "{app}"; IconFilename: "{app}\QuizDesk.ico"
Name: "{autodesktop}\QuizDesk"; Filename: "{app}\QuizDesk.exe"; WorkingDir: "{app}"; IconFilename: "{app}\QuizDesk.ico"; Tasks: desktopicon
#endif
[Run]
Filename: "{app}\QuizDesk.exe"; Description: "{cm:LaunchProgram,QuizDesk}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
#ifndef ValidationBuild
[Registry]
Root: HKCU; Subkey: "Software\Classes\.quiz\OpenWithProgids"; ValueType: none; ValueName: "QuizDesk.Quiz"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\QuizDesk.Quiz"; ValueType: string; ValueData: "Bộ đề QuizDesk"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\QuizDesk.Quiz\DefaultIcon"; ValueType: string; ValueData: "{app}\QuizDesk.ico,0"
Root: HKCU; Subkey: "Software\Classes\QuizDesk.Quiz\shell\open\command"; ValueType: string; ValueData: """{app}\QuizDesk.exe"" ""%1"""
#endif
[CustomMessages]
vietnamese.CreateDesktopIcon=Tạo biểu tượng trên Desktop
vietnamese.AdditionalIcons=Biểu tượng bổ sung:
vietnamese.LaunchProgram=Mở %1
; Settings and history are stored outside {app}. Do not delete user-created files.
