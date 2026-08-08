using System.Text.RegularExpressions;

namespace ImageSpider.Infrastructure.Baidu;

/// <summary>
/// 解密百度图片 JSON 中的 objURL / fromURL 等加密地址。
/// 参考: https://www.cnblogs.com/wgscd/p/8630506.html
/// </summary>
public static class BaiduUrlDecoder
{
    private static readonly string[] MultiCharTokens = ["_z2C$q", "_z&e3B", "AzdH3F"];

    private static readonly Dictionary<string, string> CharMap = new()
    {
        ["w"] = "a", ["k"] = "b", ["v"] = "c", ["1"] = "d", ["j"] = "e", ["u"] = "f", ["2"] = "g",
        ["i"] = "h", ["t"] = "i", ["3"] = "j", ["h"] = "k", ["s"] = "l", ["4"] = "m", ["g"] = "n",
        ["5"] = "o", ["r"] = "p", ["q"] = "q", ["6"] = "r", ["f"] = "s", ["p"] = "t", ["7"] = "u",
        ["e"] = "v", ["o"] = "w", ["8"] = "1", ["d"] = "2", ["n"] = "3", ["9"] = "4", ["c"] = "5",
        ["m"] = "6", ["0"] = "7", ["b"] = "8", ["l"] = "9", ["a"] = "0",
        ["_z2C$q"] = ":", ["_z&e3B"] = ".", ["AzdH3F"] = "/"
    };

    private static readonly Regex EncodedCharPattern = new("^[a-w\\d]+$", RegexOptions.Compiled);

    public static string? Decode(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        url = url.Replace("\\/", "/").Trim();

        if (url.StartsWith("ippr_", StringComparison.OrdinalIgnoreCase))
            url = url[5..];
        else if (url.StartsWith("hjpj_", StringComparison.OrdinalIgnoreCase))
            url = url[5..];

        if (url.Contains("http://", StringComparison.OrdinalIgnoreCase)
            || url.Contains("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (!url.StartsWith("ippr", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("hjpj", StringComparison.OrdinalIgnoreCase)
            && !url.Contains("_z2C$q", StringComparison.Ordinal)
            && !url.Contains("AzdH3F", StringComparison.Ordinal))
        {
            return url;
        }

        var decoded = url;
        foreach (var token in MultiCharTokens)
        {
            if (CharMap.TryGetValue(token, out var replacement))
                decoded = decoded.Replace(token, replacement, StringComparison.Ordinal);
        }

        var chars = decoded.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var key = chars[i].ToString();
            if (CharMap.TryGetValue(key, out var mapped) && EncodedCharPattern.IsMatch(key))
                chars[i] = mapped[0];
        }

        decoded = new string(chars);

        if (decoded.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || decoded.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return decoded;

        var httpIndex = decoded.IndexOf("http", StringComparison.OrdinalIgnoreCase);
        if (httpIndex >= 0)
            return decoded[httpIndex..];

        return null;
    }

    public static bool IsHttpUrl(string? url) => Core.Utilities.UrlHelper.IsHttpUrl(url);
}
