using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Kuinox.NupkgDeterministicator;

public class Build
{
    // Adapted from https://github.com/Thealexbarney/LibHac/blob/master/build/Build.cs
    // Removed all dependencies.
    // Distributed as a dotnet tool.

    public static int Main(string[] args)
    {
        var argPathToNupkg = new Argument<string>(
                "path-to-nupkg",
                "The full or relative path of a .nupkg, .symbols.nupkg, or .snupkg file."
        )
        {
            Arity = new(1, 1)
        };

        var argDateTime = new Argument<DateTime>(
            name: "optional-date",
            getDefaultValue: () => DefaultDateTime,
            description: $"The optional {nameof(DateTime)} to assign to the Modified Date of every file in a given nupkg.\nDefault: {DefaultDateTime:yyyy-MM-ddTHH:mm:ss.fffzzz}"
        )
        {
            Arity = new(0, 1)
        };

        var rootCommand = new RootCommand(
@"Try to make a NuGet package (.nupkg) deterministic.
It will try to produce a bit to bit identical .nupkg as long as the packed content is the same.
");
        rootCommand.AddArgument(argPathToNupkg);
        rootCommand.AddArgument(argDateTime);

        rootCommand.SetHandler(
            (pathToNupkg, dateTime) =>
            {
                if (!File.Exists(pathToNupkg))
                    Console.Error.WriteLine($"File {args[0]} doesn't exist");
                RepackNugetPackage(pathToNupkg, dateTime);
            },
            argPathToNupkg,
            argDateTime
        );

        return rootCommand.Invoke(args);
    }

    static readonly DateTime DefaultDateTime = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void RepackNugetPackage(string path, DateTime dateTime)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var libDir = Path.Combine(tempDir, "lib");
        var relsFile = Path.Combine(tempDir, "_rels", ".rels");

        try
        {
            Directory.CreateDirectory(tempDir);

            List<string> fileList = UnzipPackage(path, tempDir);

            string newPsmdcpName = CalcPsmdcpName(libDir);
            string newPsmdcpPath = RenamePsmdcp(tempDir, newPsmdcpName);
            EditManifestRelationships(relsFile, newPsmdcpPath);
            int index = fileList.FindIndex(x => x.Contains(".psmdcp"));
            fileList[index] = newPsmdcpPath;

            ZipDirectory(path, tempDir, fileList, dateTime);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    public static List<string> UnzipPackage(string package, string dest)
    {
        ZipFile.ExtractToDirectory(package, dest);
        var fileList = new List<string>();
        using (var s = ZipFile.OpenRead(package))
        {
            foreach (var entry in s.Entries)
            {
                fileList.Add(entry.FullName);
            }
        }
        return fileList;
    }

    public static string CalcPsmdcpName(string libDir)
    {
        using var sha = SHA256.Create();
        if (Directory.Exists(libDir))
        {
            foreach (string file in Directory.EnumerateFiles(libDir, "*", SearchOption.AllDirectories))
            {
                byte[] data = File.ReadAllBytes(file);
                sha.TransformBlock(data, 0, data.Length, data, 0);
            }
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return ToHexString(sha.Hash!).ToLower().Substring(0, 32);
    }

    public static string RenamePsmdcp(string packageDir, string name)
    {
        string fileName = Directory.EnumerateFiles(packageDir, "*.psmdcp", SearchOption.AllDirectories).Single();
        string newFileName = Path.Combine(Path.GetDirectoryName(fileName)!, name + ".psmdcp");

        if (fileName != newFileName)
            Directory.Move(fileName, newFileName);

        return Path.GetRelativePath(packageDir, newFileName).Replace('\\', '/');
    }

    public static void EditManifestRelationships(string path, string psmdcpPath)
    {
        XDocument doc = XDocument.Load(path);
        XNamespace ns = doc.Root!.GetDefaultNamespace();

        foreach (XElement rs in doc.Root.Elements(ns + "Relationship"))
        {
            using var sha = SHA256.Create();
            if (rs.Attribute("Target")!.Value.Contains(".psmdcp"))
            {
                rs.Attribute("Target")!.Value = "/" + psmdcpPath;
            }

            string s = "/" + psmdcpPath + rs.Attribute("Target")!.Value;
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            string id = string.Concat("R", ToHexString(hash).AsSpan(0, 16));
            rs.Attribute("Id")!.Value = id;
        }

        doc.Save(path);
    }

    public static string ToHexString(byte[] arr) => BitConverter.ToString(arr).ToLower().Replace("-", "");

    public static void ZipDirectory(string outFile, string directory, IEnumerable<string> files, DateTime dateTime)
    {
        using var s = new ZipArchive(File.Create(outFile), ZipArchiveMode.Create);
        foreach (string filePath in files)
        {
            var entry = s.CreateEntry(filePath);
            entry.LastWriteTime = dateTime;
            using var fs = File.OpenRead(Path.Combine(directory, filePath));
            using var entryS = entry.Open();
            fs.CopyTo(entryS);
        }
    }
}