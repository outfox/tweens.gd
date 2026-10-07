using System.Reflection;
using System.Runtime.Versioning;
using tutorial;

public sealed class StandaloneTests
{
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
