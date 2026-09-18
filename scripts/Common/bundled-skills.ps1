# SPDX-License-Identifier: MIT

function Get-BundledSkillFiles {
    <# .SYNOPSIS Returns only the reviewed files shipped in the built-in skill catalog. #>
    @(
        'skills/git/SKILL.md'
        'skills/filesystem/SKILL.md'
        'skills/concat-files/SKILL.md'
        'skills/concat-files/scripts/run.ps1'
        'skills/clipboard/SKILL.md'
        'skills/clipboard/scripts/run.ps1'
        'skills/screenshot/SKILL.md'
        'skills/screenshot/scripts/run.ps1'
        'skills/system_info/SKILL.md'
        'skills/system_info/scripts/run.ps1'
        'skills/http_request/SKILL.md'
        'skills/http_request/scripts/run.ps1'
        'skills/web_search/SKILL.md'
        'skills/web_search/scripts/run.ps1'
        'skills/timezone_convert/SKILL.md'
        'skills/timezone_convert/scripts/run.ps1'
        'skills/set_reminder/SKILL.md'
    )
}
