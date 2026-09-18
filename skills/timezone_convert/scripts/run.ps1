# Adapted for PromptMeUp from the author's CLI-Intelligence time zone tool.
[CmdletBinding()]
param(
    [string]$Action = 'convert',
    [string]$FromTimeZone = '',
    [string]$ToTimeZone = '',
    [string]$Time = '',
    [string]$Filter = '',
    [string]$_ValidationError = 'Use valid time zone IDs and ISO dates; ambiguous or missing daylight saving times are not supported.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
foreach ($value in @($FromTimeZone, $ToTimeZone, $Time, $Filter)) {
    if ($value.Length -gt 100 -or $value -match '[\x00-\x1F\x7F]') { throw $_ValidationError }
}
if ($Action -ceq 'list') {
    if ($PSBoundParameters.ContainsKey('FromTimeZone') -or $PSBoundParameters.ContainsKey('ToTimeZone') -or $PSBoundParameters.ContainsKey('Time')) { throw $_ValidationError }
    $zones = @([TimeZoneInfo]::GetSystemTimeZones() | Where-Object { $_.Id.Contains($Filter, [StringComparison]::OrdinalIgnoreCase) } | Sort-Object Id)
    [pscustomobject]@{ Zones = @($zones | Select-Object -First 50 -ExpandProperty Id); Truncated = $zones.Count -gt 50 } | ConvertTo-Json -Compress
    return
}
if ($Action -cne 'convert' -or [string]::IsNullOrWhiteSpace($ToTimeZone) -or $PSBoundParameters.ContainsKey('Filter')) { throw $_ValidationError }
$from = if ([string]::IsNullOrEmpty($FromTimeZone)) { [TimeZoneInfo]::Local } else { [TimeZoneInfo]::FindSystemTimeZoneById($FromTimeZone) }
$to = [TimeZoneInfo]::FindSystemTimeZoneById($ToTimeZone)
if ($PSBoundParameters.ContainsKey('Time')) {
    $sourceDate = [DateTime]::MinValue
    if (-not [DateTime]::TryParseExact($Time, [string[]]@('yyyy-MM-ddTHH:mm', 'yyyy-MM-ddTHH:mm:ss'), [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$sourceDate)) { throw $_ValidationError }
    $sourceDate = [DateTime]::SpecifyKind($sourceDate, [DateTimeKind]::Unspecified)
    if ($from.IsInvalidTime($sourceDate) -or $from.IsAmbiguousTime($sourceDate)) { throw $_ValidationError }
    $source = [DateTimeOffset]::new($sourceDate, $from.GetUtcOffset($sourceDate))
}
else { $source = [TimeZoneInfo]::ConvertTime([DateTimeOffset]::UtcNow, $from) }
$target = [TimeZoneInfo]::ConvertTime($source, $to)
[pscustomobject]@{
    FromTimeZone = $from.Id
    SourceTime = $source.ToString('yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture)
    ToTimeZone = $to.Id
    TargetTime = $target.ToString('yyyy-MM-ddTHH:mm:sszzz', [Globalization.CultureInfo]::InvariantCulture)
} | ConvertTo-Json -Compress
