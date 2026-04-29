
[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{DA4A987D-8B8B-4C9D-9D7A-41AB2CD5E99D}
AppName=MapSearchApp
AppVersion=1.0.3
AppPublisher=Development
DefaultDirName={autopf}\MapSearchApp
DisableProgramGroupPage=yes
; Output directory for the generated installer
OutputDir=Installer
OutputBaseFilename=MapSearchApp.1.0.3
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; Application Icon
SetupIconFile=MapSearchApp.ico
UninstallDisplayIcon={app}\MapSearchApp.ico
; Self-contained build: no .NET runtime prerequisite check required

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main Executable
Source: "bin\Release\net8.0\win-x64\publish\MapSearchApp.exe"; DestDir: "{app}"; Flags: ignoreversion
; All other published files
Source: "bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Default Base Map
Source: "Tax-Maps\*"; DestDir: "{app}\Tax-Maps"; Flags: ignoreversion recursesubdirs createallsubdirs
; Application Icon (displayed in Add/Remove Programs and shortcuts)
Source: "MapSearchApp.ico"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\MapSearchApp"; Filename: "{app}\MapSearchApp.exe"; IconFilename: "{app}\MapSearchApp.ico"
Name: "{autodesktop}\MapSearchApp"; Filename: "{app}\MapSearchApp.exe"; IconFilename: "{app}\MapSearchApp.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\MapSearchApp.exe"; Description: "{cm:LaunchProgram,MapSearchApp}"; Flags: nowait postinstall skipifsilent
