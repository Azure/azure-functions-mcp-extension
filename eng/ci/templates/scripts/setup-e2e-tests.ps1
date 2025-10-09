#Requires -Version 6

param(
    [Switch]
    $SkipStorageEmulator,

    [Switch]
    $SkipCoreTools,

    [String]
    $RepoRoot
)

$FunctionsRuntimeVersion = 4

if (!$IsWindows -and !$IsLinux -and !$IsMacOs)
{
    # For pre-PS6
    Write-Host "Could not resolve OS. Assuming Windows."
    $IsWindows = $true
}

if($SkipCoreTools)
{
  Write-Host
  Write-Host "---Skipping Core Tools download---"  
}
else
{
  $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
  if ($IsWindows) {
      $os = "win"
  }
  elseif ($IsMacOS) {
      $os = "osx"
  }
  elseif ($IsLinux) {
      $os = "linux"
  }
  else {
    throw "Unsupported operating system detected. Please run this script on Windows, macOS, or Linux."
  }

  Write-Host ""
  Write-Host "Using latest Core Tools release from GitHub..."

  # GitHub API call for latest release
  $releaseInfo = Invoke-RestMethod -Uri "https://api.github.com/repos/Azure/azure-functions-core-tools/releases/latest" -Headers @{ "User-Agent" = "PowerShell" }

  $latestVersion = $releaseInfo.tag_name
  Write-Host "`nLatest Core Tools version: $latestVersion"

  # Look for zip file matching os and arch
  $pattern = "Azure\.Functions\.Cli\.$os-$arch\..*\.zip$"
  $asset = $releaseInfo.assets | Where-Object {
      $_.name -match $pattern
  }

  if (-not $asset) {
      Write-Error "Could not find a Core Tools .zip for OS '$os' and arch '$arch'"
      exit 1
  }

  $coreToolsURL = $asset.browser_download_url

  # Add query string to avoid caching issues
  $coreToolsURL = $coreToolsURL + "?raw=true"

  Write-Host ""
  Write-Host "---Downloading the Core Tools for Functions V$FunctionsRuntimeVersion---"
  Write-Host "Core Tools download url: $coreToolsURL"

  $FUNC_CLI_DIRECTORY = Join-Path $RepoRoot 'Azure.Functions.Cli'
  Write-Host 'Deleting Functions Core Tools if exists...'
  Remove-Item -Force "$FUNC_CLI_DIRECTORY.zip" -ErrorAction Ignore
  Remove-Item -Recurse -Force $FUNC_CLI_DIRECTORY -ErrorAction Ignore

  if ($versionUrl)
  {
    $version = Invoke-RestMethod -Uri $versionUrl
    Write-Host "Downloading Functions Core Tools (Version: $version)..."
  }

  $output = "$FUNC_CLI_DIRECTORY.zip"
  Invoke-RestMethod -Uri $coreToolsURL -OutFile $output

  Write-Host 'Extracting Functions Core Tools...'
  Expand-Archive $output -DestinationPath $FUNC_CLI_DIRECTORY

  Write-Host "Downloaded core tools to: $FUNC_CLI_DIRECTORY"

  if ($IsMacOS -or $IsLinux)
  {
    & "chmod" "a+x" "$FUNC_CLI_DIRECTORY/func"
  }
  
  Write-Host "------"
}

if (Test-Path $output) 
{
  Remove-Item $output -Recurse -Force -ErrorAction Ignore
}

function IsStorageEmulatorRunning()
{
    try
    {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:10000/"
        $StatusCode = $Response.StatusCode
    }
    catch
    {
        $StatusCode = $_.Exception.Response.StatusCode.value__
    }

    if ($StatusCode -eq 400)
    {
        return $true
    }

    return $false
}

if ($SkipStorageEmulator)
{
  Write-Host
  Write-Host "---Skipping emulator startup---"
  Write-Host
}
else 
{
    Write-Host "------"
    Write-Host ""
    Write-Host "---Starting Storage emulator---"
    $storageEmulatorRunning = IsStorageEmulatorRunning

    if ($storageEmulatorRunning -eq $false)
    {
        if ($IsWindows)
        {
            npm install -g azurite
            Start-Process azurite.cmd -ArgumentList "--silent"
        }
        else
        {
            sudo npm install -g azurite
            sudo mkdir azurite
            sudo azurite --silent --location azurite --debug azurite\debug.log &
        }

        $startedStorage = $true
    }
    else
    {
        Write-Host "Storage emulator is already running."
    }

    Write-Host "------"
    Write-Host
}
