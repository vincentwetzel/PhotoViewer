[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'Install')]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$InstallPath,

    [Parameter(Mandatory = $true, ParameterSetName = 'Uninstall')]
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
$progId = 'PhotoViewer.Image'
$extensions = '.jpg', '.jpeg', '.png', '.bmp', '.gif', '.mp4', '.mov', '.avi', '.mkv', '.webm'

function Set-UserRegistryValue {
    param([string]$Path, [string]$Name, [string]$Value)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -Path $Path -Force | Out-Null
    }
    New-ItemProperty -LiteralPath $Path -Name $Name -Value $Value -PropertyType String -Force | Out-Null
}

if ($Uninstall) {
    $commandKey = "HKCU:\Software\Classes\$progId\shell\open\command"
    if ((Test-Path -LiteralPath $commandKey) -and
        ((Get-ItemProperty -LiteralPath $commandKey -Name '(default)' -ErrorAction SilentlyContinue).'(default)' -like '*PhotoViewer.Image*')) {
        Remove-Item -LiteralPath "HKCU:\Software\Classes\$progId" -Recurse -Force
    }

    foreach ($extension in $extensions) {
        $extensionKey = "HKCU:\Software\Classes\$extension"
        $openWithKey = "$extensionKey\OpenWithProgids"
        if (Test-Path -LiteralPath $openWithKey) {
            Remove-ItemProperty -LiteralPath $openWithKey -Name $progId -ErrorAction SilentlyContinue
            if (-not (Get-ItemProperty -LiteralPath $openWithKey -ErrorAction SilentlyContinue).PSObject.Properties.Where({ $_.Name -notlike 'PS*' })) {
                Remove-Item -LiteralPath $openWithKey -Recurse -Force
            }
        }
    }

    Write-Host 'PhotoViewer per-user file associations removed.'
    exit 0
}

$resolvedInstallPath = (Resolve-Path -LiteralPath $InstallPath).Path
$command = '"{0}" "%1"' -f $resolvedInstallPath
$progIdKey = "HKCU:\Software\Classes\$progId"
Set-UserRegistryValue -Path $progIdKey -Name '(default)' -Value 'PhotoViewer media file'
Set-UserRegistryValue -Path "$progIdKey\shell\open\command" -Name '(default)' -Value $command

foreach ($extension in $extensions) {
    $openWithKey = "HKCU:\Software\Classes\$extension\OpenWithProgids"
    Set-UserRegistryValue -Path $openWithKey -Name $progId -Value ''
}

Write-Host "PhotoViewer associations registered for $resolvedInstallPath. Existing default handlers were not changed."
