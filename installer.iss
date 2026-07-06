[Setup]
AppName=FastAccountingSoftware
AppVersion=1.0.0
AppPublisher=Segun Micheal
AppPublisherURL=https://github.com/segunmicheal27/FastAccountingSoftware
AppSupportURL=https://github.com/segunmicheal27/FastAccountingSoftware/issues
AppContact=segunmicheal27@yahoo.com
DefaultDirName={autopf}\FastAccountingSoftware
DefaultGroupName=FastAccountingSoftware
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=Installer
OutputBaseFilename=FastAccountingSoftware-v1.0.0-Premium-Setup
SetupIconFile=Assets\WindowIcon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\FastAccountingSoftware.exe
VersionInfoVersion=1.0.0.0
VersionInfoCompany=Segun Micheal
VersionInfoDescription=FastAccountingSoftware - Smart Accounting for Business

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application files from Release build
Source: "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FastAccountingSoftware"; Filename: "{app}\FastAccountingSoftware.exe"
Name: "{group}\{cm:UninstallProgram,FastAccountingSoftware}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\FastAccountingSoftware"; Filename: "{app}\FastAccountingSoftware.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FastAccountingSoftware.exe"; Description: "{cm:LaunchProgram,FastAccountingSoftware}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
