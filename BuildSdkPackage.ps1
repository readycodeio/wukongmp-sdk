#!powershell.exe -ExecutionPolicy Bypass -File

<#
    Builds the ReadyM.SDK.Wukong NuGet package.

    The package carries both the WukongMP SDK and the ReadyM core SDK it builds on. They
    ship together on purpose: the core SDK is not released on its own and has no version of
    its own, so a mod can never end up pairing a Wukong version with a core version that
    never shipped with it.

    Third-party assemblies are not bundled. They are declared as package dependencies, and
    that list is generated from the projects that go into each lib folder rather than
    maintained by hand.

      -SyncDependencies    regenerate the dependency list in the packaging project
      -CheckDependencies   fail if the list has drifted, without changing anything
#>

param(
    [string] $Configuration = 'Release',
    [string] $Output = (Join-Path $PSScriptRoot '..\local-nuget-feed'),
    [switch] $SyncDependencies,
    [switch] $CheckDependencies
)

$ErrorActionPreference = 'Stop'

$packaging = Join-Path $PSScriptRoot 'Packaging\ReadyM.SDK.Wukong.csproj'
$coreSdk = Join-Path $PSScriptRoot 'readym-core-sdk\src'

# What lands in each lib folder. Keep in step with the ProjectReferences in the packaging
# project: these are the same assemblies, listed here so their dependencies can be read.
$buckets = [ordered]@{
    'netstandard2.0' = @(
        "$PSScriptRoot\WukongMp.Api\WukongMp.Api.csproj"
        "$PSScriptRoot\WukongMp.Sdk\WukongMp.Sdk.csproj"
        "$PSScriptRoot\ReadyM.Wukong.Common\ReadyM.Wukong.Common.csproj"
        "$coreSdk\ReadyM.Api\ReadyM.Api.csproj"
        "$coreSdk\ReadyM.Api.Multiplayer\ReadyM.Api.Multiplayer.csproj"
        "$coreSdk\ReadyM.Relay.Client\ReadyM.Relay.Client.csproj"
        "$coreSdk\YooniCSharp\Native\Container\Yooni.Native.Container.csproj"
        "$coreSdk\YooniCSharp\Native\LowLevel\Yooni.Native.LowLevel.csproj"
        "$coreSdk\YooniCSharp\Native\Serialization\Yooni.Native.Serialization.csproj"
        "$coreSdk\Friflo.Engine.ECS\src\ECS\Friflo.Engine.ECS.csproj"
        "$coreSdk\Friflo.Engine.ECS\src\ECS.Boost\Friflo.Engine.ECS.Boost.csproj"
        "$coreSdk\LiteNetLib\LiteNetLib\LiteNetLib.csproj"
    )
    'net10.0' = @(
        "$PSScriptRoot\WukongMp.Sdk.Serverside\WukongMp.Sdk.Serverside.csproj"
        "$PSScriptRoot\ReadyM.Wukong.Common\ReadyM.Wukong.Common.csproj"
        "$coreSdk\ReadyM.Api\ReadyM.Api.csproj"
        "$coreSdk\ReadyM.Api.Multiplayer\ReadyM.Api.Multiplayer.csproj"
        "$coreSdk\ReadyM.Relay.Server.Sdk\ReadyM.Relay.Server.Sdk.csproj"
        "$coreSdk\YooniCSharp\Native\Container\Yooni.Native.Container.csproj"
        "$coreSdk\YooniCSharp\Native\LowLevel\Yooni.Native.LowLevel.csproj"
        "$coreSdk\YooniCSharp\Native\Serialization\Yooni.Native.Serialization.csproj"
        "$coreSdk\Friflo.Engine.ECS\src\ECS\Friflo.Engine.ECS.csproj"
        "$coreSdk\Friflo.Engine.ECS\src\ECS.Boost\Friflo.Engine.ECS.Boost.csproj"
        "$coreSdk\LiteNetLib\LiteNetLib\LiteNetLib.csproj"
    )
}

# The forks multi-target below net10.0, so ask them about the framework they actually build.
$forkTfm = @{ 'net10.0' = 'net8.0' }

function Test-Excluded([string] $id)
{
    if ($id -like 'Microsoft.CodeAnalysis*') { return $true }   # the generator's own, compiler supplied
    if ($id -like 'Microsoft.SourceLink*') { return $true }     # build time only
    if ($id -in @('Nullable', 'PolySharp')) { return $true }    # source only
    if ($id -in @('Friflo.Engine.ECS', 'Friflo.Engine.ECS.Boost', 'LiteNetLib')) { return $true }  # bundled
    # our own assemblies are bundled, except the game reference assemblies
    if ($id -like 'ReadyM.*' -and $id -ne 'ReadyM.Wukong.GameRefs') { return $true }
    return $false
}

function Get-VersionKey([string] $v)
{
    $core = ($v -split '[-+]')[0]
    $parts = @($core -split '\.' | ForEach-Object { [int]($_ -replace '\D', '0') })
    while ($parts.Count -lt 4) { $parts += 0 }
    return [version]::new($parts[0], $parts[1], $parts[2], $parts[3])
}

function Get-BucketDependencies([string] $tfm)
{
    $found = @{}
    foreach ($proj in $buckets[$tfm])
    {
        if (-not (Test-Path $proj))
        {
            Write-Error "missing project: $proj"
        }

        $ask = $tfm
        if ($proj -match 'Friflo\.Engine\.ECS|LiteNetLib' -and $forkTfm.ContainsKey($tfm))
        {
            $ask = $forkTfm[$tfm]
        }

        $raw = & dotnet msbuild $proj -getItem:PackageReference "-p:TargetFramework=$ask" -nologo 2>$null
        if ($LASTEXITCODE -ne 0)
        {
            Write-Error "could not evaluate $proj for $ask"
        }

        foreach ($i in ($raw | ConvertFrom-Json).Items.PackageReference)
        {
            $id = $i.Identity
            $ver = $i.Version
            if (-not $id -or -not $ver) { continue }
            if ($i.PrivateAssets -and $i.PrivateAssets.ToLower() -eq 'all') { continue }
            if (Test-Excluded $id) { continue }
            if (-not $found.ContainsKey($id) -or (Get-VersionKey $ver) -gt (Get-VersionKey $found[$id]))
            {
                $found[$id] = $ver
            }
        }
    }
    return $found
}

function Format-DependencyRegion()
{
    $sb = [System.Text.StringBuilder]::new()
    foreach ($tfm in $buckets.Keys)
    {
        $deps = Get-BucketDependencies $tfm
        # Write-Host, not Write-Output: anything written to the pipeline here would end up
        # concatenated into this function's return value and land in the csproj.
        Write-Host "  $tfm : $($deps.Count) dependencies"
        [void]$sb.AppendLine('    <ItemGroup Condition="''$(TargetFramework)'' == ''' + $tfm + '''">')
        foreach ($id in ($deps.Keys | Sort-Object))
        {
            [void]$sb.AppendLine('        <PackageReference Include="' + $id + '" Version="' + $deps[$id] + '" />')
        }
        [void]$sb.AppendLine('    </ItemGroup>')
    }
    return $sb.ToString().TrimEnd([char]13, [char]10)
}

if ($SyncDependencies -or $CheckDependencies)
{
    $begin = '    <!-- BEGIN GENERATED DEPENDENCIES -->'
    $end = '    <!-- END GENERATED DEPENDENCIES -->'
    $text = Get-Content $packaging -Raw
    $bi = $text.IndexOf($begin)
    $ei = $text.IndexOf($end)
    if ($bi -lt 0 -or $ei -lt 0)
    {
        Write-Error 'generated markers not found in the packaging project'
    }

    Write-Output 'Reading dependencies from the bundled projects...'
    $fresh = Format-DependencyRegion
    $current = $text.Substring($bi + $begin.Length, $ei - $bi - $begin.Length).Trim([char]13, [char]10)

    $same = $current.Replace([string][char]13, '') -eq $fresh.Replace([string][char]13, '')
    if ($same)
    {
        Write-Output 'Dependency list is up to date.'
    }
    elseif (-not $SyncDependencies)
    {
        Write-Output ''
        Write-Output 'Dependency list has drifted from the bundled projects. Run with -SyncDependencies.'
        exit 1
    }
    else
    {
        $nl = [string][char]13 + [string][char]10
        $updated = $text.Substring(0, $bi + $begin.Length) + $nl + $fresh + $nl + $text.Substring($ei)
        Set-Content -Path $packaging -Value $updated -NoNewline
        Write-Output 'Dependency list updated.'
    }

    if ($CheckDependencies -and -not $SyncDependencies)
    {
        exit 0
    }
}

$Output = [System.IO.Path]::GetFullPath($Output)
if (-not (Test-Path $Output))
{
    New-Item -ItemType Directory -Force $Output | Out-Null
}

Write-Output ''
Write-Output "Packing ReadyM.SDK.Wukong ($Configuration) into $Output"
& dotnet pack $packaging -c $Configuration -o $Output
if ($LASTEXITCODE -ne 0)
{
    Write-Error 'pack failed'
    exit 1
}

Get-ChildItem $Output -Filter 'ReadyM.SDK.Wukong.*.nupkg' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 |
        ForEach-Object {
            Write-Output ''
            Write-Output "Built $($_.Name) ($([math]::Round($_.Length / 1MB, 1)) MB)"
        }
