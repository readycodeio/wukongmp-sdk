#!powershell.exe -ExecutionPolicy Bypass -File

<#
    Builds the WukongMP SDK NuGet packages.

    Three packages, matching the three project shapes a mod has:

      ReadyM.SDK.Wukong.Common   shared types, ECS components, RPC contracts, the generator
      ReadyM.SDK.Wukong.Client   the in-game half, plus the mod loader assemblies
      ReadyM.SDK.Wukong.Server   the relay-server half

    Client and Server both depend on Common and neither depends on the other, so a mod's
    server-side project cannot reference client-only API and vice versa. The core SDK is not
    a package of its own: it has no version of its own, so its assemblies are distributed
    across these three by which side actually needs them.

    Third-party assemblies are not bundled. They are declared as package dependencies, and
    that list is generated from the projects whose assemblies each package ships rather than
    maintained by hand.

      -PackageVersion      override the version, e.g. 0.4.0-preview.1
      -SyncDependencies    regenerate the dependency lists in the packaging projects
      -CheckDependencies   fail if a list has drifted, without changing anything
#>

param(
    [string] $Configuration = 'Release',
    [string] $Output = (Join-Path $PSScriptRoot '..\local-nuget-feed'),
    # Overrides the version from Directory.Build.props. Passed as a global property so the
    # dependency the client and server packages record on the shared one carries the same
    # version, rather than the three drifting apart.
    [string] $PackageVersion,
    # An extra NuGet source, added to the configured ones rather than replacing them. Needed
    # when a dependency is only in a local feed, such as a ReadyM.Wukong.GameRefs version
    # that has not been published yet.
    [string] $AdditionalSource,
    [switch] $SyncDependencies,
    [switch] $CheckDependencies
)

$ErrorActionPreference = 'Stop'

$coreSdk = Join-Path $PSScriptRoot 'readym-core-sdk\src'
$packagingDir = Join-Path $PSScriptRoot 'Packaging'

# The shared set, referenced by both sides. Listed once and used for both frameworks.
$sharedProjects = @(
    "$PSScriptRoot\ReadyM.Wukong.Common\ReadyM.Wukong.Common.csproj"
    "$coreSdk\ReadyM.Api\ReadyM.Api.csproj"
    "$coreSdk\ReadyM.Api.Multiplayer\ReadyM.Api.Multiplayer.csproj"
    "$coreSdk\YooniCSharp\Native\Container\Yooni.Native.Container.csproj"
    "$coreSdk\YooniCSharp\Native\LowLevel\Yooni.Native.LowLevel.csproj"
    "$coreSdk\YooniCSharp\Native\Serialization\Yooni.Native.Serialization.csproj"
    "$coreSdk\Friflo.Engine.ECS\src\ECS\Friflo.Engine.ECS.csproj"
    "$coreSdk\Friflo.Engine.ECS\src\ECS.Boost\Friflo.Engine.ECS.Boost.csproj"
    "$coreSdk\LiteNetLib\LiteNetLib\LiteNetLib.csproj"
)

# Packed in this order: Common first, so a feed is never left with a client or server
# package whose shared dependency is not there yet.
#
# Buckets are the projects whose assemblies each package ships, per framework. They exist so
# the dependency list can be read from them, and they must stay in step with the
# SdkPackageAssembly lists in the packaging projects.
$packages = @(
    [ordered]@{
        Name = 'ReadyM.SDK.Wukong.Common'
        Buckets = [ordered]@{
            'netstandard2.0' = $sharedProjects
            'net10.0' = $sharedProjects
        }
    }
    [ordered]@{
        Name = 'ReadyM.SDK.Wukong.Client'
        Buckets = [ordered]@{
            'netstandard2.0' = @(
                "$PSScriptRoot\WukongMp.Sdk\WukongMp.Sdk.csproj"
                "$PSScriptRoot\WukongMp.Api\WukongMp.Api.csproj"
                "$coreSdk\ReadyM.Relay.Client\ReadyM.Relay.Client.csproj"
            )
        }
    }
    [ordered]@{
        Name = 'ReadyM.SDK.Wukong.Server'
        Buckets = [ordered]@{
            'net10.0' = @(
                "$PSScriptRoot\WukongMp.Sdk.Serverside\WukongMp.Sdk.Serverside.csproj"
                "$coreSdk\ReadyM.Relay.Server.Sdk\ReadyM.Relay.Server.Sdk.csproj"
            )
        }
    }
)

foreach ($p in $packages)
{
    $p.Project = Join-Path $packagingDir "$($p.Name)\$($p.Name).csproj"
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

function Get-BucketDependencies([string] $tfm, [array] $projects)
{
    $found = @{}
    foreach ($proj in $projects)
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

function Format-DependencyRegion($package)
{
    $sb = [System.Text.StringBuilder]::new()
    foreach ($tfm in $package.Buckets.Keys)
    {
        $deps = Get-BucketDependencies $tfm $package.Buckets[$tfm]
        # Write-Host, not Write-Output: anything written to the pipeline here would end up
        # concatenated into this function's return value and land in the csproj.
        Write-Host "    $tfm : $($deps.Count) dependencies"
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
    $drifted = @()

    foreach ($package in $packages)
    {
        Write-Host "$($package.Name):"
        $text = Get-Content $package.Project -Raw
        $bi = $text.IndexOf($begin)
        $ei = $text.IndexOf($end)
        if ($bi -lt 0 -or $ei -lt 0)
        {
            Write-Error "generated markers not found in $($package.Project)"
        }

        $fresh = Format-DependencyRegion $package
        $current = $text.Substring($bi + $begin.Length, $ei - $bi - $begin.Length).Trim([char]13, [char]10)

        if ($current.Replace([string][char]13, '') -eq $fresh.Replace([string][char]13, ''))
        {
            Write-Host '    up to date'
            continue
        }

        if (-not $SyncDependencies)
        {
            $drifted += $package.Name
            continue
        }

        $nl = [string][char]13 + [string][char]10
        $updated = $text.Substring(0, $bi + $begin.Length) + $nl + $fresh + $nl + $text.Substring($ei)
        Set-Content -Path $package.Project -Value $updated -NoNewline
        Write-Host '    updated'
    }

    if ($drifted.Count -gt 0)
    {
        Write-Output ''
        Write-Output "Dependency lists have drifted: $($drifted -join ', '). Run with -SyncDependencies."
        exit 1
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

$packArgs = @()
if ($PackageVersion)
{
    $packArgs += @("-p:PackageVersion=$PackageVersion", "-p:Version=$PackageVersion")
}
if ($AdditionalSource)
{
    $packArgs += "-p:RestoreAdditionalProjectSources=$AdditionalSource"
}

foreach ($package in $packages)
{
    Write-Output ''
    Write-Output "Packing $($package.Name) ($Configuration)$(if ($PackageVersion) { " $PackageVersion" })"
    & dotnet pack $package.Project -c $Configuration -o $Output @packArgs
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "pack failed for $($package.Name)"
        exit 1
    }
}

Write-Output ''
Write-Output "Built into $Output"
foreach ($package in $packages)
{
    Get-ChildItem $Output -Filter "$($package.Name).*.nupkg" |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1 |
            ForEach-Object { Write-Output "  $($_.Name)  $([math]::Round($_.Length / 1MB, 1)) MB" }
}
