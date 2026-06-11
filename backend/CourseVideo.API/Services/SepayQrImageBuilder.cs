using System.Net;
using System.Text;
using CourseVideo.API.Models;

namespace CourseVideo.API.Services;

public static class SepayQrImageBuilder
{
    private static readonly IReadOnlyDictionary<string, string> BankAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["VCB"] = "VCB",
        ["VIETCOMBANK"] = "VCB",
        ["BIDV"] = "BIDV",
        ["VIETINBANK"] = "ICB",
        ["ICB"] = "ICB",
        ["AGRIBANK"] = "VBA",
        ["VBA"] = "VBA",
        ["ACB"] = "ACB",
        ["TECHCOMBANK"] = "TCB",
        ["TCB"] = "TCB",
        ["MBBANK"] = "MB",
        ["MB"] = "MB",
        ["TPBANK"] = "TPB",
        ["TPBANKDIGI"] = "TPB",
        ["TPB"] = "TPB",
        ["VPBANK"] = "VPB",
        ["VPB"] = "VPB",
        ["SACOMBANK"] = "STB",
        ["STB"] = "STB",
        ["HDBANK"] = "HDB",
        ["HDB"] = "HDB",
        ["OCB"] = "OCB",
        ["MSB"] = "MSB",
        ["VIB"] = "VIB",
        ["EXIMBANK"] = "EIB",
        ["EIB"] = "EIB",
        ["SHB"] = "SHB",
        ["SEABANK"] = "SEAB",
        ["SEAB"] = "SEAB",
        ["LPBANK"] = "LPB",
        ["LIENVIETPOSTBANK"] = "LPB",
        ["LPB"] = "LPB",
        ["KIENLONGBANK"] = "KLB",
        ["KLB"] = "KLB",
        ["NAMABANK"] = "NAB",
        ["NAB"] = "NAB",
        ["PGBANK"] = "PGB",
        ["PGB"] = "PGB",
        ["PVCOMBANK"] = "PVCB",
        ["PVCB"] = "PVCB",
        ["ABBANK"] = "ABB",
        ["ABB"] = "ABB",
        ["BACABANK"] = "BAB",
        ["BAB"] = "BAB",
        ["VIETBANK"] = "VBB",
        ["VBB"] = "VBB"
    };

    public static string Build(PaymentOrder order, string storeName, string environment)
    {
        if (UsesUnsupportedSandboxBank(order, environment))
        {
            return BuildSandboxFallbackSvg(order, storeName);
        }

        var qrBank = ResolveQrBankParameter(order);
        if (string.IsNullOrWhiteSpace(qrBank))
        {
            return BuildUnsupportedBankFallbackSvg(order, storeName);
        }

        var query = new Dictionary<string, string?>
        {
            ["acc"] = order.BankAccountNumber,
            ["bank"] = qrBank,
            ["amount"] = order.Amount.ToString(),
            ["des"] = order.TransferContent,
            ["template"] = "compact",
            ["showinfo"] = "true",
            ["fullacc"] = "true",
            ["holder"] = RemoveDiacritics(order.AccountHolderName ?? string.Empty),
            ["store"] = RemoveDiacritics(storeName)
        };

        var encoded = string.Join("&",
            query
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value!)}"));

        return $"https://qr.sepay.vn/img?{encoded}";
    }

    internal static bool UsesUnsupportedSandboxBank(PaymentOrder order, string environment)
    {
        if (!string.Equals(environment, "Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var bankCode = order.BankCode?.Trim() ?? string.Empty;
        var bankName = order.BankName?.Trim() ?? string.Empty;

        return bankCode.StartsWith("ACME", StringComparison.OrdinalIgnoreCase)
            || bankName.Contains("giả lập", StringComparison.OrdinalIgnoreCase)
            || bankName.Contains("gia lap", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSandboxFallbackSvg(PaymentOrder order, string storeName)
    {
        var svg = $"""
<svg xmlns="http://www.w3.org/2000/svg" width="720" height="720" viewBox="0 0 720 720" role="img" aria-labelledby="title desc">
  <title id="title">SePay Sandbox QR fallback</title>
  <desc id="desc">Sandbox mock bank is not supported by qr.sepay.vn, so this fallback image shows the transfer details instead of a broken image.</desc>
  <defs>
    <linearGradient id="bg" x1="0%" x2="100%" y1="0%" y2="100%">
      <stop offset="0%" stop-color="#111827" />
      <stop offset="100%" stop-color="#1f2937" />
    </linearGradient>
  </defs>
  <rect width="720" height="720" rx="32" fill="url(#bg)" />
  <rect x="44" y="44" width="632" height="632" rx="24" fill="#ffffff" />
  <text x="360" y="120" text-anchor="middle" font-family="Arial, sans-serif" font-size="34" font-weight="700" fill="#0f172a">SePay Sandbox</text>
  <text x="360" y="168" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#334155">Ngân hàng giả lập không hỗ trợ ảnh QR trực tiếp</text>
  <rect x="108" y="220" width="504" height="220" rx="20" fill="#f8fafc" stroke="#cbd5e1" stroke-width="2" />
  <text x="130" y="270" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#0f172a">Số tiền</text>
  <text x="130" y="310" font-family="Arial, sans-serif" font-size="30" font-weight="700" fill="#16a34a">{WebUtility.HtmlEncode(order.Amount.ToString("N0"))} VND</text>
  <text x="130" y="360" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#0f172a">Nội dung CK</text>
  <text x="130" y="400" font-family="Arial, sans-serif" font-size="28" font-weight="700" fill="#2563eb">{WebUtility.HtmlEncode(order.TransferContent)}</text>
  <text x="130" y="478" font-family="Arial, sans-serif" font-size="22" fill="#334155">TK: {WebUtility.HtmlEncode(order.BankAccountNumber ?? string.Empty)}</text>
  <text x="130" y="514" font-family="Arial, sans-serif" font-size="22" fill="#334155">Chủ TK: {WebUtility.HtmlEncode(RemoveDiacritics(order.AccountHolderName ?? string.Empty))}</text>
  <text x="360" y="584" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#334155">Dùng SePay Test mode de mo phong giao dich va webhook</text>
  <text x="360" y="620" text-anchor="middle" font-family="Arial, sans-serif" font-size="20" fill="#64748b">{WebUtility.HtmlEncode(RemoveDiacritics(storeName))}</text>
</svg>
""";

        return $"data:image/svg+xml;charset=utf-8,{Uri.EscapeDataString(svg)}";
    }

    private static string BuildUnsupportedBankFallbackSvg(PaymentOrder order, string storeName)
    {
        var svg = $"""
<svg xmlns="http://www.w3.org/2000/svg" width="720" height="720" viewBox="0 0 720 720" role="img" aria-labelledby="title desc">
  <title id="title">QR bank mapping fallback</title>
  <desc id="desc">The configured bank code could not be normalized into a SePay QR-compatible bank code, so this fallback image shows the transfer details.</desc>
  <defs>
    <linearGradient id="bg" x1="0%" x2="100%" y1="0%" y2="100%">
      <stop offset="0%" stop-color="#111827" />
      <stop offset="100%" stop-color="#1f2937" />
    </linearGradient>
  </defs>
  <rect width="720" height="720" rx="32" fill="url(#bg)" />
  <rect x="44" y="44" width="632" height="632" rx="24" fill="#ffffff" />
  <text x="360" y="120" text-anchor="middle" font-family="Arial, sans-serif" font-size="34" font-weight="700" fill="#0f172a">QR tam thoi chua tao duoc</text>
  <text x="360" y="168" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#334155">Thong tin chuyen khoan van hop le, vui long chuyen thu cong</text>
  <rect x="108" y="220" width="504" height="260" rx="20" fill="#f8fafc" stroke="#cbd5e1" stroke-width="2" />
  <text x="130" y="270" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#0f172a">Ngan hang</text>
  <text x="130" y="310" font-family="Arial, sans-serif" font-size="28" font-weight="700" fill="#2563eb">{WebUtility.HtmlEncode(RemoveDiacritics(order.BankName ?? order.BankCode ?? string.Empty))}</text>
  <text x="130" y="360" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#0f172a">So tai khoan</text>
  <text x="130" y="400" font-family="Arial, sans-serif" font-size="28" font-weight="700" fill="#2563eb">{WebUtility.HtmlEncode(order.BankAccountNumber ?? string.Empty)}</text>
  <text x="130" y="450" font-family="Arial, sans-serif" font-size="24" font-weight="700" fill="#0f172a">Noi dung CK</text>
  <text x="130" y="490" font-family="Arial, sans-serif" font-size="28" font-weight="700" fill="#16a34a">{WebUtility.HtmlEncode(order.TransferContent)}</text>
  <text x="360" y="584" text-anchor="middle" font-family="Arial, sans-serif" font-size="22" fill="#334155">Cap nhat ma ngan hang nhu VCB, TPB, MB trong Admin Profile de hien QR</text>
  <text x="360" y="620" text-anchor="middle" font-family="Arial, sans-serif" font-size="20" fill="#64748b">{WebUtility.HtmlEncode(RemoveDiacritics(storeName))}</text>
</svg>
""";

        return $"data:image/svg+xml;charset=utf-8,{Uri.EscapeDataString(svg)}";
    }

    private static string ResolveQrBankParameter(PaymentOrder order)
    {
        var candidates = new[]
        {
            order.BankCode,
            order.BankName
        };

        foreach (var candidate in candidates)
        {
            var normalized = NormalizeBankAlias(candidate);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (BankAliases.TryGetValue(normalized, out var mapped))
            {
                return mapped;
            }

            if (normalized.All(char.IsLetterOrDigit))
            {
                return normalized;
            }
        }

        return string.Empty;
    }

    private static string NormalizeBankAlias(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var ascii = RemoveDiacritics(value).ToUpperInvariant();
        var builder = new StringBuilder(ascii.Length);

        foreach (var character in ascii)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        return string.Concat(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark))
            .Normalize(System.Text.NormalizationForm.FormC);
    }
}
