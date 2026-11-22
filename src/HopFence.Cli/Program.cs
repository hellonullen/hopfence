using System;
using System.IO;
using System.Threading.Tasks;
using HopFence.Core.Configuration;
using HopFence.Core.Engine;
using HopFence.Core.Models;
using HopFence.Reporting;

namespace HopFence.Cli;

public static class Program
{
    private const string Version = "1.0.0";
    private const string Author = "NulleN - Kadircan Üredi (kadircanuredi.com.tr / hellonullen@gmail.com)";

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();

        switch (command)
        {
            case "version" or "--version" or "-v":
                Console.WriteLine($"HopFence CLI v{Version}");
                Console.WriteLine($"Created by {Author}");
                return 0;

            case "init":
                return HandleInit(args);

            case "diagnose":
                return await HandleDiagnoseAsync(args);

            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Unknown command: '{args[0]}'. See 'hopfence --help' for usage.");
                Console.ResetColor();
                return 2;
        }
    }

    private static int HandleInit(string[] args)
    {
        string targetFile = "hopfence.yml";
        bool force = false;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i].Equals("--force", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-f", StringComparison.OrdinalIgnoreCase))
            {
                force = true;
            }
            else if (args[i].Equals("--output", StringComparison.OrdinalIgnoreCase) || args[i].Equals("-o", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) targetFile = args[++i];
            }
        }

        if (File.Exists(targetFile) && !force)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Error: '{targetFile}' already exists. Use --force to overwrite.");
            Console.ResetColor();
            return 2;
        }

        string defaultYaml = ConfigLoader.GenerateDefaultConfigYaml();
        File.WriteAllText(targetFile, defaultYaml);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✓ Generated default HopFence configuration in '{targetFile}'");
        Console.ResetColor();
        Console.WriteLine("Edit target.baseUrl and HOPFENCE_TOKEN, then run:");
        Console.WriteLine("  hopfence diagnose");
        return 0;
    }

    private static async Task<int> HandleDiagnoseAsync(string[] args)
    {
        string configFile = "hopfence.yml";
        string? formatOverride = null;
        string? outputOverride = null;
        string? targetOverride = null;
        string? tokenOverride = null;
        bool insecureOverride = false;
        bool verboseOverride = false;

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.Equals("--config", StringComparison.OrdinalIgnoreCase) || arg.Equals("-c", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) configFile = args[++i];
            }
            else if (arg.Equals("--format", StringComparison.OrdinalIgnoreCase) || arg.Equals("-f", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) formatOverride = args[++i];
            }
            else if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) || arg.Equals("-o", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) outputOverride = args[++i];
            }
            else if (arg.Equals("--target", StringComparison.OrdinalIgnoreCase) || arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) targetOverride = args[++i];
            }
            else if (arg.Equals("--token", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) tokenOverride = args[++i];
            }
            else if (arg.Equals("--insecure", StringComparison.OrdinalIgnoreCase) || arg.Equals("-k", StringComparison.OrdinalIgnoreCase))
            {
                insecureOverride = true;
            }
            else if (arg.Equals("--verbose", StringComparison.OrdinalIgnoreCase))
            {
                verboseOverride = true;
            }
        }

        HopFenceConfig config;

        if (File.Exists(configFile))
        {
            try
            {
                config = ConfigLoader.LoadFromFile(configFile);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error loading config from '{configFile}': {ex.Message}");
                Console.ResetColor();
                return 2;
            }
        }
        else if (!string.IsNullOrWhiteSpace(targetOverride))
        {
            // Direct target without config file
            config = new HopFenceConfig();
            config.Target.BaseUrl = targetOverride;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Configuration file '{configFile}' not found. Run 'hopfence init' or provide --target <url>.");
            Console.ResetColor();
            return 2;
        }

        // Apply command-line overrides
        if (!string.IsNullOrWhiteSpace(targetOverride)) config.Target.BaseUrl = targetOverride;
        if (!string.IsNullOrWhiteSpace(tokenOverride)) config.Target.Token = tokenOverride;
        if (insecureOverride) config.Target.Insecure = true;
        if (verboseOverride) config.Reporting.Verbose = true;
        if (!string.IsNullOrWhiteSpace(formatOverride)) config.Reporting.Format = formatOverride;
        if (!string.IsNullOrWhiteSpace(outputOverride)) config.Reporting.OutputFile = outputOverride;

        Action<string>? verboseLogger = config.Reporting.Verbose
            ? msg =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[DEBUG] {msg}");
                Console.ResetColor();
            }
            : null;

        var engine = new DiagnosticEngine();
        DiagnosticReport report;

        try
        {
            report = await engine.RunDiagnosticsAsync(config, logVerbose: verboseLogger);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal diagnostic error: {ex.Message}");
            Console.ResetColor();
            return 2;
        }

        // Primary reporting
        var primaryReporter = ReporterFactory.GetReporter(config.Reporting.Format);
        string outputText = primaryReporter.RenderToString(report);

        // Always print console reporter if format is console or if output was sent to file
        if (config.Reporting.Format.Equals("console", StringComparison.OrdinalIgnoreCase))
        {
            Console.Write(outputText);
        }
        else
        {
            // If output format was json/html/junit without --output, print to stdout
            if (string.IsNullOrWhiteSpace(config.Reporting.OutputFile))
            {
                Console.WriteLine(outputText);
            }
            else
            {
                // Print a mini console summary so CLI user sees progress
                var consoleReporter = new ConsoleReporter();
                Console.Write(consoleReporter.RenderToString(report));
            }
        }

        // Save report to file if requested
        if (!string.IsNullOrWhiteSpace(config.Reporting.OutputFile))
        {
            try
            {
                await primaryReporter.WriteToFileAsync(report, config.Reporting.OutputFile);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Report successfully saved to '{config.Reporting.OutputFile}' ({config.Reporting.Format})");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to write report to '{config.Reporting.OutputFile}': {ex.Message}");
                Console.ResetColor();
            }
        }

        return report.IsSuccess ? 0 : 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("HopFence — Reverse Proxy Diagnostics & Security Testing Tool");
        Console.WriteLine($"Author: {Author}");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  hopfence [command] [options]");
        Console.WriteLine();
        Console.WriteLine("COMMANDS:");
        Console.WriteLine("  init                   Generate a starter hopfence.yml configuration file");
        Console.WriteLine("  diagnose               Run reverse proxy diagnostic and security suite");
        Console.WriteLine("  version                Display HopFence version information");
        Console.WriteLine();
        Console.WriteLine("OPTIONS (for diagnose):");
        Console.WriteLine("  -c, --config <file>    Path to config file (default: hopfence.yml)");
        Console.WriteLine("  -t, --target <url>     Target base URL override (e.g. https://staging.example.com)");
        Console.WriteLine("  -f, --format <format>  Output format: console, json, html, junit (default: console)");
        Console.WriteLine("  -o, --output <file>    File path to save the generated report");
        Console.WriteLine("      --token <token>    Bearer authentication token override");
        Console.WriteLine("  -k, --insecure         Bypass SSL/TLS certificate verification");
        Console.WriteLine("      --verbose          Enable verbose diagnostic logging");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  hopfence init");
        Console.WriteLine("  hopfence diagnose");
        Console.WriteLine("  hopfence diagnose --format html --output report.html");
        Console.WriteLine("  hopfence diagnose --format junit --output results.xml");
        Console.WriteLine("  hopfence diagnose --target http://localhost:5000 --token mysecret --insecure");
        Console.WriteLine();
    }
}
