// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class VerifyPackages : Task
{
    private static readonly string[] Frameworks = { "net8.0", "net10.0" };

    [Required] public string Directory { get; set; }
    [Required] public string Version { get; set; }

    public override bool Execute()
    {
        using (var symbols = ZipFile.OpenRead(Path.Combine(Directory, "tweens.gd." + Version + ".snupkg")))
            foreach (var framework in Frameworks)
                Require(symbols, "lib/" + framework + "/tweens.gd.pdb");
        using (var package = ZipFile.OpenRead(Path.Combine(Directory, "tweens.gd." + Version + ".nupkg")))
        {
            foreach (var name in new[] { "LICENSE", "README.md", "THIRD-PARTY-NOTICES.md" })
                Require(package, name);
            foreach (var framework in Frameworks)
            {
                Require(package, "lib/" + framework + "/tweens.gd.dll");
                Require(package, "lib/" + framework + "/tweens.gd.xml");
            }
            using (var stream = Require(package, "tweens.gd.nuspec").Open())
            {
                var document = XDocument.Load(stream);
                var ns = document.Root.Name.Namespace;
                var metadata = document.Root.Element(ns + "metadata");
                if ((string)metadata.Element(ns + "id") != "tweens.gd" || (string)metadata.Element(ns + "version") != Version)
                    Log.LogError("Package ID or version is incorrect.");
                var license = metadata.Element(ns + "license");
                if ((string)license != "MIT" || (string)license.Attribute("type") != "expression")
                    Log.LogError("Package must declare the MIT license expression.");
                // NuGet writes one dependency group per target framework.
                var groups = metadata.Descendants(ns + "group").ToArray();
                var frameworks = groups.Select(group => (string)group.Attribute("targetFramework")).OrderBy(name => name);
                if (!frameworks.SequenceEqual(Frameworks.OrderBy(name => name)))
                    Log.LogError("Package must target exactly " + string.Join(" and ", Frameworks) + ".");
                if (!groups.All(group => group.Elements(ns + "dependency")
                        .Select(dependency => (string)dependency.Attribute("id")).SequenceEqual(new[] { "GodotSharp" })))
                    Log.LogError("Each target framework must depend only on GodotSharp.");
            }
        }
        return !Log.HasLoggedErrors;
    }

    private static ZipArchiveEntry Require(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        if (entry == null || entry.Length == 0) throw new InvalidDataException("Package is missing or has empty " + name);
        return entry;
    }
}
