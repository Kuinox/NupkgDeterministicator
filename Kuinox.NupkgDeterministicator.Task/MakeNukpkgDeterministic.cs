using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Kuinox.NupkgDeterministicator.Task
{
    public sealed class MakeNukpkgDeterministic : ToolTask
    {
        /// <summary>
        /// The zip to make deterministic.
        /// </summary>
        public string ZipPath { get; set; }

        /// <summary>
        /// The last write time to sets. Default to 2000-01-01.
        /// </summary>
        public DateTime LastWriteTime { get; set; } = new DateTime(2000, 1, 1);

        /// <summary>
        /// Used to identify the nupkg and symbols package
        /// </summary>
        public string PackageVersion { get; set; }

        protected override string ToolName => Path.GetFileName(GetDotNetPath());

        protected override string GenerateCommandLineCommands() => $"exec \"{ToolPath}\"";

        protected override string GenerateResponseFileCommands() =>
            ZipPath + " " + LastWriteTime.ToString(CultureInfo.InvariantCulture);

        
        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        // https://github.com/dotnet/roslyn/blob/020db28fa9b744146e6f072dbdc6bf3e62c901c1/src/Compilers/Shared/RuntimeHostInfo.cs#L59
        private static string GetDotNetPath()
        {
            if (Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName) is string pathToDotNet)
            {
                return pathToDotNet;
            }

            var (fileName, sep) = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? ("dotnet.exe", ';')
                : ("dotnet", ':');

            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var item in path.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var filePath = Path.Combine(item, fileName);
                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }
                }
                catch
                {
                    // If we can't read a directory for any reason just skip it
                }
            }

            return fileName;
        }

        protected override string GenerateFullPathToTool() => Path.GetFullPath(GetDotNetPath());
    }
}
