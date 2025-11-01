# Quick Setup Guide

## Prerequisites Checklist

- [ ] .NET 9.0 SDK installed
- [ ] Azure AD tenant access
- [ ] SharePoint site with admin permissions

## Step-by-Step Setup

### 1. Azure AD Configuration (5 minutes)

1. Navigate to https://portal.azure.com
2. Go to **Azure Active Directory** → **App registrations** → **New registration**
3. Set:
   - Name: `SharePoint Deduplicator`
   - Supported accounts: **Single tenant**
4. Click **Register**
5. Copy the following from the **Overview** page:
   - **Application (client) ID**
   - **Directory (tenant) ID**

### 2. Configure API Permissions (3 minutes)

1. In your app, go to **API permissions**
2. Click **Add a permission** → **Microsoft Graph** → **Application permissions**
3. Add these permissions:
   - `Sites.Read.All`
   - `Files.Read.All`
   - `Files.ReadWrite.All`
4. Click **Grant admin consent for [your tenant]**

### 3. Create Client Secret (2 minutes)

1. Go to **Certificates & secrets**
2. Click **New client secret**
3. Add description: `SharePoint Deduplicator Secret`
4. Choose expiry: **6 months** (or as per policy)
5. Click **Add**
6. **IMMEDIATELY COPY THE VALUE** - you cannot see it again!

### 4. Configure Application (2 minutes)

1. Open `src/SharePointDeduplicator.API/appsettings.json`
2. Replace placeholders:

```json
{
  "AzureAd": {
    "TenantId": "paste-tenant-id-here",
    "ClientId": "paste-client-id-here",
    "ClientSecret": "paste-secret-value-here"
  }
}
```

### 5. Run the Application (1 minute)

Open two terminals:

**Terminal 1 - Backend:**
```bash
cd src/SharePointDeduplicator.API
dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd src/SharePointDeduplicator.Web
dotnet run
```

### 6. Test the Application (5 minutes)

1. Open browser to: https://localhost:7001
2. Click **Start Scanning**
3. Enter SharePoint site URL: `https://yourtenant.sharepoint.com/sites/testsite`
4. Click **Start Scan**
5. Wait for results

## Troubleshooting

### Error: "Unable to retrieve site information"
✅ **Solution**: Check that the SharePoint URL is correct and accessible

### Error: "403 Forbidden"
✅ **Solution**: Ensure admin consent was granted for API permissions

### Error: "Invalid client secret"
✅ **Solution**: Generate a new client secret and update appsettings.json

### Error: "Site not found"
✅ **Solution**: Verify the exact SharePoint site URL format

## Test URLs

Use these SharePoint URL formats:
- Root site: `https://yourtenant.sharepoint.com`
- Team site: `https://yourtenant.sharepoint.com/sites/sitename`
- Personal site: `https://yourtenant-my.sharepoint.com/personal/user_domain_com`

## Security Best Practices

⚠️ **For Production:**
1. Use Azure Key Vault for secrets
2. Enable Application Insights for logging
3. Implement rate limiting
4. Add authentication to the frontend
5. Use managed identities instead of client secrets

## Next Steps

1. ✅ Test with a small SharePoint site first
2. ✅ Review duplicate report before making changes
3. ✅ Always verify shortcuts after replacement
4. ✅ Keep backup of SharePoint content

## Support

For issues, check:
- README.md - Full documentation
- Azure Portal - App registration settings
- SharePoint Admin Center - Site permissions

## Estimated Time

- Initial setup: **15-20 minutes**
- First scan: **5-10 minutes** (depending on site size)
- Total: **~30 minutes** to full operation
