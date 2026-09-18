---
name: web_search
description: Find DuckDuckGo instant-answer summaries and related topics without an API key.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
---

# Web search

Enable this package in `hm --skills`, choose **run**, and enter JSON parameters:

```json
{"Query":"PowerShell","MaxResults":3}
```

`Query` is required (up to 300 characters). `MaxResults` defaults to 5 and accepts 1–10.

Your query is sent to DuckDuckGo only after command approval. Do not include private information or credentials. This is the Instant Answer API: it returns summaries and related topics, not a full web search or page crawler. An empty `Results` list is a valid response.

The request uses public HTTPS with checked IP addresses, no proxy, redirects, cookies, or API key. It stops after 15 seconds and rejects provider responses larger than 128 KiB. Each result has a short excerpt and an optional HTTPS link; links are not opened automatically. This package grants no execution permission.
