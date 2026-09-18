---
name: http_request
description: Request a public HTTPS URL with GET, HEAD, POST or PUT and inspect a bounded response.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
---

# HTTP request

Enable this package in `hm --skills`, choose **run**, and enter JSON parameters:

```json
{"Url":"https://example.com","Method":"GET"}
```

`Url` is required. `Method` defaults to `GET`; `HEAD`, `POST`, and `PUT` are also supported. For a write request, supply `Body` as a JSON string and optionally `ContentType` (`application/json` or `text/plain`). Review the destination and full body before approving: POST and PUT may change remote data.

Only public HTTPS destinations on port 443 are allowed. There are no custom headers, authentication, cookies, proxies, or redirects. Never pass secrets in a URL or body. DNS answers are checked and the connection uses a checked IP address.

Requests stop after 15 seconds. The JSON result includes the HTTP status and a 4 KiB UTF-8 body preview; `Truncated` marks a longer body and `Omitted` marks binary or compressed content. HTTP error responses are displayed and then reported as failures. This package grants no execution permission.
