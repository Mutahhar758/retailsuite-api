using System.Text.RegularExpressions;
using QRCoder;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public static class QrCodeHelper
{
    public static byte[] GeneratePng(
        string payload,
        int pixelSize = 5,
        QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return Array.Empty<byte>();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, eccLevel);
        using var pngCode = new PngByteQRCode(data);
        return pngCode.GetGraphic(pixelSize);
    }
}

public class QrPaymentInfo
{
    public string AccountTitle { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }

    private static readonly Dictionary<string, string> BankCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        { "meezan", "MEZN" },
        { "hbl", "HABB" },
        { "habib", "HABB" },
        { "ubl", "UNIL" },
        { "united", "UNIL" },
        { "mcb", "MUCB" },
        { "alfalah", "ALFH" },
        { "allied", "ABPA" },
        { "abl", "ABPA" },
        { "askari", "ASCM" },
        { "faysal", "FAYS" },
        { "bop", "BPUN" },
        { "punjab", "BPUN" },
        { "bankislami", "BKIP" },
        { "islami", "BKIP" },
        { "soneri", "SONE" },
        { "scb", "SCBL" },
        { "standard", "SCBL" },
        { "easypaisa", "TMFB" },
        { "telenor", "TMFB" },
        { "jazzcash", "MMBL" },
        { "mobilink", "MMBL" },
        { "sadapay", "SADA" },
        { "nayapay", "NPAY" },
        { "js", "JSBL" },
        { "albaraka", "BARK" },
        { "habibmetro", "HMBP" },
        { "dubai", "DIBP" }
    };

    public static string NormalizeToIban(string input, string bankName)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string clean = Regex.Replace(input, @"[^A-Za-z0-9]", string.Empty).ToUpperInvariant();
        if (clean.Length == 24 && clean.StartsWith("PK", StringComparison.OrdinalIgnoreCase))
            return clean;
        if (clean.StartsWith("PK", StringComparison.OrdinalIgnoreCase))
            return clean;

        string? bankCode = null;
        if (!string.IsNullOrWhiteSpace(bankName))
        {
            string bLower = Regex.Replace(bankName, @"[^A-Za-z0-9]", string.Empty).ToLowerInvariant();
            foreach (var kvp in BankCodes)
            {
                if (bLower.Contains(kvp.Key))
                {
                    bankCode = kvp.Value;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(bankCode))
            return clean;

        string account16 = clean.Length > 16 ? clean.Substring(clean.Length - 16) : clean.PadLeft(16, '0');

        string numStr = string.Empty;
        foreach (char c in bankCode)
        {
            numStr += (c - 55).ToString();
        }
        numStr += account16 + "252000";

        int remainder = 0;
        for (int i = 0; i < numStr.Length; i++)
        {
            remainder = (remainder * 10 + (numStr[i] - '0')) % 97;
        }
        int check = 98 - remainder;

        return "PK" + check.ToString("D2") + bankCode + account16;
    }

    public static string FormatIban(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return string.Empty;
        string clean = Regex.Replace(iban, @"[^A-Za-z0-9]", string.Empty).ToUpperInvariant();
        return Regex.Replace(clean, ".{4}", "$0 ").Trim();
    }

    public string BuildEmvCoPayload(decimal amount)
    {
        string iban = NormalizeToIban(AccountNumber, BankName);
        bool hasAmount = amount > 0;
        string poi = hasAmount ? "12" : "11";

        string payload =
            Tlv("00", "02") +
            Tlv("01", poi) +
            Tlv("02", "00") +
            Tlv("04", iban);

        if (hasAmount)
        {
            decimal rounded = Math.Round(amount, MidpointRounding.AwayFromZero);
            string amtStr = ((long)rounded).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
            payload += Tlv("05", amtStr);
            payload += Tlv("07", DateTime.Now.AddDays(30).ToString("ddMMyyyy") + "2359");
        }

        payload += "1004";
        return payload + Crc16Hex(payload);
    }

    private static string Tlv(string tag, string value)
    {
        return tag + value.Length.ToString("D2") + value;
    }

    public static string Crc16Hex(string data)
    {
        ushort crc = 0xFFFF;
        foreach (char c in data)
        {
            crc ^= (ushort)(c << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            }
        }
        return crc.ToString("X4");
    }
}
