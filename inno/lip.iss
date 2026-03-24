#define AppName "lip"
#define SourceDir "..\\.tmp\\artifacts"
#define OutputDir "..\\.tmp\\nsis"

#if BuildArch == "arm64"
  #define RuntimeSuffix "win-arm64"
#else
  #define RuntimeSuffix "win-x64"
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
