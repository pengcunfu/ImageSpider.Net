namespace ImageSpider.Core.Utilities;

public static class UrlHelper
{
    public static bool IsHttpUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static string? FirstHttpUrl(params string?[] candidates)
    {
        foreach (var url in candidates)
        {
            if (IsHttpUrl(url))
                return url;
        }
        return null;
    }
}
