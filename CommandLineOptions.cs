using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace AnalyzeDotNetProject
{
    public class CommandLineOptions
    {
        [Option(Required = true, HelpText = "Paths to entry-point project files")]
        public IEnumerable<string> Projects { get; set; }

        [Option(HelpText = "Optional list of filters to specify depended-on projects to include (starts-with test)")]
        public IEnumerable<string> ProjectIncludeFilter { get; set; }

        [Option(HelpText = "Optional list of filters to specify depended-on projects to exclude (starts-with test)")]
        public IEnumerable<string> ProjectExcludeFilter { get; set; }

        [Option(HelpText = "Optional list of filters to specify packages to include (starts-with test)")]
        public IEnumerable<string> PackageIncludeFilter { get; set; }

        [Option(HelpText = "Optional list of filters to specified packages to exclude (starts-with test)")]
        public IEnumerable<string> PackageExcludeFilter { get; set; }

        [Option(HelpText = "Controls whether to report at project or package level")]
        public DependencyLevel Level { get; set; } = DependencyLevel.Package;

        [Option(HelpText = "Controls output format")]
        public OutputFormat Format { get; set; } = OutputFormat.Nested;

        [Usage(ApplicationAlias = "analysedotnetproject.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                    new Example("Outputs list of project and package dependencies", new CommandLineOptions { Projects = new [] { "MyProject/MyProject.csproj"} }),
                    new Example("Outputs list of project and package dependencies", new CommandLineOptions { Projects = new [] { "MyProject/MyProject1.csproj", "MyProject/MyProject2.csproj" } }),
                    new Example("Outputs list of project and package dependencies with excluded projects", new CommandLineOptions { Projects = new [] { "MyProject/MyProject1.csproj", "MyProject/MyProject2.csproj" }, ProjectExcludeFilter = new [] { "AnotherProject."}}),
                    new Example("Outputs list of project and package dependencies with excluded packages", new CommandLineOptions { Projects = new [] { "MyProject/MyProject1.csproj" }, PackageExcludeFilter = new [] { "Ignore1.", "Ignore2."}}),
                };
            }
        }
    }
}