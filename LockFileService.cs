using System;
using System.IO;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    public class LockFileService
    {
        public LockFile GetLockFile(string projectPath, string outputPath)
        {
            string lockFilePath = Path.Combine(outputPath, "project.assets.json");
            if (!File.Exists(lockFilePath))
            {
                // Run the restore command
                var dotNetRunner = new DotNetRunner();
                string[] arguments = new[] { "restore", $"\"{projectPath}\"" };
                var runStatus = dotNetRunner.Run(Path.GetDirectoryName(projectPath), arguments);
                if (!runStatus.IsSuccess)
                {
                    throw new Exception(
                        $"Unable to get project.assets.json for project {projectPath}. If NuGet repository authentication is required, run \"dotnet restore --interactive\" in the project / solution directory before running this tool. Output: {runStatus.Output} Error: {runStatus.Errors}");
                }
            }
            // Load the lock file
            return LockFileUtilities.GetLockFile(lockFilePath, NuGet.Common.NullLogger.Instance);
        }
    }
}