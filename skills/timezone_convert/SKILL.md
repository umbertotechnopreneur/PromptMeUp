---
name: timezone_convert
description: Convert explicit dates or the current time using the operating system time zone database.
version: 1.0.1
icon: "🕒"
color: "#F2CDCD"
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
---

# Time zone conversion

Open `hm --skills`, enable this package, then choose `run`.

- Now in Tokyo: `{"ToTimeZone":"Asia/Tokyo"}`
- Convert a date: `{"FromTimeZone":"Europe/Rome","ToTimeZone":"Asia/Ho_Chi_Minh","Time":"2026-09-18T09:00"}`
- Find IDs: `{"Action":"list","Filter":"Europe"}`

Use exact IANA or Windows IDs supported by your operating system, not city fragments or abbreviations. The list action returns up to 50 matching IDs. `FromTimeZone` defaults to the local zone; without `Time`, conversion uses the current instant.

Explicit dates use `yyyy-MM-ddTHH:mm` or `yyyy-MM-ddTHH:mm:ss`, with no offset. Times skipped or repeated by daylight saving changes are rejected rather than guessed. Calculations are local; no web service or AI inference is used.
