[Setup]
; App Information
AppName=MapSearchApp
AppVersion=1.0
AppPublisher=BoundaryQC Default Publisher
AppId={{E6124B51-689B-4D88-AC48-DE8DBCD38B42}

; Windows Environment and Deployment
DefaultDirName={autopf}\MapSearchApp
DefaultGroupName=MapSearchApp
OutputDir=.\Installer
OutputBaseFilename=MapSearchApp_Setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copy the single-file MapSearchApp.exe standalone executable. 
Source: "bin\Release\net8.0\win-x64\publish\MapSearchApp.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\MapSearchApp"; Filename: "{app}\MapSearchApp.exe"
Name: "{group}\{cm:UninstallProgram,MapSearchApp}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MapSearchApp"; Filename: "{app}\MapSearchApp.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MapSearchApp.exe"; Description: "{cm:LaunchProgram,MapSearchApp}"; Flags: nowait postinstall skipifsilent
