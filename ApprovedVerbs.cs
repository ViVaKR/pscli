using System.Diagnostics;

namespace pscli;

internal static class ApprovedVerbs
{
  // pscli 빌드 시점 기준 PowerShell 승인 동사 전체 목록 (100개)
  // Get-Verb | Select-Object -ExpandProperty Verb 로 뽑아 갱신
  private static readonly HashSet<string> Verbs = new(StringComparer.OrdinalIgnoreCase)
  {
    "Add", "Approve", "Assert", "Backup", "Block", "Build", "Checkpoint",
    "Clear", "Close", "Compare", "Complete", "Compress", "Confirm",
    "Connect", "Convert", "ConvertFrom", "ConvertTo", "Copy", "Debug",
    "Deny", "Deploy", "Disable", "Disconnect", "Dismount", "Edit",
    "Enable", "Enter", "Exit", "Expand", "Export", "Find", "Format",
    "Get", "Grant", "Group", "Hide", "Import", "Initialize", "Install",
    "Invoke", "Join", "Limit", "Lock", "Measure", "Merge", "Mount",
    "Move", "New", "Open", "Optimize", "Out", "Ping", "Pop", "Protect",
    "Publish", "Push", "Read", "Receive", "Redo", "Register", "Remove",
    "Rename", "Repair", "Request", "Reset", "Resize", "Resolve",
    "Restart", "Restore", "Resume", "Revoke", "Save", "Search", "Select",
    "Send", "Set", "Show", "Skip", "Split", "Start", "Step", "Stop",
    "Submit", "Suspend", "Switch", "Sync", "Test", "Trace", "Unblock",
    "Undo", "Uninstall", "Unlock", "Unprotect", "Unpublish", "Unregister",
    "Update", "Use", "Wait", "Watch", "Write",
  };

  // 흔한 오타/관례 매핑 — 승인 안 된 동사를 쓰면 대안을 제안해줌
  private static readonly Dictionary<string, string> CommonMistakes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["Create"] = "New",
      ["Delete"] = "Remove",
      ["Fetch"] = "Get",
      ["Run"] = "Invoke",
      ["List"] = "Get",
      ["Execute"] = "Invoke",
      ["Change"] = "Set",
      ["Modify"] = "Set",
      ["Load"] = "Import",
      ["Unload"] = ("Remove"),
      ["Validate"] = "Test",
      ["Check"] = "Test",
      ["Refresh"] = "Update",
      ["Print"] = "Write",
      ["Display"] = "Show",
    };

  public static void WarnIfNotApproved(string functionName)
  {
    int dashIdx = functionName.IndexOf('-');
    if (dashIdx <= 0) return; // Verb-Noun 형식이 아니면 검증 스킵

    string verb = functionName[..dashIdx];

    if (Verbs.Contains(verb)) return; // 승인됨, 조용히 통과

    Console.WriteLine($"⚠️  '{verb}'는 PowerShell 승인 동사 목록에 없어. 'Get-Verb'로 대안을 확인해봐.");

    if (CommonMistakes.TryGetValue(verb, out var suggestion))
      Console.WriteLine($"    혹시 '{suggestion}-{functionName[(dashIdx + 1)..]}'를 찾는 거야?");
  }
}
