using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace LibHacBuild;

public class Build
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("You must specify the nupkg path.\n" + Help);
            return 1;
        }
        if (args.Length > 2)
        {
            Console.Error.WriteLine("Too many arguments provided.\n" + Help);
        }

        if (!File.Exists(args[0]))
        {
            Console.Error.WriteLine($"File {args[0]} doesn't exist");
        }

        var dateTime = args.Length > 1 ? DateTime.Parse(args[1]) : new DateTime(2000, 1, 1);

        RepackNugetPackage(args[0], dateTime);
        return 0;
    }

    const string Help =
@"Usage: nupkg-deterministicator [path-to-nupkg]
Try to make a NuGet package (.nupkg) deterministic.
It will try to produce a bit to bit identical .nupkg as long as the packed content is the same.
";

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

            IEnumerable<string> files = Directory.EnumerateFiles(tempDir, "*.json", SearchOption.AllDirectories)
                .Concat(Directory.EnumerateFiles(tempDir, "*.xml", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(tempDir, "*.rels", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(tempDir, "*.psmdcp", SearchOption.AllDirectories))
                .Concat(Directory.EnumerateFiles(tempDir, "*.nuspec", SearchOption.AllDirectories));

            foreach (string filename in files)
            {
                ReplaceLineEndings(filename);
            }

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
        using (var sha = SHA256.Create())
        {
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
    }

    public static string RenamePsmdcp(string packageDir, string name)
    {
        string fileName = Directory.EnumerateFiles(packageDir, "*.psmdcp", SearchOption.AllDirectories).Single();
        string newFileName = Path.Combine(Path.GetDirectoryName(fileName)!, name + ".psmdcp");
        Directory.Move(fileName, newFileName);

        return Path.GetRelativePath(packageDir, newFileName).Replace('\\', '/');
    }

    public static void EditManifestRelationships(string path, string psmdcpPath)
    {
        XDocument doc = XDocument.Load(path);
        XNamespace ns = doc.Root!.GetDefaultNamespace();

        foreach (XElement rs in doc.Root.Elements(ns + "Relationship"))
        {
            using (var sha = SHA256.Create())
            {
                if (rs.Attribute("Target")!.Value.Contains(".psmdcp"))
                {
                    rs.Attribute("Target")!.Value = "/" + psmdcpPath;
                }

                string s = "/" + psmdcpPath + rs.Attribute("Target")!.Value;
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                string id = "R" + ToHexString(hash).Substring(0, 16);
                rs.Attribute("Id")!.Value = id;
            }
        }

        doc.Save(path);
    }

    public static string ToHexString(byte[] arr)
    {
        return BitConverter.ToString(arr).ToLower().Replace("-", "");
    }

    public static void ZipDirectory(string outFile, string directory, IEnumerable<string> files, DateTime dateTime)
    {
        using var s = new ZipArchive(File.Create(outFile), ZipArchiveMode.Create);
        foreach (string filePath in files)
        {
            string absolutePath = Path.Combine(directory, filePath);
            var entry = s.CreateEntry(filePath);
            entry.LastWriteTime = dateTime;
            using var fs = File.OpenRead(absolutePath);
            using var entryS = entry.Open();
            fs.CopyTo(entryS);
        }
    }
    public static void ReplaceLineEndings(string filename)
        => File.WriteAllText(filename, File.ReadAllText(filename).ReplaceLineEndings("\r\n"));
}