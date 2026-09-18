# Adapted for PromptMeUp from the author's CLI-Intelligence system information tool.
[CmdletBinding()]
param(
    [string]$Action = 'overview',
    [string]$_ValidationError = 'The system information action is unavailable, invalid, or exceeds its limits.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
switch -CaseSensitive -Exact ($Action) {
    'overview' {
        [pscustomobject]@{
            OS = [Runtime.InteropServices.RuntimeInformation]::OSDescription
            Architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
            ProcessArchitecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
            Runtime = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
            Processors = [Environment]::ProcessorCount
            ProcessWorkingSetBytes = [Environment]::WorkingSet
            UptimeSeconds = [long]([Environment]::TickCount64 / 1000)
        } | ConvertTo-Json -Compress
    }
    'env' {
        $names = @([Environment]::GetEnvironmentVariables().Keys | Sort-Object)
        [pscustomobject]@{ Names = @($names | Select-Object -First 100); Truncated = $names.Count -gt 100 } | ConvertTo-Json -Compress
    }
    'processes' {
        $processes = @([Diagnostics.Process]::GetProcesses())
        try {
            if ($processes.Count -gt 4096) { throw $_ValidationError }
            $items = [Collections.Generic.List[object]]::new()
            $unavailable = 0
            foreach ($process in $processes) {
                try {
                    $items.Add([pscustomobject]@{ Id = $process.Id; Name = $process.ProcessName; WorkingSetBytes = $process.WorkingSet64 })
                }
                catch [InvalidOperationException] { $unavailable++ }
                catch [ComponentModel.Win32Exception] { $unavailable++ }
            }
            [pscustomobject]@{
                Processes = @($items | Sort-Object WorkingSetBytes -Descending | Select-Object -First 15)
                UnavailableProcesses = $unavailable
                Truncated = $items.Count -gt 15
            } | ConvertTo-Json -Depth 3 -Compress
        }
        finally { foreach ($process in $processes) { $process.Dispose() } }
    }
    'disk' {
        $drives = @([IO.DriveInfo]::GetDrives() | Where-Object { $_.DriveType -in @([IO.DriveType]::Fixed, [IO.DriveType]::Removable, [IO.DriveType]::Ram) })
        if ($drives.Count -gt 64) { throw $_ValidationError }
        $items = [Collections.Generic.List[object]]::new()
        foreach ($drive in $drives) {
            if ($drive.IsReady) {
                $items.Add([pscustomobject]@{ Index = $items.Count + 1; Type = $drive.DriveType.ToString(); Format = $drive.DriveFormat; FreeBytes = $drive.AvailableFreeSpace; TotalBytes = $drive.TotalSize })
            }
        }
        [pscustomobject]@{ Disks = $items.ToArray() } | ConvertTo-Json -Depth 3 -Compress
    }
    'network' {
        $interfaces = @([Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces())
        if ($interfaces.Count -gt 256) { throw $_ValidationError }
        $groups = @($interfaces | Group-Object { $_.NetworkInterfaceType.ToString() + '/' + $_.OperationalStatus.ToString() } | Sort-Object Name | ForEach-Object {
            [pscustomobject]@{ TypeAndStatus = $_.Name; Count = $_.Count }
        })
        [pscustomobject]@{ Interfaces = $groups } | ConvertTo-Json -Depth 3 -Compress
    }
    default { throw $_ValidationError }
}
