using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SecureWSDLImporter;

namespace SecureWSDLImporter.Classes
{
    /// <summary>
    /// Provides functionality to process and download XML schema imports.
    /// </summary>
    public class SchemaService
    {
        private readonly HttpClient _httpClient;
        private readonly Options _options;
        private readonly System.Collections.Generic.Dictionary<string, string> _downloadedSchemas;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaService"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="options">The command line options.</param>
        public SchemaService(HttpClient httpClient, Options options)
        {
            _httpClient = httpClient;
            _options = options;
            _downloadedSchemas = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes all XML schema import elements in the given document recursively.
        /// </summary>
        /// <param name="doc">The XML document containing schema imports.</param>
        /// <param name="baseUrl">The base URL for resolving relative schema locations.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessImportsAsync(XDocument doc, string baseUrl)
        {
            XNamespace xs = "http://www.w3.org/2001/XMLSchema";
            var imports = doc.Descendants(xs + "import")
                             .Where(e => e.Attribute("schemaLocation") != null)
                             .ToList();

            foreach (var import in imports)
            {
                string importLocation = import.Attribute("schemaLocation")!.Value;
                string absoluteUrl = ResolveUrl(baseUrl, importLocation);
                Log.Debug("Processing import: {AbsoluteUrl}", absoluteUrl);

                try
                {
                    string localFilePath = await DownloadAndProcessSchemaAsync(absoluteUrl);
                    // Update the schemaLocation attribute to point to the local file.
                    import.SetAttributeValue("schemaLocation", Path.GetFileName(localFilePath));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error processing import '{AbsoluteUrl}'", absoluteUrl);
                }
            }
        }

        /// <summary>
        /// Downloads a schema from the specified URL, processes its imports recursively, saves it locally, and returns the local file path.
        /// </summary>
        /// <param name="schemaUrl">The URL of the schema to download.</param>
        /// <returns>The local file path where the schema is saved.</returns>
        public async Task<string> DownloadAndProcessSchemaAsync(string schemaUrl)
        {
            if (_downloadedSchemas.TryGetValue(schemaUrl, out string existingFile))
            {
                Log.Debug("Schema already downloaded: {SchemaUrl} -> {ExistingFile}", schemaUrl, existingFile);
                return existingFile;
            }

            Log.Information("Downloading schema: {SchemaUrl}", schemaUrl);
            string schemaContent = await _httpClient.GetStringAsync(schemaUrl);

            // Parse the downloaded schema.
            XDocument schemaDoc = XDocument.Parse(schemaContent);

            // Recursively process any nested schema imports.
            await ProcessImportsAsync(schemaDoc, schemaUrl);

            // Determine a suitable file name from the URL.
            Uri schemaUri = new Uri(schemaUrl);
            string baseName = Path.GetFileName(schemaUri.LocalPath);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"imported_{Guid.NewGuid()}.xsd";
            }

            // Construct a unique local file path.
            string localFilePath = Path.Combine(_options.OutputDirectory, baseName);
            int counter = 1;
            while (File.Exists(localFilePath))
            {
                string newBaseName = $"{Path.GetFileNameWithoutExtension(baseName)}_{counter}{Path.GetExtension(baseName)}";
                localFilePath = Path.Combine(_options.OutputDirectory, newBaseName);
                counter++;
            }

            schemaDoc.Save(localFilePath);
            Log.Information("Saved schema from {SchemaUrl} as {LocalFilePath}", schemaUrl, localFilePath);

            _downloadedSchemas[schemaUrl] = localFilePath;
            return localFilePath;
        }

        /// <summary>
        /// Resolves the absolute URL of a schema import based on a base URL and a relative path.
        /// </summary>
        /// <param name="baseUrl">The base URL for resolving.</param>
        /// <param name="relativeUrl">The relative URL to resolve.</param>
        /// <returns>The absolute URL as a string.</returns>
        private string ResolveUrl(string baseUrl, string relativeUrl)
        {
            try
            {
                Uri baseUri = new Uri(baseUrl);
                return new Uri(baseUri, relativeUrl).ToString();
            }
            catch
            {
                return relativeUrl;
            }
        }
    }
}
