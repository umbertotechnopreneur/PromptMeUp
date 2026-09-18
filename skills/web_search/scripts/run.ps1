# Adapted for PromptMeUp from the author's CLI-Intelligence DuckDuckGo tool.
[CmdletBinding()]
param(
    [string]$Query = '',
    [int]$MaxResults = 5,
    [string]$_ValidationError = 'Request failed or input is invalid.'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
try {
    $networkSource = @'
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public static class PromptMeUpWebSearchSkill
{
    public sealed class Response
    {
        public int StatusCode { get; set; }
        public string MediaType { get; set; }
        public string Body { get; set; }
        public bool Truncated { get; set; }
        public bool Omitted { get; set; }
    }

    /// <summary>Rejects local, reserved, documentation, and transition addresses before opening a socket.</summary>
    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            int a = bytes[0], b = bytes[1], c = bytes[2];
            return a != 0 && a != 10 && a != 127 && a < 224
                && !(a == 100 && b >= 64 && b <= 127)
                && !(a == 169 && b == 254) && !(a == 172 && b >= 16 && b <= 31)
                && !(a == 192 && b == 0 && (c == 0 || c == 2))
                && !(a == 192 && b == 88 && c == 99) && !(a == 192 && b == 168)
                && !(a == 198 && (b == 18 || b == 19))
                && !(a == 198 && b == 51 && c == 100)
                && !(a == 203 && b == 0 && c == 113);
        }
        if (address.AddressFamily != AddressFamily.InterNetworkV6 || address.ScopeId != 0) return false;
        return (bytes[0] & 0xe0) == 0x20
            && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] < 2)
            && !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8)
            && !(bytes[0] == 0x20 && bytes[1] == 0x02)
            && !(bytes[0] == 0x3f && bytes[1] == 0xff && (bytes[2] & 0xf0) == 0);
    }

    /// <summary>Accepts only credential-free HTTPS destinations on the public default port.</summary>
    public static Uri ValidateUri(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2048 || value.Any(char.IsControl)
            || !Uri.TryCreate(value, UriKind.Absolute, out Uri uri)
            || uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException();
        string host = uri.IdnHost.TrimEnd('.');
        if (IPAddress.TryParse(host.Trim('[', ']'), out IPAddress address))
        {
            if (!IsPublic(address)) throw new InvalidOperationException();
        }
        else if (!host.Contains('.') || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".home.arpa", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException();
        foreach (string part in uri.Query.TrimStart('?').Split('&', ';'))
        {
            string field = Uri.UnescapeDataString(part.Split('=')[0]).Replace("+", "");
            string normalized = new string(field.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            if (normalized.Contains("password") || normalized.Contains("apikey") || normalized.Contains("authorization")
                || normalized.Contains("secret") || normalized.EndsWith("token") || normalized.EndsWith("key")
                || normalized == "sig" || normalized == "signature" || normalized == "credential")
                throw new InvalidOperationException();
        }
        return uri;
    }

    /// <summary>Resolves and validates every answer, then connects directly to an inspected address to prevent rebinding.</summary>
    private static async ValueTask<Stream> ConnectPublicAsync(SocketsHttpConnectionContext context, CancellationToken cancellation)
    {
        string host = context.DnsEndPoint.Host.Trim('[', ']');
        IPAddress[] addresses = IPAddress.TryParse(host, out IPAddress literal)
            ? new[] { literal } : await Dns.GetHostAddressesAsync(host, cancellation).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Length > 16 || addresses.Any(address => !IsPublic(address)))
            throw new InvalidOperationException();
        foreach (IPAddress address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, 443), cancellation).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException) { socket.Dispose(); }
            catch { socket.Dispose(); throw; }
        }
        throw new InvalidOperationException();
    }

    /// <summary>Performs one bounded request without proxies, redirects, cookies, authentication, or decompression.</summary>
    public static async Task<Response> FetchAsync(string url, string method, string body, string contentType, int limit, bool requireComplete)
    {
        Uri uri = ValidateUri(url);
        method = (method ?? "").ToUpperInvariant();
        body = body ?? "";
        if ((method != "GET" && method != "HEAD" && method != "POST" && method != "PUT")
            || Encoding.UTF8.GetByteCount(body) > 4096 || limit < 1 || limit > 131072
            || ((method == "GET" || method == "HEAD") && body.Length != 0)
            || (contentType != "application/json" && contentType != "text/plain"))
            throw new InvalidOperationException();
        if ((method == "POST" || method == "PUT") && contentType == "application/json")
        {
            using (JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 16 })) { }
        }
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var handler = new SocketsHttpHandler
        {
            UseProxy = false, AllowAutoRedirect = false, UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None, MaxResponseHeadersLength = 16,
            ConnectTimeout = TimeSpan.FromSeconds(10), MaxConnectionsPerServer = 1, MaxResponseDrainSize = 0,
            ConnectCallback = ConnectPublicAsync
        };
        using var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        request.Headers.UserAgent.ParseAdd("PromptMeUp/1.0");
        request.Headers.AcceptEncoding.ParseAdd("identity");
        if (method == "POST" || method == "PUT") request.Content = new StringContent(body, Encoding.UTF8, contentType);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token).ConfigureAwait(false);
        string mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
        bool textual = mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("javascript", StringComparison.OrdinalIgnoreCase);
        var result = new Response { StatusCode = (int)response.StatusCode, MediaType = mediaType, Body = "" };
        if (method == "HEAD") return result;
        if (!textual || response.Content.Headers.ContentEncoding.Any(value => !value.Equals("identity", StringComparison.OrdinalIgnoreCase)))
        {
            if (requireComplete) throw new InvalidOperationException();
            result.Omitted = true;
            return result;
        }
        using Stream stream = await response.Content.ReadAsStreamAsync(cancellation.Token).ConfigureAwait(false);
        byte[] bytes = new byte[limit + 1];
        int total = 0;
        while (total < bytes.Length)
        {
            int count = await stream.ReadAsync(bytes.AsMemory(total), cancellation.Token).ConfigureAwait(false);
            if (count == 0) break;
            total += count;
        }
        result.Truncated = total > limit;
        if (requireComplete && result.Truncated) throw new InvalidOperationException();
        result.Body = Encoding.UTF8.GetString(bytes, 0, Math.Min(total, limit));
        return result;
    }

    public sealed class SearchItem
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public sealed class SearchResponse
    {
        public string Provider { get; set; } = "DuckDuckGo Instant Answer";
        public SearchItem[] Results { get; set; }
    }

    /// <summary>Returns bounded instant answers and related topics without fetching result links.</summary>
    public static async Task<SearchResponse> SearchAsync(string query, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 300 || query.Any(char.IsControl)
            || maxResults < 1 || maxResults > 10) throw new InvalidOperationException();
        string url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query)
            + "&format=json&no_html=1&skip_disambig=1";
        Response response = await FetchAsync(url, "GET", "", "application/json", 131072, true).ConfigureAwait(false);
        if (response.StatusCode != 200) throw new InvalidOperationException();
        using JsonDocument document = JsonDocument.Parse(response.Body, new JsonDocumentOptions { MaxDepth = 16 });
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException();
        var results = new System.Collections.Generic.List<SearchItem>();
        AddResult(results, ReadString(root, "AbstractText"), ReadString(root, "AbstractURL"), maxResults);
        AddResult(results, ReadString(root, "Answer"), "", maxResults);
        var queue = new System.Collections.Generic.Queue<JsonElement>();
        if (root.TryGetProperty("RelatedTopics", out JsonElement topics) && topics.ValueKind == JsonValueKind.Array)
            foreach (JsonElement topic in topics.EnumerateArray())
                if (queue.Count < 200) queue.Enqueue(topic);
        int inspected = 0;
        while (queue.Count > 0 && results.Count < maxResults && inspected++ < 200)
        {
            JsonElement topic = queue.Dequeue();
            if (topic.ValueKind != JsonValueKind.Object) continue;
            AddResult(results, ReadString(topic, "Text"), ReadString(topic, "FirstURL"), maxResults);
            if (topic.TryGetProperty("Topics", out JsonElement children) && children.ValueKind == JsonValueKind.Array)
                foreach (JsonElement child in children.EnumerateArray())
                    if (queue.Count < 200) queue.Enqueue(child);
        }
        return new SearchResponse { Results = results.ToArray() };
    }

    /// <summary>Reads optional provider fields without accepting nested executable or markup data as instructions.</summary>
    private static string ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";
    }

    /// <summary>Keeps result text short and only displays ordinary HTTPS links without visiting them.</summary>
    private static void AddResult(System.Collections.Generic.List<SearchItem> results, string text, string url, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(text) || results.Count >= maxResults) return;
        text = new string(text.Where(character => !char.IsControl(character)).Take(220).ToArray());
        if (text.Length == 0 || results.Any(item => item.Text == text)) return;
        if (url.Length > 200 || url.Any(char.IsControl) || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo)) url = "";
        results.Add(new SearchItem { Text = text, Url = url });
    }

}
'@
    if (-not ('PromptMeUpWebSearchSkill' -as [type])) { Add-Type -TypeDefinition $networkSource -ErrorAction Stop }
    [PromptMeUpWebSearchSkill]::SearchAsync($Query, $MaxResults).GetAwaiter().GetResult() | ConvertTo-Json -Depth 5
}
catch { throw $_ValidationError }
