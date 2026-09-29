namespace pscli;

internal static class NewFunctionCommand
{
  public static void Run(string[] args)
  {
    string? functionName = null;
    string templateKey = "cmdlet";
    string outputDir = ".";
    bool force = false;
    bool toStdout = false;

    for (int i = 2; i < args.Length; i++)
    {
      switch (args[i])
      {
        case "-n" when i + 1 < args.Length:
          functionName = args[++i];
          break;
        case "-t" when i + 1 < args.Length:
          templateKey = args[++i].ToLowerInvariant();
          break;
        case "-o" when i + 1 < args.Length:
          outputDir = args[++i];
          break;
        case "--force":
          force = true;
          break;
        case "--stdout":
          toStdout = true;
          break;
      }
    }

    if (string.IsNullOrWhiteSpace(functionName))
    {
      Console.WriteLine("Error: -n <FunctionName> 은 필수야. 예) pscli new function -n Get-Something");
      return;
    }

    if (!FunctionTemplates.Descriptions.ContainsKey(templateKey))
    {
      Console.WriteLine($"Error: 알 수 없는 템플릿 '{templateKey}'. 'pscli list'로 확인해줘.");
      return;
    }

    string content = FunctionTemplates.Render(templateKey, functionName);

    if (toStdout)
    {
      Console.WriteLine(content);
      return;
    }

    if (!string.IsNullOrWhiteSpace(outputDir) && outputDir != ".")
      Directory.CreateDirectory(outputDir);

    string filePath = Path.Combine(outputDir, $"{functionName}.ps1");
    if (!force && File.Exists(filePath))
    {
      Console.WriteLine($"Error: {filePath} 이미 존재해. --force 로 덮어써줘.");
      return;
    }

    File.WriteAllText(filePath, content);
    Console.WriteLine($"Created: {filePath} (Template: {templateKey})");
  }
}
