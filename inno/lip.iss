#define AppName "lip"
#define SourceDir "..\\.tmp\\artifacts"
#define OutputDir "..\\.tmp\\inno"
#ifndef DotNetRuntimeVersion
  #define DotNetRuntimeVersion "10.0.5"
#endif

#ifndef DotNetRuntimeMajor
  #define DotNetRuntimeMajor "10"
#endif

#if BuildArch == "arm64"
  #define RuntimeSuffix "win-arm64"
  #define DotNetRuntimeArch "arm64"
#else
  #define RuntimeSuffix "win-x64"
  #define DotNetRuntimeArch "x64"
#endif

[Setup]
AppId={{C9BEB1D4-E698-4D84-A644-9E0C4B2E72BD}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
OutputDir={#OutputDir}
OutputBaseFilename=lip-{#AppVersion}-{#RuntimeSuffix}-setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed={#BuildArch}
ArchitecturesInstallIn64BitMode={#BuildArch}
ChangesEnvironment=yes

[Files]
Source: "{#SourceDir}\lip.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\lipd.exe"; DestDir: "{app}"; Flags: ignoreversion

[Code]
const
  EnvironmentKey = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment';
  DotNetInstallLocationRegistryKey = 'SOFTWARE\dotnet\Setup\InstalledVersions\{#DotNetRuntimeArch}';
  DotNetRuntimeInstallArgs = '/install /quiet /norestart';

var
  DotNetRuntimeNeedsRestart: Boolean;

function NormalizePathValue(Value: string): string;
begin
  Result := Trim(Value);
  while (Result <> '') and (Result[Length(Result)] = ';') do
    Delete(Result, Length(Result), 1);
end;

function ContainsPath(PathValue: string; Dir: string): Boolean;
begin
  Result := Pos(';' + Uppercase(Dir) + ';', ';' + Uppercase(PathValue) + ';') > 0;
end;

function AddPathEntry(PathValue: string; Dir: string): string;
begin
  PathValue := NormalizePathValue(PathValue);
  if (PathValue = '') then
    Result := Dir
  else if ContainsPath(PathValue, Dir) then
    Result := PathValue
  else
    Result := PathValue + ';' + Dir;
end;

function RemovePathEntry(PathValue: string; Dir: string): string;
var
  SearchValue: string;
  EntryPos: Integer;
begin
  SearchValue := ';' + NormalizePathValue(PathValue) + ';';
  EntryPos := Pos(';' + Uppercase(Dir) + ';', Uppercase(SearchValue));

  while EntryPos > 0 do
  begin
    Delete(SearchValue, EntryPos, Length(Dir) + 1);
    EntryPos := Pos(';' + Uppercase(Dir) + ';', Uppercase(SearchValue));
  end;

  if (SearchValue <> '') and (SearchValue[1] = ';') then
    Delete(SearchValue, 1, 1);
  if (SearchValue <> '') and (SearchValue[Length(SearchValue)] = ';') then
    Delete(SearchValue, Length(SearchValue), 1);

  Result := SearchValue;
end;

function DotNetRootDir: string;
begin
  Result := ExpandConstant('{commonpf}\dotnet');
end;

function GetRegisteredDotNetRootDir: string;
begin
  if not RegQueryStringValue(HKLM32, DotNetInstallLocationRegistryKey, 'InstallLocation', Result) then
    Result := '';
end;

function DotNetSharedRuntimeDir(const RootDir: string): string;
begin
  Result := AddBackslash(RootDir) + 'shared\Microsoft.NETCore.App';
end;

function HasDotNetRuntimeInstalled: Boolean;
var
  FindRec: TFindRec;
  RuntimeRootDir: string;
  RuntimeSharedDir: string;
begin
  Result := False;

  RuntimeRootDir := GetRegisteredDotNetRootDir;
  if RuntimeRootDir = '' then
  begin
    if '{#DotNetRuntimeArch}' = 'x64' then
      RuntimeRootDir := DotNetRootDir + '\x64'
    else
      RuntimeRootDir := DotNetRootDir;
  end;

  RuntimeSharedDir := DotNetSharedRuntimeDir(RuntimeRootDir);
  if not DirExists(RuntimeSharedDir) then
    exit;

  if not FindFirst(RuntimeSharedDir + '\{#DotNetRuntimeMajor}.*', FindRec) then
    exit;

  try
    repeat
      if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
      begin
        Result := True;
        exit;
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

function LoadTrimmedStringFromFile(const FileName: string): string;
var
  Content: AnsiString;
begin
  if not LoadStringFromFile(FileName, Content) then
    RaiseException(Format('Failed to read "%s".', [FileName]));

  Result := Trim(String(Content));
end;

function GetDotNetRuntimeInstallerFileName(const Version: string): string;
begin
  Result := Format('dotnet-runtime-%s-win-{#DotNetRuntimeArch}.exe', [Version]);
end;

function GetDotNetRuntimeInstallerUrl(const Version: string): string;
begin
  Result := Format('https://builds.dotnet.microsoft.com/dotnet/Runtime/%s/%s', [Version, GetDotNetRuntimeInstallerFileName(Version)]);
end;

var
  ProgressPage: TOutputProgressWizardPage;
function OnDownloadProgress(const Url, Filename: String; const Progress, ProgressMax: Int64): Boolean;
begin
  ProgressPage.SetProgress(Progress, ProgressMax);
  Result := True;
end;

function EnsureDotNetRuntimeInstalled: string;
var
  ExitCode: Integer;
  InstallerUrl: string;
  InstallerPath: string;
  DotNetRuntimeVersion: string;
begin
  Result := '';

  if HasDotNetRuntimeInstalled then
    exit;

  try
    Log('Missing .NET Runtime {#DotNetRuntimeMajor}.x, downloading prerequisite installer.');
    DotNetRuntimeVersion := '{#DotNetRuntimeVersion}'
    InstallerUrl := GetDotNetRuntimeInstallerUrl(DotNetRuntimeVersion);

    ProgressPage := CreateOutputProgressPage('Download .NET', GetDotNetRuntimeInstallerFileName(DotNetRuntimeVersion))
    Log(Format('Downloading %s', [InstallerUrl]));
    ProgressPage.Show;
    DownloadTemporaryFile(
      InstallerUrl,
      GetDotNetRuntimeInstallerFileName(DotNetRuntimeVersion),
      '',
      @OnDownloadProgress
    );

    InstallerPath := ExpandConstant('{tmp}\') + GetDotNetRuntimeInstallerFileName(DotNetRuntimeVersion);

    Log(Format('Running .NET Runtime installer: %s %s', [InstallerPath, DotNetRuntimeInstallArgs]));
    if not Exec(
      InstallerPath,
      DotNetRuntimeInstallArgs,
      '',
      SW_SHOWNORMAL,
      ewWaitUntilTerminated,
      ExitCode
    ) then
    begin
      Result := Format('Failed to launch the Microsoft .NET Runtime installer: %s', [SysErrorMessage(ExitCode)]);
      exit;
    end;

    if (ExitCode <> 0) and (ExitCode <> 3010) then
    begin
      Result := Format('The Microsoft .NET Runtime installer exited with code %d.', [ExitCode]);
      exit;
    end;

    DotNetRuntimeNeedsRestart := ExitCode = 3010;

    if not HasDotNetRuntimeInstalled then
      Result := 'The Microsoft .NET Runtime installer finished, but .NET Runtime 10.x was still not detected.';
  except
    Result := GetExceptionMessage;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := EnsureDotNetRuntimeInstalled;
  NeedsRestart := DotNetRuntimeNeedsRestart and (Result <> '');
end;

function NeedRestart: Boolean;
begin
  Result := DotNetRuntimeNeedsRestart;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  PathValue: string;
begin
  if CurStep <> ssPostInstall then
    exit;

  if not RegQueryStringValue(HKLM, EnvironmentKey, 'Path', PathValue) then
    PathValue := '';

  PathValue := AddPathEntry(PathValue, ExpandConstant('{app}'));
  RegWriteExpandStringValue(HKLM, EnvironmentKey, 'Path', PathValue);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  PathValue: string;
begin
  if CurUninstallStep <> usUninstall then
    exit;
  if not RegQueryStringValue(HKLM, EnvironmentKey, 'Path', PathValue) then
    exit;

  PathValue := RemovePathEntry(PathValue, ExpandConstant('{app}'));
  RegWriteExpandStringValue(HKLM, EnvironmentKey, 'Path', PathValue);
end;
