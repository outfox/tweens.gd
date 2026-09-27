# SPDX-License-Identifier: MIT
# SPDX-FileCopyrightText: 2026 Moritz Voss
param(
    [Parameter(Mandatory)][string] $Version,
    [string] $PackageDirectory = 'artifacts/packages',
    [string] $Godot,
    [string] $GodotDotNet
)
$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$package = Join-Path ([IO.Path]::GetFullPath($PackageDirectory, $repository)) "tweens.gd-$Version.zip"
$consumer = Join-Path $repository "artifacts/addon-consumer/$([Guid]::NewGuid())"
New-Item -ItemType Directory -Path $consumer -Force | Out-Null
[IO.Compression.ZipFile]::ExtractToDirectory($package, $consumer)
# Deliberately use a normal Godot project, without tweens.gd references/analyzers,
# implicit imports or nullable enabled. This catches settings leaking from the library build.
@'
<Project Sdk="Godot.NET.Sdk/4.7.2">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
'@ | Set-Content (Join-Path $consumer 'Consumer.csproj')
@'
config_version=5
[application]
config/name="Addon consumer"
[dotnet]
project/assembly_name="Consumer"
'@ | Set-Content (Join-Path $consumer 'project.godot')
@'
using Godot;
using tweens.gd;
public partial class Consumer : Node2D
{
    private int frames;
    public override void _Process(double delta)
    {
        if (++frames > 300) GetTree().Quit(1);
    }
    public override async void _Ready()
    {
        try
        {
            await Animate();
            if (Position != new Vector2(100, 50) || !Mathf.IsEqualApprox(Rotation, 1))
                throw new System.Exception("Unexpected final tween values.");
            GD.Print("C# addon smoke passed.");
            GetTree().Quit();
        }
        catch (System.Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    public async System.Threading.Tasks.Task Animate()
    {
        var move = new Tweens.Position2D(new Vector2(100, 50), 0.25);
        await this.Tween(move with { Delay = 0.1 });
        var custom = new Tweens.Property<Node2D, float>(
            node => node.Rotation, (node, value) => node.Rotation = value,
            Interpolators.Float) { To = 1, Duration = 0.1 };
        await this.Tween(custom);
    }
}
'@ | Set-Content (Join-Path $consumer 'Consumer.cs')
$scene = @'
[gd_scene load_steps=2 format=3]
[ext_resource type="Script" path="res://Consumer.cs" id="1"]
[node name="Consumer" type="Node2D"]
script = ExtResource("1")
'@
@'
extends SceneTree
var frames := 0
func _initialize() -> void:
    _run.call_deferred()
func _process(_delta: float) -> bool:
    frames += 1
    if frames > 300:
        quit(1)
    return false
func _run() -> void:
    var target := Node2D.new()
    root.add_child(target)
    var handle = Tweens.play(target, Tweens.position_2d(Vector2(100, 50), 0.05))
    await handle.end
    if target.position != Vector2(100, 50) or handle.completion_reason != Tweens.Reason.COMPLETED:
        quit(1)
        return
    print("GDScript addon smoke passed.")
    target.queue_free()
    quit()
'@ | Set-Content (Join-Path $consumer 'smoke.gd')
foreach ($configuration in 'Debug', 'Release') {
    dotnet build (Join-Path $consumer 'Consumer.csproj') -c $configuration
    if ($LASTEXITCODE -ne 0) { throw "Addon consumer failed to build ($configuration)." }
}
foreach ($engine in @(
    @{ Path = $Godot; Arguments = @('--script', 'res://smoke.gd'); Marker = 'GDScript addon smoke passed.' },
    @{ Path = $GodotDotNet; Arguments = @('res://main.tscn'); Marker = 'C# addon smoke passed.' }
)) {
    if (!$engine.Path) { continue }
    if ($engine.Path -eq $GodotDotNet) { $scene | Set-Content (Join-Path $consumer 'main.tscn') }
    # Match installation in the editor: populate the global script-class cache first.
    $importLog = & $engine.Path --headless --editor --path $consumer --import 2>&1
    $importExitCode = $LASTEXITCODE
    if ($importExitCode -ne 0 -or ($importLog | Select-String 'SCRIPT ERROR:|ERROR:')) {
        $importLog | Write-Output
        throw "Addon import failed (exit $importExitCode): $($engine.Path)"
    }
    $engineArgs = @('--headless', '--path', $consumer, '--fixed-fps', '60', '--quit-after', '600') + $engine.Arguments
    $log = & $engine.Path @engineArgs 2>&1
    $exitCode = $LASTEXITCODE
    $log | Write-Output
    if ($exitCode -ne 0 -or !($log | Select-String -SimpleMatch $engine.Marker) -or ($log | Select-String 'SCRIPT ERROR:|ERROR:')) {
        throw "Addon engine smoke test failed: $($engine.Path)"
    }
}
Write-Output "Verified addon source installation in $consumer"
