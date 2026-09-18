---
name: system_info
description: Inspect bounded system information without environment values or network addresses.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
---

# System information

Open `hm --skills`, enable this package, then run it with an `Action`:

| JSON input | Result |
| --- | --- |
| `{"Action":"overview"}` | OS, architecture, runtime, processor count and uptime |
| `{"Action":"env"}` | Up to 100 environment variable names, never values |
| `{"Action":"processes"}` | Up to 15 processes, sorted by memory use |
| `{"Action":"disk"}` | Local disk type, format and free space; no mount paths |
| `{"Action":"network"}` | Interface counts grouped by type and status; no addresses |

The default action is `overview`. Every action requires normal command approval. Host names, user names, command lines, environment values, IP addresses and MAC addresses are omitted. Process names may still reveal which applications are running.
