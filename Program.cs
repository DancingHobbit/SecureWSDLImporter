using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine; // Install via NuGet: dotnet add package CommandLineParser
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SecureWSDLImporter.Classes;

namespace SecureWSDLImporter
{
    /// <summary>
    /// Command line options for the application.
    /// </summary>
    public class Options
    {
        [Option('p', "pfx", Required = true, HelpText = "Path to the PFX file.")]
        public required string PfxFile { get; set; }

        [Option('s', "password", Required = false, HelpText = "Password for the PFX file.")]
        public string? PfxPassword { get; set; }

        [Option('w', "wsdl", Required = true, HelpText = "URL of the WSDL file to download.")]
        public required string WsdlUrl { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output directory to save the downloaded files.", Default = ".")]
        public required string OutputDirectory { get; set; }

        [Option('v', "verbose", Required = false, HelpText = "Enable verbose logging.", Default = false)]
        public bool Verbose { get; set; }
    }

   
    class Program
    {
      
        static async Task<int> Main(string[] args)
        {
            // First, parse the command-line arguments.
            return await Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    async (Options options) =>
                    {
                        // Configure Serilog with colored console output and set the minimum level based on the verbose flag.
                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Is(options.Verbose ? LogEventLevel.Debug : LogEventLevel.Information)
                            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                            .CreateLogger();

                        Log.Information("Starting WsdlDownloader...");
                        await RunOptionsAsync(options);
                        return 0;
                    },
                    async errors =>
                    {
                        // In case of command-line parsing errors, set a default logger and log the failure.
                        Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Information()
                            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
                            .CreateLogger();

                        Log.Error("Failed to parse command line arguments.");
                        return 1;
                    });
        }

        /// <summary>
        /// Parses the provided options, sets up required resources, and starts the WSDL processing.
        /// </summary>
        /// <param name="options">Parsed command line options.</param>
        private static async Task RunOptionsAsync(Options options)
        {
            // Check that the certificate file exists.
            if (!File.Exists(options.PfxFile))
            {
                Log.Error("PFX file not found: {PfxFile}", options.PfxFile);
                return;
            }

            // Ensure the output directory exists.
            try
            {
                Directory.CreateDirectory(options.OutputDirectory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create output directory {OutputDirectory}", options.OutputDirectory);
                return;
            }

            // Load the client certificate.
            X509Certificate2 certificate;
            try
            {
                certificate = new X509Certificate2(options.PfxFile, options.PfxPassword, X509KeyStorageFlags.MachineKeySet);
                Log.Information("Certificate loaded successfully from {PfxFile}", options.PfxFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading certificate from {PfxFile}", options.PfxFile);
                return;
            }

            // Configure HttpClient with the certificate.
            using var httpClient = CreateHttpClient(certificate);

            // Initialize the services.
            var schemaService = new SchemaService(httpClient, options);
            var wsdlService = new WsdlService(httpClient, options, schemaService);

            try
            {
                await wsdlService.ProcessWsdlAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred during WSDL processing.");
            }
        }

        /// <summary>
        /// Creates an HttpClient configured with the provided client certificate.
        /// </summary>
        /// <param name="certificate">The client certificate.</param>
        /// <returns>A configured HttpClient instance.</returns>
        private static HttpClient CreateHttpClient(X509Certificate2 certificate)
        {
            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(certificate);
            //Bypass server certificate validation.
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            return new HttpClient(handler);
        }
    }
}
