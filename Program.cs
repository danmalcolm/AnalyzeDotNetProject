using System;
using System.Linq;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    class Program
    {
        private static string[] IgnoredPrefixes = new string[] {""};

        static void Main(string[] args)
        {
            if (ShowHelp(args)) return;

            string projectPath = args[0];
            IgnoredPrefixes = args.Length > 1 ? args[1].Split(',', StringSplitOptions.RemoveEmptyEntries) : new string[] { };
            
            var dependencyGraphService = new DependencyGraphService();
            var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(projectPath);

            foreach(var project in dependencyGraph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
            {
                Console.WriteLine(project.Name);

                // Generate lock file
                var lockFileService = new LockFileService();
                var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);
                if (lockFile != null)
                {
                    foreach (var targetFramework in project.TargetFrameworks)
                    {
                        Console.WriteLine($"  [{targetFramework.FrameworkName}]");

                        var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t =>
                            t.TargetFramework.Equals(targetFramework.FrameworkName));
                        if (lockFileTargetFramework != null)
                        {
                            foreach (var dependency in targetFramework.Dependencies.Where(
                                x => IncludeDependency(x.Name)))
                            {
                                var projectLibrary =
                                    lockFileTargetFramework.Libraries.FirstOrDefault(library =>
                                        library.Name == dependency.Name);

                                ReportDependency(projectLibrary, lockFileTargetFramework, 1);
                            }
                        }
                    }
                }
            }
        }

        private static bool ShowHelp(string[] args)
        {
            if (args.Length == 0 || args[0] == "/?" || args[0] == "--help" || args.Length > 2)
            {
                Console.WriteLine(@"
Usage:

analyzedotnetproject.exe <projectpath> <ignores>

Example:

analyzedotnetproject.exe ""C:\MyRepos\MyProject\src\MyProject.Api.csproj"" ""System.,Microsoft.""

Further details:

If NuGet repository authentication is required, run ""dotnet restore --interactive"" in the project / solution directory before running this tool.

Note that existing project.assets.json files will be used if present in a project's output directory, so ensure that NuGet packages are restored (via build / restore) following changes to dependencies. 
");
                return true;
            }

            return false;
        }

        private static bool IncludeDependency(string name)
        {
            bool ignore = IgnoredPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            return !ignore;
        }

        private static void ReportDependency(LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, int indentLevel)
        {
            Console.Write(new String(' ', indentLevel * 2));
            Console.WriteLine($"{projectLibrary.Name}, v{projectLibrary.Version}");

            foreach (var childDependency in projectLibrary.Dependencies.Where(x => IncludeDependency(x.Id)))
            {
                var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => string.Equals(library.Name, childDependency.Id, StringComparison.OrdinalIgnoreCase));

                ReportDependency(childLibrary, lockFileTargetFramework, indentLevel + 1);
            }
        }
    }
}
