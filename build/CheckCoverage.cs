// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public sealed class CheckCoverage : Task
{
    [Required] public string Directory { get; set; }
    public double MinimumLine { get; set; }
    public double MinimumBranch { get; set; }
    public string SummaryFile { get; set; }

    private sealed class Line
    {
        public bool Hit;
        public int Covered, Total;
    }

    public override bool Execute()
    {
        // Preserve the existing merge: union of hit lines, best branch count per line across suites.
        var files = new Dictionary<string, Dictionary<int, Line>>();
        foreach (var report in System.IO.Directory.GetFiles(Directory, "coverage.cobertura.xml", SearchOption.AllDirectories))
        foreach (var type in XDocument.Load(report).Descendants("class"))
        {
            var filename = ((string)type.Attribute("filename")).Replace('\\', '/');
            Dictionary<int, Line> lines;
            if (!files.TryGetValue(filename, out lines)) files[filename] = lines = new Dictionary<int, Line>();
            foreach (var element in type.Element("lines").Elements("line"))
            {
                var number = (int)element.Attribute("number");
                Line line;
                if (!lines.TryGetValue(number, out line)) lines[number] = line = new Line();
                line.Hit |= (long)element.Attribute("hits") > 0;
                var branches = Regex.Match((string)element.Attribute("condition-coverage") ?? "", @"\((\d+)/(\d+)\)");
                if (!branches.Success) continue;
                line.Covered = Math.Max(line.Covered, int.Parse(branches.Groups[1].Value));
                line.Total = Math.Max(line.Total, int.Parse(branches.Groups[2].Value));
            }
        }
        if (!files.Any(pair => !pair.Key.EndsWith(".g.cs", StringComparison.Ordinal) && pair.Value.Count > 0))
        {
            Log.LogError("No coverage of library sources was found in " + Directory);
            return false;
        }
        var summary = new List<string> { "| tweens.gd coverage | |", "| --- | --- |" };
        foreach (var generated in new[] { false, true })
        {
            var lines = files.Where(pair => pair.Key.EndsWith(".g.cs", StringComparison.Ordinal) == generated).SelectMany(pair => pair.Value.Values).ToArray();
            var hit = lines.Count(line => line.Hit);
            var covered = lines.Sum(line => line.Covered);
            var total = lines.Sum(line => line.Total);
            var lineRate = lines.Length == 0 ? 100 : 100.0 * hit / lines.Length;
            var branchRate = total == 0 ? 100 : 100.0 * covered / total;
            var name = generated ? "Generated Tweens.* definitions" : "Library sources";
            var text = string.Format("{0}/{1} lines ({2:F2}%), {3}/{4} branches ({5:F2}%)", hit, lines.Length, lineRate, covered, total, branchRate);
            Log.LogMessage(MessageImportance.High, name + ": " + text);
            summary.Add("| " + name + " | " + text + " |");
            if (generated) continue;
            if (lineRate < MinimumLine) Log.LogError("Library line coverage {0:F2}% is below {1}%.", lineRate, MinimumLine);
            if (branchRate < MinimumBranch) Log.LogError("Library branch coverage {0:F2}% is below {1}%.", branchRate, MinimumBranch);
        }
        foreach (var file in files.Where(pair => !pair.Key.EndsWith(".g.cs", StringComparison.Ordinal)).OrderBy(pair => pair.Key))
        {
            var lines = file.Value.Values;
            if (lines.Any(line => !line.Hit || line.Covered < line.Total))
                Log.LogMessage(MessageImportance.High, "  {0}: {1}/{2} lines, {3}/{4} branches", file.Key, lines.Count(line => line.Hit), lines.Count, lines.Sum(line => line.Covered), lines.Sum(line => line.Total));
        }
        if (!string.IsNullOrEmpty(SummaryFile)) File.AppendAllLines(SummaryFile, summary);
        return !Log.HasLoggedErrors;
    }
}
