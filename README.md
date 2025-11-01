# SharePoint File Deduplicator

A full-stack .NET application that scans SharePoint sites for duplicate files and replaces them with shortcuts to save storage space.

## Features

- 🔍 **Scan SharePoint sites** using Microsoft Graph API
- 🔐 **Secure authentication** via Azure AD
- 📊 **Detailed duplicate reports** with file hashes and space savings
- 🔗 **Smart replacement** - replace duplicates with shortcuts while maintaining folder structure
- ✅ **Verification system** to ensure navigation integrity after changes

## Architecture

- **Backend**: ASP.NET Core Web API (.NET 9)
  - Microsoft Graph SDK integration
  - SharePoint file scanning and hash calculation
  - Duplicate detection and replacement logic
  
- **Frontend**: Blazor WebAssembly (.NET 9)
  - Interactive UI for scanning and reviewing duplicates
  - File selection interface
  - Real-time progress and results display

## Prerequisites

- .NET 9.0 SDK or later
- Azure AD application registration with Microsoft Graph API permissions
- SharePoint site access

## Azure AD Setup

1. **Register an Azure AD Application**:
   - Go to [Azure Portal](https://portal.azure.com)
   - Navigate to Azure Active Directory → App registrations → New registration
   - Name: `SharePoint Deduplicator`
   - Supported account types: Accounts in this organizational directory only
   - Click **Register**

2. **Configure API Permissions**:
   - In your app registration, go to **API permissions**
   - Add the following Microsoft Graph **Application permissions**:
     - `Sites.Read.All` - Read items in all site collections
     - `Files.Read.All` - Read files in all site collections
     - `Files.ReadWrite.All` - Read and write files in all site collections (for replacing duplicates)
   - Click **Grant admin consent** for your organization

3. **Create a Client Secret**:
   - Go to **Certificates & secrets**
   - Click **New client secret**
   - Add a description and choose an expiry period
   - Copy the secret **value** immediately (you won't be able to see it again)

4. **Note your Application Details**:
   - **Tenant ID**: Found in the Overview page
   - **Client ID (Application ID)**: Found in the Overview page
   - **Client Secret**: The value you just copied

## Configuration

### Backend API Configuration

1. Open `src/SharePointDeduplicator.API/appsettings.json`

2. Update the Azure AD configuration:

```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id-here",
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here"
  }
}
```

**Security Note**: For production, use Azure Key Vault or environment variables instead of storing secrets in appsettings.json

### Frontend Configuration

1. Open `src/SharePointDeduplicator.Web/wwwroot/appsettings.json`

2. Update the API base address if needed (default is `https://localhost:7042`)

```json
{
  "ApiBaseAddress": "https://localhost:7042"
}
```

## Running the Application

### Using Visual Studio

1. Open `SharePointDeduplicator.sln`
2. Set both projects as startup projects:
   - Right-click solution → Properties → Multiple startup projects
   - Set both `SharePointDeduplicator.API` and `SharePointDeduplicator.Web` to **Start**
3. Press F5 to run

### Using Command Line

**Terminal 1 - API**:
```bash
cd src/SharePointDeduplicator.API
dotnet run
```

**Terminal 2 - Frontend**:
```bash
cd src/SharePointDeduplicator.Web
dotnet run
```

The API will be available at: `https://localhost:7042`  
The Frontend will be available at: `https://localhost:7001`

## Usage

### 1. Scan for Duplicates

1. Navigate to the Scanner page
2. Enter your SharePoint site URL (e.g., `https://yourtenant.sharepoint.com/sites/yoursite`)
3. Click **Start Scan**
4. Wait for the scan to complete

### 2. Review Duplicate Report

1. After scanning, click **View Detailed Report**
2. Review the duplicate groups, showing:
   - Number of duplicate files
   - Total space wasted
   - File details for each duplicate

### 3. Select True Copies

1. For each duplicate group, select which file should be kept as the "true" copy
2. All other files will be replaced with shortcuts pointing to the true copy
3. Click **Replace Duplicates with Shortcuts** when ready

### 4. Verify Folder Structure

1. After replacement, click **Verify Shortcuts**
2. The system will check that all shortcuts are valid
3. Review any broken shortcuts if found

## API Endpoints

### Scan Endpoint
```
POST /api/sharepoint/scan
Body: { "siteUrl": "https://yourtenant.sharepoint.com/sites/yoursite" }
```

### Get Scan Report
```
GET /api/sharepoint/scan/{scanId}
```

### Replace with Shortcuts
```
POST /api/sharepoint/replace
Body: {
  "scanId": "scan-id",
  "selections": [
    { "hash": "file-hash", "trueCopyFileId": "file-id" }
  ]
}
```

### Verify Shortcuts
```
POST /api/sharepoint/verify/{scanId}
```

## Development

### Project Structure

```
SharePointDeduplicator/
├── src/
│   ├── SharePointDeduplicator.API/
│   │   ├── Controllers/
│   │   │   └── SharePointController.cs
│   │   ├── Models/
│   │   │   ├── FileInfo.cs
│   │   │   ├── DuplicateGroup.cs
│   │   │   ├── ScanReport.cs
│   │   │   ├── ReplacementRequest.cs
│   │   │   ├── ReplacementResult.cs
│   │   │   └── VerificationResult.cs
│   │   ├── Services/
│   │   │   └── SharePointScannerService.cs
│   │   └── Program.cs
│   └── SharePointDeduplicator.Web/
│       ├── Pages/
│       │   ├── Home.razor
│       │   ├── Scanner.razor
│       │   └── Report.razor
│       ├── Layout/
│       └── Program.cs
└── README.md
```

### Building

```bash
dotnet build
```

### Testing

```bash
dotnet test
```

## Security Considerations

1. **Never commit secrets**: Use environment variables or Azure Key Vault for production
2. **Least privilege**: Grant only required Graph API permissions
3. **Audit logging**: Review logs for file operations
4. **Backup**: Always backup SharePoint sites before bulk operations

## Troubleshooting

### "Unable to retrieve site information"
- Verify the SharePoint site URL is correct
- Ensure the Azure AD app has proper permissions
- Check that admin consent was granted

### "Error scanning site: 403 Forbidden"
- Verify Graph API permissions are configured correctly
- Ensure admin consent was granted for the application
- Check that the service principal has access to the SharePoint site

### Build Errors
- Ensure .NET 9.0 SDK is installed: `dotnet --version`
- Restore NuGet packages: `dotnet restore`

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
