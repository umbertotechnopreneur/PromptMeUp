---
title: Code header templates
aliases:
  - Source file header library
  - Umberto Giacobbi code headers
tags:
  - assets
  - templates
  - source-code
  - licensing
---

# Code header templates

> Reusable source-file identity and licensing headers for public and private
> repositories.

These templates keep attribution visible without turning each source file into
a legal document. The repository-level `LICENSE`, `NOTICE`, `README`, and
contribution files remain authoritative.

## Placeholder contract

| Placeholder | Replace with |
| --- | --- |
| `{{PROJECT_NAME}}` | Canonical project or product name |
| `{{FILE_NAME}}` | File name, including its extension |
| `{{FILE_PURPOSE}}` | One concise sentence describing responsibility, not implementation history |
| `{{COPYRIGHT_YEARS}}` | Creation year or maintained range, such as `2026` or `2024-2026` |
| `{{COPYRIGHT_HOLDER}}` | Copyright owner, normally `Umberto Giacobbi` |
| `{{AUTHOR}}` | Primary author or responsible maintainer |
| `{{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}` | Canonical public repository URL; remove the complete line for private repositories |
| `{{LICENSE_NAME}}` | Human-readable license name |
| `{{SPDX_LICENSE_IDENTIFIER}}` | Valid SPDX expression, for example `MIT`, `Apache-2.0`, or `PolyForm-Noncommercial-1.0.0` |

For proprietary repositories, use the organization-approved license wording
and an approved SPDX `LicenseRef-...` identifier. Do not invent a public license
or repository URL.

## C#

```csharp
// -----------------------------------------------------------------------------
// Project:    {{PROJECT_NAME}}
// File:       {{FILE_NAME}}
// Purpose:    {{FILE_PURPOSE}}
// Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
// Author:     {{AUTHOR}}
// Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
// License:    {{LICENSE_NAME}}
// SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
// Open source: https://umbertogiacobbi.biz/opensource
// -----------------------------------------------------------------------------
```

Individual file: `templates/source-header.cs.txt`

## PowerShell

```powershell
<#
    Project:    {{PROJECT_NAME}}
    File:       {{FILE_NAME}}
    Purpose:    {{FILE_PURPOSE}}
    Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
    Author:     {{AUTHOR}}
    Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
    License:    {{LICENSE_NAME}}
    SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
    Open source: https://umbertogiacobbi.biz/opensource
#>
```

Place comment-based help (`.SYNOPSIS`, `.DESCRIPTION`, and parameters)
immediately after this identity header.

Individual file: `templates/source-header.ps1.txt`

## C source

```c
/* --------------------------------------------------------------------------
 * Project:    {{PROJECT_NAME}}
 * File:       {{FILE_NAME}}
 * Purpose:    {{FILE_PURPOSE}}
 * Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
 * Author:     {{AUTHOR}}
 * Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
 * License:    {{LICENSE_NAME}}
 * SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
 * Open source: https://umbertogiacobbi.biz/opensource
 * -------------------------------------------------------------------------- */
```

Individual file: `templates/source-header.c.txt`

## C++ source

```cpp
// -----------------------------------------------------------------------------
// Project:    {{PROJECT_NAME}}
// File:       {{FILE_NAME}}
// Purpose:    {{FILE_PURPOSE}}
// Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
// Author:     {{AUTHOR}}
// Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
// License:    {{LICENSE_NAME}}
// SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
// Open source: https://umbertogiacobbi.biz/opensource
// -----------------------------------------------------------------------------
```

Individual file: `templates/source-header.cpp.txt`

## C header (`.h`)

```c
/* --------------------------------------------------------------------------
 * Project:    {{PROJECT_NAME}}
 * File:       {{FILE_NAME}}
 * Purpose:    {{FILE_PURPOSE}}
 * Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
 * Author:     {{AUTHOR}}
 * Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
 * License:    {{LICENSE_NAME}}
 * SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
 * Open source: https://umbertogiacobbi.biz/opensource
 * -------------------------------------------------------------------------- */
```

Individual file: `templates/source-header.h.txt`

## C++ header (`.hpp`)

```cpp
// -----------------------------------------------------------------------------
// Project:    {{PROJECT_NAME}}
// File:       {{FILE_NAME}}
// Purpose:    {{FILE_PURPOSE}}
// Copyright:  (c) {{COPYRIGHT_YEARS}} {{COPYRIGHT_HOLDER}}
// Author:     {{AUTHOR}}
// Repository: {{PUBLIC_REPOSITORY_URL_OR_OMIT_LINE}}
// License:    {{LICENSE_NAME}}
// SPDX-License-Identifier: {{SPDX_LICENSE_IDENTIFIER}}
// Open source: https://umbertogiacobbi.biz/opensource
// -----------------------------------------------------------------------------
```

Individual file: `templates/source-header.hpp.txt`

## Public repository profile

- Keep the `Repository:` line and use the canonical HTTPS URL.
- Use the exact license name and SPDX identifier from the repository.
- Keep the `Open source:` link in every file.
- Do not publish with unresolved placeholders.

## Private repository profile

- Remove the complete `Repository:` line rather than inserting an internal URL.
- Use approved proprietary wording and `LicenseRef-...` only when defined by
  the organization.
- Keep the `Open source:` link as the public licensing and authorization
  reference.
- Never add internal endpoints, ticket references, credentials, customer names,
  or infrastructure details to a file header.

## Final check

```powershell
pwsh -NoProfile -Command "rg -n '\{\{[A-Z0-9_]+\}\}' ."
```

The check should return no matches in files intended for publication.
