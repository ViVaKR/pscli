using System.Reflection;
using pscli;

// ---------------
// --version / -v
// ---------------
if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
  var version = Assembly.GetExecutingAssembly()
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                        .InformationalVersion ?? "0.1.0";
  Console.WriteLine($"pscli version {version}");
  return;
}

// ---------------------------------------------------------------------
// list — 템플릿 목록
// ---------------------------------------------------------------------
if (args.Length > 0 && args[0] == "list")
{
  Console.WriteLine("사용 가능한 함수 템플릿:");
  foreach (var (key, desc) in FunctionTemplates.Descriptions)
  {
    Console.WriteLine($"  {key,-10} {desc}");
  }
  Console.WriteLine();
  Console.WriteLine("사용 예:");
  Console.WriteLine("  pscli new function -n Get-Something -t cmdlet -o .");
  Console.WriteLine("  pscli new module -n MyTools -o .");
  return;
}

// ---------------------------------------------------------------------
// new function | new module
// ---------------------------------------------------------------------
if (args.Length >= 2 && args[0] == "new")
{
  switch (args[1])
  {
    case "function":
      NewFunctionCommand.Run(args);
      return;
    case "module":
      ModuleScaffolder.Run(args);
      return;
    default:
      Console.WriteLine($"Error: 알 수 없는 서브커맨드 '{args[1]}'. 'function' 또는 'module'을 써줘.");
      return;
  }
}

PrintUsage();
return;

// =========================================================================
static void PrintUsage()
{
  Console.WriteLine("""
    Usage:
      pscli new function -n <Name> [-t basic|cmdlet|pipeline] [-o <dir>] [--force] [--stdout]
      pscli new module -n <Name> [-o <dir>] [--git] [--force]
      pscli list
      pscli --version

    예시:
      pscli new function -n Get-Something -t cmdlet -o ./Public
      pscli new module -n MyTools -o . --git
    """);
}
