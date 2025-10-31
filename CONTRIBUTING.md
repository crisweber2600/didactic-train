# Contributing to SharePoint File Deduplicator

Thank you for your interest in contributing! This document provides guidelines for contributing to the project.

## Development Setup

1. Fork and clone the repository
2. Install .NET 9.0 SDK
3. Open the solution in Visual Studio or VS Code
4. Follow SETUP.md for Azure AD configuration

## Project Structure

```
SharePointDeduplicator/
├── src/
│   ├── SharePointDeduplicator.API/      # Backend ASP.NET Core Web API
│   └── SharePointDeduplicator.Web/      # Frontend Blazor WebAssembly
├── README.md                             # Main documentation
├── SETUP.md                              # Quick setup guide
└── CONTRIBUTING.md                       # This file
```

## Development Workflow

1. Create a feature branch from `main`
2. Make your changes
3. Write/update tests
4. Ensure all tests pass
5. Update documentation if needed
6. Submit a pull request

## Coding Standards

### C# Code Style

- Follow Microsoft C# Coding Conventions
- Use meaningful variable and method names
- Add XML comments for public APIs
- Keep methods focused and concise
- Use async/await for I/O operations

### Example:

```csharp
/// <summary>
/// Scans a SharePoint site for duplicate files.
/// </summary>
/// <param name="siteUrl">The SharePoint site URL</param>
/// <param name="cancellationToken">Cancellation token</param>
/// <returns>A scan report containing duplicate information</returns>
public async Task<ScanReport> ScanSiteAsync(string siteUrl, CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Razor/Blazor Components

- Use meaningful component names
- Keep components focused on single responsibility
- Use parameters for reusable components
- Handle errors gracefully with user-friendly messages

## Testing Guidelines

### Unit Tests

- Test business logic in services
- Mock external dependencies
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)

### Integration Tests

- Test API endpoints
- Test Graph API integration
- Verify error handling

## Documentation

- Update README.md for new features
- Add XML comments to public methods
- Update SETUP.md if configuration changes
- Include inline comments for complex logic

## Pull Request Process

1. **Title**: Use a descriptive title
   - ✅ Good: "Add pagination to duplicate file list"
   - ❌ Bad: "Update Report.razor"

2. **Description**: Include:
   - What changes were made
   - Why the changes were needed
   - How to test the changes
   - Screenshots for UI changes

3. **Checklist**:
   - [ ] Code follows project style guidelines
   - [ ] Self-review completed
   - [ ] Comments added for complex code
   - [ ] Documentation updated
   - [ ] No new warnings generated
   - [ ] Tests added/updated
   - [ ] All tests pass

## Reporting Issues

### Bug Reports

Include:
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment (OS, .NET version, browser)
- Screenshots if applicable

### Feature Requests

Include:
- Problem statement
- Proposed solution
- Alternative solutions considered
- Additional context

## Security

- Never commit secrets or credentials
- Report security vulnerabilities privately
- Use Azure Key Vault for production secrets
- Follow least privilege principle

## Code of Conduct

- Be respectful and constructive
- Welcome newcomers
- Focus on what is best for the community
- Show empathy towards others

## Questions?

Feel free to open an issue for:
- Questions about the codebase
- Clarification on requirements
- Discussion about new features

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
