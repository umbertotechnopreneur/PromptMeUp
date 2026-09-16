; SPDX-License-Identifier: MIT
; Build only through scripts/build-windows-installer.ps1, which validates the payload.
#ifndef AppVersion
  #error AppVersion is required.
#endif
#ifndef AppArchitecture
  #error AppArchitecture is required.
#endif
#ifndef PublishDirectory
  #error PublishDirectory is required.
#endif
#ifndef RepositoryDirectory
  #error RepositoryDirectory is required.
#endif
#if AppArchitecture == "x64"
  #define TargetArchitecture "x64os"
#elif AppArchitecture == "arm64"
  #define TargetArchitecture "arm64"
#else
  #error AppArchitecture must be x64 or arm64.
#endif

[Setup]
AppId=UmbertoGiacobbi.PromptMeUp
AppName=PromptMeUp
AppVersion={#AppVersion}
AppPublisher=Umberto Giacobbi
AppPublisherURL=https://github.com/umbertotechnopreneur/PromptMeUp
AppSupportURL=https://github.com/umbertotechnopreneur/PromptMeUp/issues
VersionInfoVersion={#AppVersion}.0
DefaultDirName={localappdata}\Programs\PromptMeUp
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed={#TargetArchitecture}
ArchitecturesInstallIn64BitMode={#TargetArchitecture}
MinVersion=10.0.19041
ChangesEnvironment=yes
UninstallDisplayIcon={app}\hm.exe
SetupIconFile={#RepositoryDirectory}\assets\PromptMeUp.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SignedUninstaller=no

[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Messages]
FinishedLabel=PromptMeUp is installed.%n%nOpen a new terminal and type hm to get started. PowerShell 7 is required for command execution.%n%nIf you already installed PromptMeUp as an MSIX package, remove that package to avoid conflicting hm command aliases.

[Code]
const
  InstallerKey = 'Software\Umberto Giacobbi\PromptMeUp\Installer';
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\UmbertoGiacobbi.PromptMeUp_is1';

function NormalizePathEntry(Value: String): String;
begin
  Value := Trim(Value);
  if (Length(Value) >= 2) and (Value[1] = '"') and (Value[Length(Value)] = '"') then
    Value := Copy(Value, 2, Length(Value) - 2);
  Result := Lowercase(RemoveBackslashUnlessRoot(Value));
end;

function PathHasEntry(PathValue, Entry: String): Boolean;
var
  StartAt, Separator: Integer;
begin
  Result := False;
  StartAt := 1;
  for Separator := 1 to Length(PathValue) + 1 do
  begin
    if (Separator > Length(PathValue)) or (PathValue[Separator] = ';') then
    begin
      if NormalizePathEntry(Copy(PathValue, StartAt, Separator - StartAt)) = NormalizePathEntry(Entry) then
      begin
        Result := True;
        Exit;
      end;
      StartAt := Separator + 1;
    end;
  end;
end;

function ReadUserPath: String;
begin
  Result := '';
  if RegValueExists(HKCU, 'Environment', 'Path') and
     not RegQueryStringValue(HKCU, 'Environment', 'Path', Result) then
    RaiseException('Cannot read your user PATH. The installer cannot safely change it.');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  PreviousDirectory, ExistingExecutable, CurrentPath: String;
  InstalledVersion, PackageVersion: Int64;
begin
  Result := '';
  if Pos(';', ExpandConstant('{app}')) > 0 then
  begin
    Result := 'The installation folder cannot contain a semicolon because it is added to PATH.';
    Exit;
  end;
  if RegQueryStringValue(HKCU, UninstallKey, 'InstallLocation', PreviousDirectory) then
  begin
    if NormalizePathEntry(PreviousDirectory) <> NormalizePathEntry(ExpandConstant('{app}')) then
    begin
      Result := 'Use the existing installation folder, or uninstall PromptMeUp before moving it.';
      Exit;
    end;
  end
  else if DirExists(ExpandConstant('{app}')) then
  begin
    Result := 'The installation folder already exists and is not managed by this installer. Remove or relocate the previous installation first.';
    Exit;
  end;
  ExistingExecutable := ExpandConstant('{app}\hm.exe');
  if FileExists(ExistingExecutable) then
  begin
    if not GetPackedVersion(ExistingExecutable, InstalledVersion) or
       not StrToVersion('{#AppVersion}.0', PackageVersion) then
    begin
      Result := 'Cannot read the installed version. Uninstall PromptMeUp before reinstalling.';
      Exit;
    end;
    if ComparePackedVersion(InstalledVersion, PackageVersion) > 0 then
    begin
      Result := 'A newer version of PromptMeUp is installed. Uninstall it before installing an older version.';
      Exit;
    end;
  end;
  CurrentPath := ReadUserPath;
  if not PathHasEntry(CurrentPath, ExpandConstant('{app}')) and
     (Length(CurrentPath) + Length(ExpandConstant('{app}')) + 1 > 32766) then
    Result := 'Your user PATH is too long to add the installation folder safely.';
end;

procedure AddUserPath;
var
  CurrentPath, Entry, UpdatedPath, PreviousOwnedEntry: String;
  HadOwnership: Boolean;
begin
  Entry := ExpandConstant('{app}');
  CurrentPath := ReadUserPath;
  if PathHasEntry(CurrentPath, Entry) then
    Exit;
  HadOwnership := RegQueryStringValue(HKCU, InstallerKey, 'PathEntry', PreviousOwnedEntry);
  if HadOwnership and (NormalizePathEntry(PreviousOwnedEntry) <> NormalizePathEntry(Entry)) then
    RaiseException('The previous PATH entry belongs to another installation folder.');
  UpdatedPath := CurrentPath;
  if UpdatedPath <> '' then
    UpdatedPath := UpdatedPath + ';';
  UpdatedPath := UpdatedPath + Entry;
  if Length(UpdatedPath) > 32766 then
    RaiseException('Your user PATH is too long to add the installation folder safely.');
  { Keep ownership across upgrades, and never claim a pre-existing entry. }
  if not RegWriteStringValue(HKCU, InstallerKey, 'PathEntry', Entry) then
    RaiseException('Cannot record the installer PATH entry.');
  if not RegWriteStringValue(HKCU, 'Environment', 'Path', UpdatedPath) then
  begin
    if not HadOwnership then
      RegDeleteValue(HKCU, InstallerKey, 'PathEntry');
    RaiseException('Cannot add PromptMeUp to your user PATH.');
  end;
end;

procedure RemoveUserPath;
var
  OwnedEntry, CurrentPath, UpdatedPath, Entry: String;
  StartAt, Separator: Integer;
  HaveEntry: Boolean;
begin
  if not RegQueryStringValue(HKCU, InstallerKey, 'PathEntry', OwnedEntry) then
    Exit;
  if NormalizePathEntry(OwnedEntry) <> NormalizePathEntry(ExpandConstant('{app}')) then
    RaiseException('The recorded PATH entry belongs to another installation folder.');
  CurrentPath := ReadUserPath;
  UpdatedPath := '';
  HaveEntry := False;
  StartAt := 1;
  { Preserve every other entry, including its spelling, order, and empty entries. }
  for Separator := 1 to Length(CurrentPath) + 1 do
  begin
    if (Separator > Length(CurrentPath)) or (CurrentPath[Separator] = ';') then
    begin
      Entry := Copy(CurrentPath, StartAt, Separator - StartAt);
      if NormalizePathEntry(Entry) <> NormalizePathEntry(OwnedEntry) then
      begin
        if HaveEntry then
          UpdatedPath := UpdatedPath + ';';
        UpdatedPath := UpdatedPath + Entry;
        HaveEntry := True;
      end;
      StartAt := Separator + 1;
    end;
  end;
  if (UpdatedPath <> CurrentPath) and not RegWriteStringValue(HKCU, 'Environment', 'Path', UpdatedPath) then
    RaiseException('Cannot remove the PromptMeUp entry from your user PATH.');
  if not RegDeleteValue(HKCU, InstallerKey, 'PathEntry') then
    RaiseException('Cannot remove the installer PATH record.');
  RegDeleteKeyIfEmpty(HKCU, InstallerKey);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    AddUserPath;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveUserPath;
end;
