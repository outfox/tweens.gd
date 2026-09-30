// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2026 Moritz Voss
using System.Reflection;
using System.Runtime.Versioning;

namespace tweens.gd.Tests.Unit;

public class LibraryFrameworkTests
{
#if LIBRARY_NET8
    private const string Expected = ".NETCoreApp,Version=v8.0";
#else
    private const string Expected = ".NETCoreApp,Version=v10.0";
#endif

    [Fact]
    public void SuiteTestsTheSelectedLibraryBuild() =>
        Assert.Equal(Expected, typeof(Easing).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName);
}
