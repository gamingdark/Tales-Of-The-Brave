[CmdletBinding()]
param(
    [string] $WebsiteRepository,
    [string] $UnityPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($WebsiteRepository)) {
    $WebsiteRepository = Join-Path $projectRoot 'WebsitePublish'
}
$WebsiteRepository = [IO.Path]::GetFullPath($WebsiteRepository)
$gameRelativePath = 'games/tales-of-the-brave'
$buildPath = [IO.Path]::GetFullPath((Join-Path $WebsiteRepository $gameRelativePath))

function Invoke-Git {
    param(
        [Parameter(Mandatory)] [string] $Repository,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $safeRepository = $Repository.Replace('\', '/')
    try {
        $output = & git -c "safe.directory=$safeRepository" -C $Repository @Arguments 2>&1
        $gitExitCode = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
    if ($gitExitCode -ne 0) {
        throw "git $($Arguments -join ' ') failed in '$Repository':`n$($output -join [Environment]::NewLine)"
    }
    return $output
}

function Assert-CleanRepository {
    param([Parameter(Mandatory)] [string] $Repository)

    $changes = @(Invoke-Git $Repository @('status', '--porcelain=v1', '--untracked-files=all'))
    if ($changes.Count -gt 0) {
        throw "Repository '$Repository' is not clean:`n$($changes -join [Environment]::NewLine)"
    }
}

function Find-UnityEditor {
    param([string] $RequestedPath)

    if (-not [string]::IsNullOrWhiteSpace($RequestedPath)) {
        $resolved = [IO.Path]::GetFullPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
            throw "Unity executable was not found at '$resolved'."
        }
        return $resolved
    }

    $versionLine = Get-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') -First 1
    $version = ($versionLine -split ':', 2)[1].Trim()
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe",
        "D:\Gamedev\UNITY EDITORS\2021.3.28f1\$version\Editor\Unity.exe"
    )
    $command = Get-Command Unity.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) { $candidates += $command.Source }

    $found = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if ($null -eq $found) {
        throw 'Unity Editor was not found. Pass its full path with -UnityPath.'
    }
    return $found
}

if (-not (Test-Path -LiteralPath (Join-Path $projectRoot '.git'))) {
    throw "Main Unity repository was not found at '$projectRoot'."
}
if (-not (Test-Path -LiteralPath (Join-Path $WebsiteRepository '.git'))) {
    throw "Website repository was not found at '$WebsiteRepository'."
}

# Unity cannot open the same project in batch mode while the Editor owns its lock.
# Check this before any website checkout, branch creation, or output deletion.
$unityLockFile = Join-Path $projectRoot 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $unityLockFile) {
    throw "The Unity project is open. Close the Unity Editor before running command-line deployment. For a manual build while the Editor is open, use 'Tales of Voyages > Build WebGL to WebsitePublish'."
}

# The deployment must describe exactly the source state already published to origin/main.
Assert-CleanRepository $projectRoot
Invoke-Git $projectRoot @('fetch', 'origin', 'main') | Out-Null
$mainBranch = (Invoke-Git $projectRoot @('branch', '--show-current') | Select-Object -First 1).Trim()
if ($mainBranch -ne 'main') {
    throw "The Unity repository must be on branch 'main', but is on '$mainBranch'."
}
$sourceCommit = (Invoke-Git $projectRoot @('rev-parse', 'HEAD') | Select-Object -First 1).Trim()
$originMain = (Invoke-Git $projectRoot @('rev-parse', 'origin/main') | Select-Object -First 1).Trim()
if ($sourceCommit -ne $originMain) {
    throw "Local main ($sourceCommit) is not identical to origin/main ($originMain). Commit and push all changes first."
}

$shortCommit = (Invoke-Git $projectRoot @('rev-parse', '--short=10', $originMain) | Select-Object -First 1).Trim()
$sourceSubject = (Invoke-Git $projectRoot @('show', '-s', '--format=%s', $originMain) | Select-Object -First 1).Trim()
$deploymentBranch = "build/tales-of-the-brave-$shortCommit"
$commitMessage = "Deploy Tales of the Brave $shortCommit - $sourceSubject"

# Pull only from a pristine website master checkout.
Assert-CleanRepository $WebsiteRepository
Invoke-Git $WebsiteRepository @('checkout', 'master') | Out-Null
Invoke-Git $WebsiteRepository @('pull', '--ff-only', 'origin', 'master') | Out-Null
Assert-CleanRepository $WebsiteRepository

$localBranch = @(Invoke-Git $WebsiteRepository @('branch', '--list', $deploymentBranch))
if ($localBranch.Count -gt 0) {
    throw "Local deployment branch '$deploymentBranch' already exists."
}
$remoteBranch = @(Invoke-Git $WebsiteRepository @('ls-remote', '--heads', 'origin', $deploymentBranch))
if ($remoteBranch.Count -gt 0) {
    throw "Remote deployment branch '$deploymentBranch' already exists."
}
Invoke-Git $WebsiteRepository @('checkout', '-b', $deploymentBranch, 'master') | Out-Null

$websiteGamesRoot = [IO.Path]::GetFullPath((Join-Path $WebsiteRepository 'games'))
if (-not $buildPath.StartsWith($websiteGamesRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clear unsafe build path '$buildPath'."
}
if (Test-Path -LiteralPath $buildPath) {
    Remove-Item -LiteralPath $buildPath -Recurse -Force
}
New-Item -ItemType Directory -Path $buildPath -Force | Out-Null

$UnityPath = Find-UnityEditor $UnityPath
$env:TALES_WEB_BUILD_PATH = $buildPath
try {
    $buildLogPath = Join-Path $projectRoot 'Logs\WebDeploymentBuild.log'
    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', "`"$projectRoot`"",
        '-buildTarget', 'WebGL',
        '-logFile', "`"$buildLogPath`""
    )
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArguments `
        -Wait -PassThru -WindowStyle Hidden
    if ($unityProcess.ExitCode -ne 0) {
        throw "Unity WebGL build failed with exit code $($unityProcess.ExitCode). See Logs/WebDeploymentBuild.log."
    }
}
finally {
    Remove-Item Env:TALES_WEB_BUILD_PATH -ErrorAction SilentlyContinue
}

if (-not (Test-Path -LiteralPath (Join-Path $buildPath 'index.html') -PathType Leaf) -or
    -not (Test-Path -LiteralPath (Join-Path $buildPath 'Build') -PathType Container)) {
    throw "Unity did not produce the expected WebGL build at '$buildPath'."
}

# Reject tracked, staged, or untracked changes outside the one deployment folder.
$changedPaths = @()
$changedPaths += @(Invoke-Git $WebsiteRepository @('diff', '--name-only'))
$changedPaths += @(Invoke-Git $WebsiteRepository @('diff', '--cached', '--name-only'))
$changedPaths += @(Invoke-Git $WebsiteRepository @('ls-files', '--others', '--exclude-standard'))
$changedPaths = @($changedPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
$outsideChanges = @($changedPaths | Where-Object {
    $_ -ne $gameRelativePath -and -not $_.StartsWith($gameRelativePath + '/', [StringComparison]::Ordinal)
})
if ($outsideChanges.Count -gt 0) {
    throw "Refusing deployment because website paths outside '$gameRelativePath' changed:`n$($outsideChanges -join [Environment]::NewLine)"
}

Invoke-Git $WebsiteRepository @('add', '-A', '--', $gameRelativePath) | Out-Null
$stagedPaths = @(Invoke-Git $WebsiteRepository @('diff', '--cached', '--name-only'))
if ($stagedPaths.Count -eq 0) {
    throw 'The new build is identical to website master; there is nothing to commit.'
}
$invalidStagedPaths = @($stagedPaths | Where-Object {
    $_ -ne $gameRelativePath -and -not $_.StartsWith($gameRelativePath + '/', [StringComparison]::Ordinal)
})
if ($invalidStagedPaths.Count -gt 0) {
    throw "Refusing to commit paths outside '$gameRelativePath':`n$($invalidStagedPaths -join [Environment]::NewLine)"
}

Invoke-Git $WebsiteRepository @('commit', '-m', $commitMessage) | Out-Null
Invoke-Git $WebsiteRepository @('push', '--set-upstream', 'origin', $deploymentBranch) | Out-Null

Write-Host "Deployment branch pushed: $deploymentBranch"
Write-Host "Commit message: $commitMessage"
