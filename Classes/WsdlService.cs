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
    /// Provides functionality to download and process a WSDL file.
    /// </summary>
    public class WsdlService
    {
        private readonly HttpClient _httpClient;
        private readonly Options _options;
        private readonly SchemaService _schemaService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WsdlService"/> class.
        /// </summary>
        /// <param name="httpClient">The HttpClient instance.</param>
        /// <param name="options">The command line options.</param>
        /// <param name="schemaService">The schema service for processing imports.</param>
        public WsdlService(HttpClient httpClient, Options options, SchemaService schemaService)
        {
            _httpClient = httpClient;
            _options = options;
            _schemaService = schemaService;
        }

        /// <summary>
        /// Downloads the WSDL, processes its schema imports
        /// </summary>
        public async Task ProcessWsdlAsync()
        {
            Log.Information("Downloading WSDL from: {WsdlUrl}", _options.WsdlUrl);

            string wsdlContent = await _httpClient.GetStringAsync(_options.WsdlUrl);

            // Save the original WSDL as a temporary file.
            string originalWsdlPath = Path.Combine(_options.OutputDirectory, "originaldownloaded.wsdl");
            await File.WriteAllTextAsync(originalWsdlPath, wsdlContent);
            Log.Information("Original WSDL saved as {OriginalWsdlPath}", originalWsdlPath);

            // Parse the WSDL.
            XDocument wsdlDoc = XDocument.Parse(wsdlContent);

            // Process schema imports recursively.
            await _schemaService.ProcessImportsAsync(wsdlDoc, _options.WsdlUrl);

            // Save the updated WSDL.
            string updatedWsdlPath = Path.Combine(_options.OutputDirectory, "updated.wsdl");
            wsdlDoc.Save(updatedWsdlPath);
            Log.Information("Updated WSDL saved as {UpdatedWsdlPath}", updatedWsdlPath);

          
        }
    }

}
