using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    class Program
    {
        private const int IndentWidth = 2;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(Run)
                .WithNotParsed(HandleArgsError);
        }

        private static void Run(CommandLineOptions options)
        {
            foreach (var entryPointProject in options.Projects)
            {
                string projectName = GetProjectNameFromPath(entryPointProject);
                Console.WriteLine($"{GetIndent(options, 0)}{projectName}");

                var dependencyGraphService = new DependencyGraphService();
                var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(entryPointProject);
                

                foreach (var project in dependencyGraph.Projects.Where(p =>
                    IncludeProject(options, p.Name)
                    && p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
                {
                    Console.WriteLine($"{GetIndent(options, 1)}{project.Name}");

                    if (options.Level == DependencyLevel.Package)
                    {
                        DisplayPackages(options, project, 2);
                    }
                }
            }
        }

        private static string GetProjectNameFromPath(string projectPath)
        {
            return Path.GetFileNameWithoutExtension(projectPath);
        }

        private static void DisplayPackages(CommandLineOptions options, PackageSpec project, int indentLevel)
        {
            // Generate lock file
            var lockFileService = new LockFileService();
            var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);
            if (lockFile != null)
            {
                foreach (var targetFramework in project.TargetFrameworks)
                {
                    string indent = GetIndent(options, indentLevel);
                    Console.WriteLine($"{indent}[{targetFramework.FrameworkName}]");

                    var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t =>
                        t.TargetFramework.Equals(targetFramework.FrameworkName));
                    if (lockFileTargetFramework != null)
                    {
                        foreach (var dependency in targetFramework.Dependencies.Where(
                            x => IncludePackage(options, x.Name)))
                        {
                            var projectLibrary =
                                lockFileTargetFramework.Libraries.FirstOrDefault(library =>
                                    library.Name == dependency.Name);

                            ReportDependency(options, projectLibrary, lockFileTargetFramework, indentLevel + 1);
                        }
                    }
                }
            }
        }

        private static string GetIndent(CommandLineOptions options, int level)
        {
            return options.Format == OutputFormat.Nested
                ? "".PadLeft(level * IndentWidth, ' ')
                : "";
        }

        static void HandleArgsError(IEnumerable<Error> errors)
        {
            var result = -2;
            Console.WriteLine("errors {0}", errors.Count());
            if (errors.Any(x => x is HelpRequestedError || x is VersionRequestedError))
                result = -1;
            Console.WriteLine("Exit code {0}", result);
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

        private static bool IncludeProject(CommandLineOptions options, string name)
            => Include(name, options.ProjectIncludeFilter, options.ProjectExcludeFilter);


        private static bool IncludePackage(CommandLineOptions options, string name) 
            => Include(name, options.PackageIncludeFilter, options.PackageExcludeFilter);

        private static bool Include(string name, IEnumerable<string> includeFilter, IEnumerable<string> excludeFilter)
        {
            bool included = !includeFilter.Any() ||
                               includeFilter.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!included)
            {
                return false;
            }

            bool excluded = excludeFilter.Any() &&
                            excludeFilter.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (excluded)
            {
                return false;
            }
            return true;
        }

        private static void ReportDependency(CommandLineOptions options,
            LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, int indentLevel)
        {
            Console.Write(new String(' ', indentLevel * 2));
            Console.WriteLine($"{projectLibrary.Name}, v{projectLibrary.Version}");

            foreach (var childDependency in projectLibrary.Dependencies.Where(x => IncludePackage(options, x.Id)))
            {
                var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => string.Equals(library.Name, childDependency.Id, StringComparison.OrdinalIgnoreCase));

                ReportDependency(options, childLibrary, lockFileTargetFramework, indentLevel + 1);
            }
        }
    }
}
