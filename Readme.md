# SecureWSDLImporter
SecureWSDLImporter is a .NET console application that downloads a WSDL file from a specified service URL that is secured by a client certificate. It processes its XML Schema imports recursively and saves the updated WSDL for local use. The WSDL can then be imported as a service reference within Visual Studio.

## Prerequisites

- [.NET 6.0 (or later)](https://dotnet.microsoft.com/download) installed on your machine.
- A valid PFX certificate file for client authentication.
- Access to the WSDL URL that is secured by a client certificate you intend to download.


## Installation

1. **Clone the Repository:**

   ```bash
   git clone https://github.com/DancingHobbit/SecureWSDLImporter.git
   cd SecureWSDLImporter
   ```

2. **Restore Dependencies and Build:**

   Restore NuGet packages (including CommandLineParser and Serilog) and build the project:

   ```bash
   dotnet build
   ```

## Usage

SecureWSDLImporter accepts several command-line options that control its behavior:

- `--pfx` (required): Path to the PFX certificate file.
- `--password` (optional): Password for the PFX certificate file.
- `--wsdl` (required): URL of the WSDL file to download.
- `--output` (optional): Output directory to save the downloaded files (default is the current directory).
- `--verbose` (optional): Enable verbose logging for detailed output.

### Detailed Usage

SecureWSDLImporter performs the following steps:
1. **Download the WSDL:** Connects to the specified WSDL URL using the provided client certificate.
2. **Process XML Schema Imports:** Scans the WSDL for `<xsd:import>` elements and downloads each referenced schema recursively.
3. **Save WSDL Files:** Saves the original WSDL as a temporary file and outputs an updated WSDL file with local schema references. This updated WSDL can be imported as a service reference in Visual Studio:
   ![image](https://github.com/user-attachments/assets/1f805540-e8b0-45d4-988d-ff21ee96ed52)


### Sample Command

```bash
dotnet run --pfx "gac.pfx" --password "password" --wsdl "https://mysite/my.svc?wsdl" --output "c:\mywsdl"
```

This command will:
- Use the client certificate located at `gac.pfx` with the password `"password"`.
- Download the WSDL from `https://mysite/my.svc?wsdl`.
- Save the processed files in the directory `c:\mywsdl`.


## License

This project is licensed under the [MIT License](LICENSE).
