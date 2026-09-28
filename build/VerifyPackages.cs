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
    [Required] public string Directory { get; set; }
    [Required] public string Version { get; set; }

    public override bool Execute()
    {
        using (var symbols = ZipFile.OpenRead(Path.Combine(Directory, "tweens.gd." + Version + ".snupkg")))
            Require(symbols, "lib/net10.0/tweens.gd.pdb");
        using (var package = ZipFile.OpenRead(Path.Combine(Directory, "tweens.gd." + Version + ".nupkg")))
        {
            foreach (var name in new[] { "LICENSE", "README.md", "THIRD-PARTY-NOTICES.md", "lib/net10.0/tweens.gd.dll", "lib/net10.0/tweens.gd.xml" })
                Require(package, name);
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
                var dependencies = metadata.Descendants(ns + "dependency").ToArray();
                if (dependencies.Length != 1 || (string)dependencies[0].Attribute("id") != "GodotSharp")
                    Log.LogError("The library package must depend only on GodotSharp.");
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
