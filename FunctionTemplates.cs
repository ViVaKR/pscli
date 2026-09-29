namespace pscli;

internal static class FunctionTemplates
{
  public static readonly Dictionary<string, string> Descriptions = new()
  {
    ["basic"] = "파라미터 없는 최소형 함수",
    ["cmdlet"] = "CmdletBinding + Mandatory 파라미터가 포함된 표준 함수 (기본값)",
    ["pipeline"] = "파이프라인 입력(ValueFromPipeline)을 받는 함수",
  };

  public static string Render(string key, string functionName)
  {
    // 승인된 동사인지 먼저 체크하고 경고만 표시 (막지는 않음 — 사용자 선택 존중)
    ApprovedVerbs.WarnIfNotApproved(functionName);

    return key switch
    {
      "basic" => $$"""
        function {{functionName}} {
            param()


        }
        """,

      "pipeline" => $$"""
        function {{functionName}} {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory, ValueFromPipeline)]
                [object]$InputObject
            )

            process {

            }
        }
        """,

      _ => $$"""
        function {{functionName}} {
            [CmdletBinding()]
            param(
                [Parameter(Mandatory)]
                [string]$Name
            )


        }
        """,
    };
  }
}
