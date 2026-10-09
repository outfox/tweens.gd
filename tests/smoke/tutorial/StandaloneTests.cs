using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Godot;
using tutorial;

[Collection<tutorial.Tests.TutorialCollection>]
public sealed class StandaloneTests
{
    [Fact]
    public void ExportPresetsCoverBundledPlatformsAndEnableWebExtensions()
    {
        using var presets = new ConfigFile();
        Assert.Equal(Error.Ok, presets.Load("res://export_presets.cfg"));
        using var extension = new ConfigFile();
        Assert.Equal(Error.Ok, extension.Load("res://addons/tweens_gd/tweens_gd.gdextension"));

        var platforms = presets.GetSections().Where(section => Regex.IsMatch(section, @"^preset\.\d+$"))
            .Select(section => presets.GetValue(section, "platform").AsString().ToLowerInvariant())
            .Select(platform => platform == "windows desktop" ? "windows" : platform).ToHashSet();
        Assert.Subset(platforms, extension.GetSectionKeys("libraries")
            .Select(key => key.Split('.')[0]).ToHashSet());

        var web = Assert.Single(presets.GetSections(), section => Regex.IsMatch(section, @"^preset\.\d+$")
            && presets.GetValue(section, "name").AsString() == "Web");
        Assert.True(presets.GetValue(web + ".options", "variant/extensions_support").AsBool());
        Assert.False(presets.GetValue(web + ".options", "variant/thread_support").AsBool());
    }

    [Fact]
    public void TutorialAndAddonCompileTogetherForStockGodot()
    {
        var assembly = typeof(Sources).Assembly;
        Assert.Same(assembly, typeof(tweens.gd.TweenInstance).Assembly);
        Assert.Equal(".NETCoreApp,Version=v8.0", assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference => reference.Name!.StartsWith("2dog"));
        Assert.Contains("Tweens.play", Sources.Read("Install/hello_tweens.gd"));
    }
}
