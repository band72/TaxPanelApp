[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{1E08E88E-EA82-4CC2-BDE5-8461FF70EFA3}
AppName=Map Search App
AppVersion=1.0
AppPublisher=Daryl Banks
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
DefaultDirName={autopf}\MapSearchApp
DefaultGroupName=Map Search App
AllowNoIcons=yes
; Output folder for the installer executable
OutputDir=InstallerOutput
OutputBaseFilename=MapSearchApp_Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; The main executable
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\MapSearchApp.exe"; DestDir: "{app}"; Flags: ignoreversion
; All other files and subdirectories in the publish folder
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Note: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\Map Search App"; Filename: "{app}\MapSearchApp.exe"
Name: "{autodesktop}\Map Search App"; Filename: "{app}\MapSearchApp.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\MapSearchApp.exe"; Description: "{cm:LaunchProgram,Map Search App}"; Flags: nowait postinstall skipifsilent
