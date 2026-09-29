using System.Text;

namespace pscli;

// =========================================================================
// ModuleScaffolder.cs
// `pscli new module` — Public/Private 폴더 구조 + .psm1 로더 + .psd1 매니페스트
// 를 갖춘 표준 PowerShell 모듈 골격을 생성한다.
// =========================================================================
internal static class ModuleScaffolder
{
  public static void Run(string[] args)
  {
    string? moduleNameArg = null;
    string outputDir = ".";
    bool force = false;
    bool withGit = false;
    string author = "";
    string description = "";
    string moduleVersion = "0.1.0";

    for (int i = 2; i < args.Length; i++)
    {
      switch (args[i])
      {
        case "-n" when i + 1 < args.Length:
          moduleNameArg = args[++i];
          break;
        case "-o" when i + 1 < args.Length:
          outputDir = args[++i];
          break;
        case "--force":
          force = true;
          break;
        case "--git":
          withGit = true;
          break;
        case "-Author" when i + 1 < args.Length:
          author = args[++i];
          break;
        case "-Description" when i + 1 < args.Length:
          description = args[++i];
          break;
        case "-ModuleVersion" when i + 1 < args.Length:
          moduleVersion = args[++i];
          break;
      }
    }

    if (string.IsNullOrWhiteSpace(moduleNameArg))
    {
      Console.WriteLine("Error: 모듈 이름이 필요해. -n <ModuleName> 으로 지정해줘.");
      Console.WriteLine("예) pscli new module -n MyTools -o .");
      return;
    }

    string moduleName = ToPascalCase(moduleNameArg);
    string root = Path.Combine(outputDir, moduleName);

    if (Directory.Exists(root) && !force)
    {
      Console.WriteLine($"Error: {root} 디렉토리가 이미 존재해. --force 로 덮어써줘.");
      return;
    }

    Console.WriteLine("════════════════════════════════════════");
    Console.WriteLine($" pscli new module: {moduleName}");
    Console.WriteLine($" 위치: {Path.GetFullPath(root)}");
    Console.WriteLine("════════════════════════════════════════");

    // --- 디렉토리 구조 ---
    string publicDir = Path.Combine(root, "Public");
    string privateDir = Path.Combine(root, "Private");
    string testsDir = Path.Combine(root, "Tests");

    Directory.CreateDirectory(publicDir);
    Directory.CreateDirectory(privateDir);
    Directory.CreateDirectory(testsDir);

    // --- .psm1 (Public/Private 자동 로더) ---
    File.WriteAllText(
      Path.Combine(root, $"{moduleName}.psm1"),
      ModuleTemplates.Psm1Loader());

    // --- .psd1 (매니페스트) ---
    File.WriteAllText(
      Path.Combine(root, $"{moduleName}.psd1"),
      ModuleTemplates.Psd1Manifest(moduleName, moduleVersion, author, description));

    // --- 샘플 Public 함수 하나 (빈 모듈보단 예시가 있는 게 UX 좋음) ---
    string sampleFuncName = $"Get-{moduleName}Info";
    File.WriteAllText(
      Path.Combine(publicDir, $"{sampleFuncName}.ps1"),
      FunctionTemplates.Render("cmdlet", sampleFuncName));

    // --- 샘플 Pester 테스트 ---
    File.WriteAllText(
      Path.Combine(testsDir, $"{moduleName}.Tests.ps1"),
      ModuleTemplates.PesterStub(moduleName, sampleFuncName));

    // --- .gitignore ---
    if (withGit)
    {
      File.WriteAllText(Path.Combine(root, ".gitignore"), ModuleTemplates.GitIgnore());
      TryRunGitInit(root);
    }

    // --- README ---
    File.WriteAllText(
      Path.Combine(root, "README.md"),
      ModuleTemplates.ReadmeMd(moduleName, description));

    Console.WriteLine("생성 완료!");
    Console.WriteLine();
    Console.WriteLine("구조:");
    Console.WriteLine($"  {moduleName}/");
    Console.WriteLine($"  ├── {moduleName}.psd1");
    Console.WriteLine($"  ├── {moduleName}.psm1");
    Console.WriteLine($"  ├── Public/{sampleFuncName}.ps1");
    Console.WriteLine($"  ├── Private/");
    Console.WriteLine($"  └── Tests/{moduleName}.Tests.ps1");
    Console.WriteLine();
    Console.WriteLine("다음 단계:");
    Console.WriteLine($"  cd {root}");
    Console.WriteLine($"  Import-Module ./{moduleName}.psd1");
    Console.WriteLine($"  {sampleFuncName}");
  }

  private static void TryRunGitInit(string root)
  {
    try
    {
      var psi = new System.Diagnostics.ProcessStartInfo("git", "init")
      {
        WorkingDirectory = root,
        UseShellExecute = false,
        CreateNoWindow = true,
      };
      using var proc = System.Diagnostics.Process.Start(psi);
      proc?.WaitForExit();
      Console.WriteLine("    ✅ git 초기화 완료");
    }
    catch
    {
      Console.WriteLine("    ⚠️ git 초기화 실패 (git이 PATH에 없을 수 있어)");
    }
  }

  private static string ToPascalCase(string input)
  {
    var parts = input.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return input;

    var sb = new StringBuilder();
    foreach (var part in parts)
    {
      if (part.Length == 0) continue;
      sb.Append(char.ToUpperInvariant(part[0]));
      if (part.Length > 1) sb.Append(part[1..]);
    }
    return sb.ToString();
  }
}
