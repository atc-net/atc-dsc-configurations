namespace Atc.Dsc.Configurations.Cli.Helpers;

/// <summary>
/// Prints a colored ASCII art startup banner to the console, ported from atc-opc-ua.
/// </summary>
internal static class StartupBanner
{
    private const string Blue = "\e[38;2;83;162;218m";
    private const string DarkPurple = "\e[38;2;75;65;151m";
    private const string LightPurple = "\e[38;2;171;116;181m";

    private const string Dim = "\e[90m";
    private const string BrightWhite = "\e[97m";
    private const string Reset = "\e[0m";

    private const int LogoCharsWide = 20;
    private const int LogoLinesHigh = 10;
    private const int DotsWide = LogoCharsWide * 2;
    private const int DotsTall = LogoLinesHigh * 4;
    private const double SvgWidth = 109.035;
    private const double SvgHeight = 110.737;

    private const double LeftYMin = 27.165;
    private const double LeftYMax = 110.737;
    private const double RightYMin = 0;
    private const double RightYMax = 83.84;

    private static readonly (int R, int G, int B) LeftGradientFrom = (173, 214, 242);
    private static readonly (int R, int G, int B) LeftGradientTo = (83, 162, 218);

    private static readonly (int R, int G, int B) RightGradientFrom = (75, 65, 151);
    private static readonly (int R, int G, int B) RightGradientTo = (171, 116, 181);

    private static readonly (double X, double Y)[] LeftUpper =
    [
        (0, 55.544), (49.492, 27.165), (49.492, 54.238), (0, 82.617),
    ];

    private static readonly (double X, double Y)[] LeftLower =
    [
        (49.492, 83.664), (0, 55.285), (0, 82.358), (49.492, 110.737),
    ];

    private static readonly (double X, double Y)[] RightLower =
    [
        (109.035, 28.388), (59.543, 56.767), (59.543, 83.84), (109.035, 55.461),
    ];

    private static readonly (double X, double Y)[] RightUpper =
    [
        (59.543, 0), (109.035, 28.379), (109.035, 55.452), (59.543, 27.073),
    ];

    private static readonly (double X, double Y)[][] AllShapes = [LeftUpper, LeftLower, RightLower, RightUpper];

    /// <summary>
    /// Writes the ATC DSC startup banner with ANSI colors.
    /// </summary>
    /// <param name="version">The application version string.</param>
    internal static void Print(string version)
    {
        var label = BuildGradientText("atc-dsc", (75, 65, 151), (83, 162, 218));
        var logoLines = BuildBrailleLogo();

        string[] atcLines =
        [
            $"{Blue} █████╗ {LightPurple}████████╗ {DarkPurple}██████╗{Reset}",
            $"{Blue}██╔══██╗{LightPurple}╚══██╔══╝{DarkPurple}██╔════╝{Reset}",
            $"{Blue}███████║{LightPurple}   ██║   {DarkPurple}██║{Reset}",
            $"{Blue}██╔══██║{LightPurple}   ██║   {DarkPurple}██║{Reset}",
            $"{Blue}██║  ██║{LightPurple}   ██║   {DarkPurple}╚██████╗{Reset}",
            $"{Blue}╚═╝  ╚═╝{LightPurple}   ╚═╝    {DarkPurple}╚═════╝  {label}{Reset}",
        ];

        System.Console.WriteLine();

        const int atcStartLine = 2;
        for (var i = 0; i < logoLines.Length; i++)
        {
            var atcIndex = i - atcStartLine;
            var atcPart = atcIndex >= 0 && atcIndex < atcLines.Length
                ? "  " + atcLines[atcIndex]
                : string.Empty;

            System.Console.WriteLine($"  {logoLines[i]}{atcPart}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine($"  📡 {Dim}Tool{Reset}       {BrightWhite}atc-dsc{Reset}");
        System.Console.WriteLine($"  🏷  {Dim}Version{Reset}    {BrightWhite}{version}{Reset}");
        System.Console.WriteLine($"  📖 {Dim}Docs{Reset}       {BrightWhite}https://github.com/atc-net/atc-dsc-configurations{Reset}");
        System.Console.WriteLine();
    }

    private static string[] BuildBrailleLogo()
    {
        var grid = RasterizeDotGrid();
        var lines = new string[LogoLinesHigh];

        for (var lineY = 0; lineY < LogoLinesHigh; lineY++)
        {
            lines[lineY] = BuildBrailleLine(grid, lineY);
        }

        return lines;
    }

    private static int[][] RasterizeDotGrid()
    {
        var grid = new int[DotsTall][];
        for (var dotY = 0; dotY < DotsTall; dotY++)
        {
            grid[dotY] = new int[DotsWide];
            for (var dotX = 0; dotX < DotsWide; dotX++)
            {
                var svgX = (dotX + 0.5) / DotsWide * SvgWidth;
                var svgY = (dotY + 0.5) / DotsTall * SvgHeight;

                grid[dotY][dotX] = -1;
                for (var shape = 0; shape < AllShapes.Length; shape++)
                {
                    if (IsInsideConvexPolygon(svgX, svgY, AllShapes[shape]))
                    {
                        grid[dotY][dotX] = shape;
                        break;
                    }
                }
            }
        }

        return grid;
    }

    private static string BuildBrailleLine(
        int[][] grid,
        int lineY)
    {
        var sb = new StringBuilder();
        var lastColor = string.Empty;

        for (var charX = 0; charX < LogoCharsWide; charX++)
        {
            var (pattern, isLeft) = ComputeCellPattern(grid, lineY, charX);

            if (pattern == 0)
            {
                sb.Append(' ');
                lastColor = string.Empty;
                continue;
            }

            var svgY = ((lineY * 4.0) + 1.5) / DotsTall * SvgHeight;
            var color = ComputeGradientAnsi(isLeft, svgY);

            if (!string.Equals(color, lastColor, StringComparison.Ordinal))
            {
                sb.Append(color);
                lastColor = color;
            }

            sb.Append((char)(0x2800 + pattern));
        }

        sb.Append(Reset);
        return sb.ToString();
    }

    private static (int Pattern, bool IsLeft) ComputeCellPattern(
        int[][] grid,
        int lineY,
        int charX)
    {
        var pattern = 0;
        var leftCount = 0;
        var rightCount = 0;

        for (var dx = 0; dx < 2; dx++)
        {
            for (var dy = 0; dy < 4; dy++)
            {
                var shapeIndex = grid[(lineY * 4) + dy][(charX * 2) + dx];

                if (shapeIndex < 0)
                {
                    continue;
                }

                pattern |= BrailleBit(dx, dy);
                if (shapeIndex <= 1)
                {
                    leftCount++;
                }
                else
                {
                    rightCount++;
                }
            }
        }

        return (pattern, leftCount >= rightCount);
    }

    private static string ComputeGradientAnsi(
        bool isLeft,
        double svgY)
    {
        var (from, to, yMin, yMax) = isLeft
            ? (LeftGradientFrom, LeftGradientTo, LeftYMin, LeftYMax)
            : (RightGradientFrom, RightGradientTo, RightYMin, RightYMax);

        var t = System.Math.Clamp((svgY - yMin) / (yMax - yMin), 0.0, 1.0);
        var r = (int)(from.R + ((to.R - from.R) * t));
        var g = (int)(from.G + ((to.G - from.G) * t));
        var b = (int)(from.B + ((to.B - from.B) * t));

        return string.Create(
            CultureInfo.InvariantCulture,
            $"\e[38;2;{r};{g};{b}m");
    }

    private static int BrailleBit(
        int dx,
        int dy)
        => (dx, dy) switch
        {
            (0, 0) => 0x01,
            (0, 1) => 0x02,
            (0, 2) => 0x04,
            (0, 3) => 0x40,
            (1, 0) => 0x08,
            (1, 1) => 0x10,
            (1, 2) => 0x20,
            (1, 3) => 0x80,
            _ => 0,
        };

    private static bool IsInsideConvexPolygon(
        double px,
        double py,
        (double X, double Y)[] vertices)
    {
        var sign = 0;
        var count = vertices.Length;

        for (var i = 0; i < count; i++)
        {
            var next = (i + 1) % count;
            var cross = ((vertices[next].X - vertices[i].X) * (py - vertices[i].Y))
                      - ((vertices[next].Y - vertices[i].Y) * (px - vertices[i].X));

            if (cross > 0)
            {
                if (sign < 0)
                {
                    return false;
                }

                sign = 1;
            }
            else if (cross < 0)
            {
                if (sign > 0)
                {
                    return false;
                }

                sign = -1;
            }
        }

        return sign != 0;
    }

    private static string BuildGradientText(
        string text,
        (int R, int G, int B) from,
        (int R, int G, int B) to)
    {
        var result = new StringBuilder();
        var lastIndex = System.Math.Max(text.Length - 1, 1);

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
            {
                result.Append(' ');
                continue;
            }

            var red = from.R + ((to.R - from.R) * i / lastIndex);
            var green = from.G + ((to.G - from.G) * i / lastIndex);
            var blue = from.B + ((to.B - from.B) * i / lastIndex);
            result.Append(CultureInfo.InvariantCulture, $"\e[1;38;2;{red};{green};{blue}m{text[i]}");
        }

        return result.ToString();
    }
}