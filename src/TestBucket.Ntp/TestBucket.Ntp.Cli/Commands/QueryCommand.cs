using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using TestBucket.Ntp.Core;
using TestBucket.Ntp.Core.Client;
using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Cli.Commands;

internal sealed class QuerySettings : CommandSettings
{
    [CommandArgument(0, "[hostname]")]
    [Description("The NTP server hostname to query")]
    public string Hostname { get; init; } = "pool.ntp.org";
}

internal sealed class QueryCommand : AsyncCommand<QuerySettings>
{
    private const ulong NtpEpochOffset = 2208988800ul;

    public override async Task<int> ExecuteAsync(CommandContext context, QuerySettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(new Rule($"[bold cyan]NTP Query  →  {Markup.Escape(settings.Hostname)}[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        var client = new NtpClient();
        NtpResponseContext? response = null;
        Exception? error = null;
        var stopwatch = new Stopwatch();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("[yellow]Waiting for NTP response...[/]", async ctx =>
            {
                try
                {
                    stopwatch.Start();
                    response = await client.QueryAsync(settings.Hostname);
                    stopwatch.Stop();
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    error = ex;
                }
            });

        if (error is not null)
        {
            AnsiConsole.MarkupLine($"[bold red]Error:[/] {Markup.Escape(error.Message)}");
            return 1;
        }

        if (response is null)
        {
            AnsiConsole.MarkupLine("[bold red]No response received.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"  [grey]Round-trip time :[/] [bold green]{stopwatch.ElapsedMilliseconds} ms[/]");
        AnsiConsole.MarkupLine($"  [grey]T1 Client send  :[/] [bold green]{response.ClientTransmitTime:yyyy-MM-dd HH:mm:ss.fff} UTC[/]");
        AnsiConsole.MarkupLine($"  [grey]T2 Server recv  :[/] [bold green]{response.ServerReceiveTime:yyyy-MM-dd HH:mm:ss.fff} UTC[/]");
        AnsiConsole.MarkupLine($"  [grey]T3 Server send  :[/] [bold green]{response.ServerTransmitTime:yyyy-MM-dd HH:mm:ss.fff} UTC[/]");
        AnsiConsole.MarkupLine($"  [grey]T4 Client recv  :[/] [bold green]{response.ClientReceiveTime:yyyy-MM-dd HH:mm:ss.fff} UTC[/]");
        AnsiConsole.MarkupLine($"  [grey]Calculated time :[/] [bold green]{response.CalculatedTime:yyyy-MM-dd HH:mm:ss.fff} UTC[/]");
        AnsiConsole.MarkupLine($"  [grey]Clock offset    :[/] [bold yellow]{response.ClockOffset.TotalMilliseconds:+0.000;-0.000;0.000} ms[/]");
        AnsiConsole.MarkupLine($"  [grey]Round-trip delay:[/] [bold green]{response.RoundTripDelay.TotalMilliseconds:0.000} ms[/]");
        AnsiConsole.WriteLine();

        if (response.Packet is not null)
        {
            PrintPacketFields(response.Packet);
            AnsiConsole.WriteLine();
        }

        if (response.RawBytes is { Length: > 0 })
        {
            PrintHexDump(response.RawBytes);
        }

        return 0;
    }

    private static void PrintPacketFields(NtpPacket p)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold]NTP Response Packet Fields[/]")
            .AddColumn(new TableColumn("[bold cyan]Field[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold cyan]Offset[/]").RightAligned())
            .AddColumn(new TableColumn("[bold cyan]Size[/]").RightAligned())
            .AddColumn(new TableColumn("[bold cyan]Value[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold cyan]Description[/]").LeftAligned());

        // Byte 0 fields
        string li = p.LeapIndicator switch
        {
            0 => "[green]0[/] [grey](No warning)[/]",
            1 => "[yellow]1[/] [grey](Last minute has 61 seconds)[/]",
            2 => "[yellow]2[/] [grey](Last minute has 59 seconds)[/]",
            3 => "[red]3[/] [grey](Alarm – clock not synchronized)[/]",
            _ => p.LeapIndicator.ToString()
        };
        table.AddRow("[white]Leap Indicator[/]",  "0",  "2 bits", li, "Warn of upcoming leap second");

        table.AddRow("[white]Version[/]",          "0",  "3 bits", $"[green]{p.VersionNumber}[/]",  $"NTPv{p.VersionNumber}");

        string mode = p.Mode switch
        {
            0 => "[grey]0 (Reserved)[/]",
            1 => "[white]1 (Symmetric Active)[/]",
            2 => "[white]2 (Symmetric Passive)[/]",
            3 => "[white]3 (Client)[/]",
            4 => "[green]4 (Server)[/]",
            5 => "[white]5 (Broadcast)[/]",
            6 => "[grey]6 (NTP Control)[/]",
            7 => "[grey]7 (Private)[/]",
            _ => p.Mode.ToString()
        };
        table.AddRow("[white]Mode[/]",             "0",  "3 bits", mode, "Association mode");

        // Byte 1
        string stratum = p.Stratum switch
        {
            0    => "[red]0[/] [grey](Unspecified / Kiss-of-Death)[/]",
            1    => "[green]1[/] [grey](Primary reference)[/]",
            <= 15 => $"[yellow]{p.Stratum}[/] [grey](Secondary reference)[/]",
            _    => $"[grey]{p.Stratum} (Reserved)[/]"
        };
        table.AddRow("[white]Stratum[/]",          "1",  "8 bits", stratum, "Clock hierarchy level");

        // Byte 2
        double pollSeconds = Math.Pow(2, p.PollInterval);
        table.AddRow("[white]Poll Interval[/]",    "2",  "8 bits",
            $"[white]{p.PollInterval}[/] [grey](2^{p.PollInterval} = {pollSeconds:0} s)[/]",
            "Max interval between messages");

        // Byte 3
        double precisionSec = Math.Pow(2, p.Precision);
        table.AddRow("[white]Precision[/]",        "3",  "8 bits",
            $"[white]{p.Precision}[/] [grey](2^{p.Precision} ≈ {precisionSec * 1e6:0.000} µs)[/]",
            "Precision of the local clock");

        // Bytes 4-7  (fixed-point 16.16)
        double rootDelayMs = ((p.RootDelay >> 16) + (p.RootDelay & 0xFFFF) / 65536.0) * 1000.0;
        table.AddRow("[white]Root Delay[/]",       "4",  "32 bits",
            $"[white]0x{p.RootDelay:X8}[/] [grey]({rootDelayMs:0.000} ms)[/]",
            "Round-trip to primary reference");

        // Bytes 8-11
        double rootDispMs = ((p.RootDispersion >> 16) + (p.RootDispersion & 0xFFFF) / 65536.0) * 1000.0;
        table.AddRow("[white]Root Dispersion[/]",  "8",  "32 bits",
            $"[white]0x{p.RootDispersion:X8}[/] [grey]({rootDispMs:0.000} ms)[/]",
            "Max error from primary reference");

        // Bytes 12-15
        string refId = FormatReferenceIdentifier(p.ReferenceIdentifier, p.Stratum);
        table.AddRow("[white]Reference ID[/]",     "12", "32 bits",
            $"[white]0x{p.ReferenceIdentifier:X8}[/] [grey]({Markup.Escape(refId)})[/]",
            "Identifies the time source");

        // Timestamps (bytes 16-47)
        table.AddRow("[white]Reference Timestamp[/]",  "16", "64 bits", FormatTimestamp(p.ReferenceTimestamp),  "When the clock was last set");
        table.AddRow("[white]Originate Timestamp[/]", "24", "64 bits", FormatTimestamp(p.OriginateTimestamp), "T1 – When the request departed client");
        table.AddRow("[white]Receive Timestamp[/]",    "32", "64 bits", FormatTimestamp(p.ReceiveTimestamp),    "T2 – When the request arrived at server");
        table.AddRow("[white]Transmit Timestamp[/]",   "40", "64 bits", FormatTimestamp(p.TransmitTimestamp),   "T3 – When the response left the server");

        AnsiConsole.Write(table);
    }

    private static string FormatReferenceIdentifier(uint refId, byte stratum)
    {
        var bytes = new byte[] { (byte)(refId >> 24), (byte)(refId >> 16), (byte)(refId >> 8), (byte)refId };
        var name = Encoding.ASCII.GetString(bytes).TrimEnd('\0');
        var known = ExternalReferenceSource.All.FirstOrDefault(s => s.Name == name);
        if(known is not null)
        {
            return known.Name;
        }

        return $"{(refId >> 24) & 0xFF}.{(refId >> 16) & 0xFF}.{(refId >> 8) & 0xFF}.{refId & 0xFF}";
    }

    private static string FormatTimestamp(ulong timestamp)
    {
        if (timestamp == 0)
            return "[grey]0x0000000000000000 (not set)[/]";

        var seconds = timestamp >> 32;
        if (seconds < NtpEpochOffset)
            return $"[grey]0x{timestamp:X16} (before Unix epoch)[/]";

        var fraction = timestamp & 0xFFFFFFFF;
        var unixSeconds = seconds - NtpEpochOffset;
        var ms = (fraction * 1000) / 0x100000000L;
        var dt = DateTimeOffset.FromUnixTimeSeconds((long)unixSeconds).AddMilliseconds(ms);
        return $"[green]{dt:yyyy-MM-dd HH:mm:ss.fff} UTC[/] [grey](0x{timestamp:X16})[/]";
    }

    private static void PrintHexDump(byte[] bytes)
    {
        AnsiConsole.Write(new Rule("[bold]Raw Packet Hex Dump[/]").RuleStyle("grey"));
        AnsiConsole.WriteLine();

        const int bytesPerRow = 16;
        var sb = new StringBuilder();

        for (int i = 0; i < bytes.Length; i += bytesPerRow)
        {
            sb.Clear();

            // Offset column
            sb.Append($"[grey]{i:X4}[/]  ");

            int rowEnd = Math.Min(i + bytesPerRow, bytes.Length);

            // Hex bytes
            for (int j = i; j < rowEnd; j++)
            {
                if (j - i == 8) sb.Append(' ');
                sb.Append($"[cyan]{bytes[j]:X2}[/] ");
            }

            // Padding for short final row
            int padding = bytesPerRow - (rowEnd - i);
            for (int j = 0; j < padding; j++)
            {
                if (rowEnd - i + j == 8) sb.Append(' ');
                sb.Append("   ");
            }

            // ASCII column
            sb.Append(" [grey]|[/]");
            for (int j = i; j < rowEnd; j++)
            {
                char c = bytes[j] is >= 32 and < 127 ? (char)bytes[j] : '.';
                sb.Append($"[grey]{Markup.Escape(c.ToString())}[/]");
            }
            sb.Append("[grey]|[/]");

            AnsiConsole.MarkupLine(sb.ToString());
        }

        AnsiConsole.WriteLine();
    }


    private static void Stamp(char[] buf, int pos, string text)
    {
        for (int i = 0; i < text.Length && pos + i < buf.Length; i++)
            buf[pos + i] = text[i];
    }
    private static string Center(string text, int width)
    {
        if (text.Length >= width)
            return text.Length == width ? text : text[..width];
        int pad = width - text.Length;
        return new string(' ', pad / 2) + text + new string(' ', pad - pad / 2);
    }
}
