using System.Text;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

/// <summary>
/// Code 128 (Subset B) vector barcode generator.
/// Produces ISO/IEC 15417 compliant SVG barcodes for ultra-crisp thermal and vector PDF printing.
/// </summary>
public static class BarcodeGenerator
{
    private static readonly byte[][] Patterns = new byte[][]
    {
        new byte[] { 2, 1, 2, 2, 2, 2 }, // 0
        new byte[] { 2, 2, 2, 1, 2, 2 }, // 1
        new byte[] { 2, 2, 2, 2, 2, 1 }, // 2
        new byte[] { 1, 2, 1, 2, 2, 3 }, // 3
        new byte[] { 1, 2, 1, 3, 2, 2 }, // 4
        new byte[] { 1, 3, 1, 2, 2, 2 }, // 5
        new byte[] { 1, 2, 2, 2, 1, 3 }, // 6
        new byte[] { 1, 2, 2, 3, 1, 2 }, // 7
        new byte[] { 1, 3, 2, 2, 1, 2 }, // 8
        new byte[] { 2, 2, 1, 2, 1, 3 }, // 9
        new byte[] { 2, 2, 1, 3, 1, 2 }, // 10
        new byte[] { 2, 3, 1, 2, 1, 2 }, // 11
        new byte[] { 1, 1, 2, 2, 3, 2 }, // 12
        new byte[] { 1, 2, 2, 1, 3, 2 }, // 13
        new byte[] { 1, 2, 2, 2, 3, 1 }, // 14
        new byte[] { 1, 1, 3, 2, 2, 2 }, // 15
        new byte[] { 1, 2, 3, 1, 2, 2 }, // 16
        new byte[] { 1, 2, 3, 2, 2, 1 }, // 17
        new byte[] { 2, 2, 3, 2, 1, 1 }, // 18
        new byte[] { 2, 2, 1, 1, 3, 2 }, // 19
        new byte[] { 2, 2, 1, 2, 3, 1 }, // 20
        new byte[] { 2, 1, 3, 2, 1, 2 }, // 21
        new byte[] { 2, 2, 3, 1, 1, 2 }, // 22
        new byte[] { 3, 1, 2, 1, 3, 1 }, // 23
        new byte[] { 3, 1, 1, 2, 2, 2 }, // 24
        new byte[] { 3, 2, 1, 1, 2, 2 }, // 25
        new byte[] { 3, 2, 1, 2, 2, 1 }, // 26
        new byte[] { 3, 1, 2, 2, 1, 2 }, // 27
        new byte[] { 3, 2, 2, 1, 1, 2 }, // 28
        new byte[] { 3, 2, 2, 2, 1, 1 }, // 29
        new byte[] { 2, 1, 2, 1, 2, 3 }, // 30
        new byte[] { 2, 1, 2, 3, 2, 1 }, // 31
        new byte[] { 2, 3, 2, 1, 2, 1 }, // 32
        new byte[] { 1, 1, 1, 3, 2, 3 }, // 33
        new byte[] { 1, 3, 1, 1, 2, 3 }, // 34
        new byte[] { 1, 3, 1, 3, 2, 1 }, // 35
        new byte[] { 1, 1, 2, 3, 1, 3 }, // 36
        new byte[] { 1, 3, 2, 1, 1, 3 }, // 37
        new byte[] { 1, 3, 2, 3, 1, 1 }, // 38
        new byte[] { 2, 1, 1, 3, 1, 3 }, // 39
        new byte[] { 2, 3, 1, 1, 1, 3 }, // 40
        new byte[] { 2, 3, 1, 3, 1, 1 }, // 41
        new byte[] { 1, 1, 2, 1, 3, 3 }, // 42
        new byte[] { 1, 1, 2, 3, 3, 1 }, // 43
        new byte[] { 1, 3, 2, 1, 3, 1 }, // 44
        new byte[] { 1, 1, 3, 1, 2, 3 }, // 45
        new byte[] { 1, 1, 3, 3, 2, 1 }, // 46
        new byte[] { 1, 3, 3, 1, 2, 1 }, // 47
        new byte[] { 3, 1, 3, 1, 2, 1 }, // 48
        new byte[] { 2, 1, 1, 3, 3, 1 }, // 49
        new byte[] { 2, 3, 1, 1, 3, 1 }, // 50
        new byte[] { 2, 1, 3, 1, 1, 3 }, // 51
        new byte[] { 2, 1, 3, 3, 1, 1 }, // 52
        new byte[] { 2, 1, 3, 1, 3, 1 }, // 53
        new byte[] { 3, 1, 1, 1, 2, 3 }, // 54
        new byte[] { 3, 1, 1, 3, 2, 1 }, // 55
        new byte[] { 3, 3, 1, 1, 2, 1 }, // 56
        new byte[] { 3, 1, 2, 1, 1, 3 }, // 57
        new byte[] { 3, 1, 2, 3, 1, 1 }, // 58
        new byte[] { 3, 3, 2, 1, 1, 1 }, // 59
        new byte[] { 3, 1, 4, 1, 1, 1 }, // 60
        new byte[] { 2, 2, 1, 4, 1, 1 }, // 61
        new byte[] { 4, 3, 1, 1, 1, 1 }, // 62
        new byte[] { 1, 1, 1, 2, 2, 4 }, // 63
        new byte[] { 1, 1, 1, 4, 2, 2 }, // 64
        new byte[] { 1, 2, 1, 1, 2, 4 }, // 65
        new byte[] { 1, 2, 1, 4, 2, 1 }, // 66
        new byte[] { 1, 4, 1, 1, 2, 2 }, // 67
        new byte[] { 1, 4, 1, 2, 2, 1 }, // 68
        new byte[] { 1, 1, 2, 2, 1, 4 }, // 69
        new byte[] { 1, 1, 2, 4, 1, 2 }, // 70
        new byte[] { 1, 2, 2, 1, 1, 4 }, // 71
        new byte[] { 1, 2, 2, 4, 1, 1 }, // 72
        new byte[] { 1, 4, 2, 1, 1, 2 }, // 73
        new byte[] { 1, 4, 2, 2, 1, 1 }, // 74
        new byte[] { 2, 4, 1, 2, 1, 1 }, // 75
        new byte[] { 2, 2, 1, 1, 1, 4 }, // 76
        new byte[] { 4, 1, 3, 1, 1, 1 }, // 77
        new byte[] { 2, 4, 1, 1, 1, 2 }, // 78
        new byte[] { 1, 3, 4, 1, 1, 1 }, // 79
        new byte[] { 1, 1, 1, 2, 4, 2 }, // 80
        new byte[] { 1, 2, 1, 1, 4, 2 }, // 81
        new byte[] { 1, 2, 1, 2, 4, 1 }, // 82
        new byte[] { 1, 1, 4, 2, 1, 2 }, // 83
        new byte[] { 1, 2, 4, 1, 1, 2 }, // 84
        new byte[] { 1, 2, 4, 2, 1, 1 }, // 85
        new byte[] { 4, 1, 1, 2, 1, 2 }, // 86
        new byte[] { 4, 2, 1, 1, 1, 2 }, // 87
        new byte[] { 4, 2, 1, 2, 1, 1 }, // 88
        new byte[] { 2, 1, 2, 1, 4, 1 }, // 89
        new byte[] { 2, 1, 4, 1, 2, 1 }, // 90
        new byte[] { 4, 1, 2, 1, 2, 1 }, // 91
        new byte[] { 1, 1, 1, 1, 4, 3 }, // 92
        new byte[] { 1, 1, 1, 3, 4, 1 }, // 93
        new byte[] { 1, 3, 1, 1, 4, 1 }, // 94
        new byte[] { 1, 1, 4, 1, 1, 3 }, // 95
        new byte[] { 1, 1, 4, 3, 1, 1 }, // 96
        new byte[] { 4, 1, 1, 1, 1, 3 }, // 97
        new byte[] { 4, 1, 1, 3, 1, 1 }, // 98
        new byte[] { 1, 1, 3, 1, 4, 1 }, // 99
        new byte[] { 1, 1, 4, 1, 3, 1 }, // 100
        new byte[] { 3, 1, 1, 1, 4, 1 }, // 101
        new byte[] { 4, 1, 1, 1, 3, 1 }, // 102
        new byte[] { 2, 1, 1, 4, 1, 2 }, // 103 (Start A)
        new byte[] { 2, 1, 1, 2, 1, 4 }, // 104 (Start B)
        new byte[] { 2, 1, 1, 2, 3, 2 }, // 105 (Start C)
        new byte[] { 2, 3, 3, 1, 1, 1, 2 } // 106 (Stop)
    };

    public readonly record struct BarcodeSlice(int Width, bool IsBlack);

    /// <summary>
    /// Generates the sequence of alternating black bars and white spaces for Code 128 (Subset B).
    /// Used for direct vector PDF rendering without relying on SVG aspect-ratio scaling.
    /// </summary>
    public static List<BarcodeSlice> GenerateSlices(string text, int quietZone = 4)
    {
        if (string.IsNullOrWhiteSpace(text)) text = "1000001";

        var encodedValues = new List<int>
        {
            104 // Start B
        };

        int checkSum = 104;
        for (int i = 0; i < text.Length; i++)
        {
            int code = text[i] - 32;
            if (code < 0 || code > 95) code = 0; // Default to space if unencodable in Subset B
            encodedValues.Add(code);
            checkSum += code * (i + 1);
        }

        encodedValues.Add(checkSum % 103);
        encodedValues.Add(106); // Stop

        var slices = new List<BarcodeSlice>();
        if (quietZone > 0)
        {
            slices.Add(new BarcodeSlice(quietZone, false));
        }

        foreach (var val in encodedValues)
        {
            var pattern = Patterns[val];
            bool isBar = true;

            for (int p = 0; p < pattern.Length; p++)
            {
                slices.Add(new BarcodeSlice(pattern[p], isBar));
                isBar = !isBar;
            }
        }

        if (quietZone > 0)
        {
            slices.Add(new BarcodeSlice(quietZone, false));
        }

        return slices;
    }

    /// <summary>
    /// Generates a scalable vector SVG string representing a Code 128 barcode.
    /// </summary>
    public static string GenerateSvg(string text, int barHeight = 46, int unitWidth = 2, int quietZone = 10)
    {
        if (string.IsNullOrWhiteSpace(text)) text = "1000001";

        var encodedValues = new List<int>
        {
            104 // Start B
        };

        int checkSum = 104;
        for (int i = 0; i < text.Length; i++)
        {
            int code = text[i] - 32;
            if (code < 0 || code > 95) code = 0; // Default to space if unencodable in Subset B
            encodedValues.Add(code);
            checkSum += code * (i + 1);
        }

        encodedValues.Add(checkSum % 103);
        encodedValues.Add(106); // Stop

        var svg = new StringBuilder();
        int currentX = quietZone;

        var rects = new StringBuilder();

        foreach (var val in encodedValues)
        {
            var pattern = Patterns[val];
            bool isBar = true;

            for (int p = 0; p < pattern.Length; p++)
            {
                int width = pattern[p] * unitWidth;
                if (isBar)
                {
                    rects.Append($"<rect x=\"{currentX}\" y=\"0\" width=\"{width}\" height=\"{barHeight}\" fill=\"black\" />");
                }

                currentX += width;
                isBar = !isBar;
            }
        }

        int totalWidth = currentX + quietZone;
        svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalWidth} {barHeight}\" width=\"100%\" height=\"100%\" preserveAspectRatio=\"xMidYMid meet\">");
        svg.Append(rects);
        svg.Append("</svg>");

        return svg.ToString();
    }
}
