namespace pscli;

internal static class ModuleTemplates
{
    public static string Psm1Loader() => """
    # 이 파일은 pscli가 자동 생성했습니다.
    # Public/Private 폴더의 모든 .ps1 파일을 로드하고,
    # Public 폴더의 함수만 외부로 노출합니다.

    $public  = @(Get-ChildItem -Path "$PSScriptRoot\Public\*.ps1"  -ErrorAction SilentlyContinue)
    $private = @(Get-ChildItem -Path "$PSScriptRoot\Private\*.ps1" -ErrorAction SilentlyContinue)

    foreach ($file in @($public + $private)) {
        try {
            . $file.FullName
        }
        catch {
            Write-Error "Failed to import $($file.FullName): $_"
        }
    }

    Export-ModuleMember -Function $public.BaseName
    """;

    public static string Psd1Manifest(string moduleName, string version, string author, string description)
    {
        string guid = Guid.NewGuid().ToString();
        string safeAuthor = string.IsNullOrWhiteSpace(author) ? "''" : $"'{author}'";
        string safeDescription = string.IsNullOrWhiteSpace(description) ? "''" : $"'{description}'";

        return $$"""
      @{
          RootModule        = '{{moduleName}}.psm1'
          ModuleVersion     = '{{version}}'
          GUID              = '{{guid}}'
          Author            = {{safeAuthor}}
          Description       = {{safeDescription}}
          PowerShellVersion = '7.0'
          FunctionsToExport = @('*')
          CmdletsToExport   = @()
          VariablesToExport = @()
          AliasesToExport   = @()
          PrivateData       = @{
              PSData = @{
                  Tags = @()
              }
          }
      }
      """;
    }

    public static string PesterStub(string moduleName, string sampleFuncName) => $$"""
    BeforeAll {
        Import-Module "$PSScriptRoot/../{{moduleName}}.psd1" -Force
    }

    Describe '{{sampleFuncName}}' {
        It 'runs without throwing' {
            { {{sampleFuncName}} } | Should -Not -Throw
        }
    }
    """;

    public static string GitIgnore() => """
    bin/
    obj/
    *.psd1.bak
    TestResults/
    .vscode/
    """;

    public static string ReadmeMd(string moduleName, string description) => $"""
    # {moduleName}

    {(string.IsNullOrWhiteSpace(description) ? "설명을 추가하세요." : description)}

    ## 설치
    `Import-Module ./{moduleName}.psd1`

    ## 구조

    - `Public/` — 외부에 노출되는 함수
    - `Private/` — 내부 헬퍼 함수
    - `Tests/` — Pester 테스트
    """;
}
