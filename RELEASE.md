# Release Guide for MapDecals

This guide explains how to create releases for the MapDecals plugin using the automated GitHub Actions workflow.

## Automated Releases

The repository includes GitHub Actions workflows that automatically build and release the plugin.

### Workflow Files

1. **`.github/workflows/build-release.yml`** - Builds and creates GitHub releases when you push a version tag
2. **`.github/workflows/ci.yml`** - Runs continuous integration builds on pull requests and pushes

### Creating a New Release

To create a new release:

1. **Update the version** in your code if needed (e.g., in `MapDecals.cs`):
   ```csharp
   public override string ModuleVersion => "1.0.0";
   ```

2. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Prepare for v1.0.0 release"
   git push
   ```

3. **Create and push a version tag**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

4. **Automated process**:
   - GitHub Actions automatically triggers the build workflow
   - The plugin is compiled in Release mode
   - A ZIP package is created containing:
     - `MapDecals.dll` and all dependencies
     - `config.json` example configuration
     - `README.md` documentation
     - `IMPLEMENTATION_NOTES.md` technical details
   - A GitHub Release is created with the ZIP file attached
   - Release notes are automatically generated from commits

### Release Package Contents

The automated release creates a ZIP file named `MapDecals-v{version}.zip` containing:

```
MapDecals-v1.0.0.zip
├── MapDecals/
│   ├── MapDecals.dll                    # Main plugin DLL
│   ├── config.json                       # Example configuration
│   └── [dependencies]                    # All required DLLs
├── README.md                             # User documentation
└── IMPLEMENTATION_NOTES.md               # Technical documentation
```

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):
- **MAJOR** version (1.0.0): Incompatible API changes
- **MINOR** version (1.1.0): New functionality, backwards compatible
- **PATCH** version (1.0.1): Bug fixes, backwards compatible

Examples:
- `v1.0.0` - Initial release
- `v1.1.0` - Added new features
- `v1.0.1` - Bug fixes
- `v2.0.0` - Breaking changes

### Manual Release (Alternative)

If you prefer to create releases manually:

1. **Build the plugin**:
   ```bash
   cd MapDecals
   dotnet build -c Release
   ```

2. **Publish with dependencies**:
   ```bash
   dotnet publish -c Release -o ./release
   ```

3. **Create ZIP package**:
   ```bash
   mkdir -p release-package/MapDecals
   cp -r release/* release-package/MapDecals/
   cp config.json release-package/MapDecals/
   cp ../README.md release-package/
   cp ../IMPLEMENTATION_NOTES.md release-package/
   cd release-package
   zip -r MapDecals-v1.0.0.zip .
   ```

4. **Upload to GitHub**:
   - Go to GitHub repository → Releases → "Draft a new release"
   - Create a new tag (e.g., `v1.0.0`)
   - Upload the ZIP file
   - Add release notes
   - Publish release

## Continuous Integration

### Pull Request Builds

Every pull request automatically triggers a CI build that:
- Builds the plugin in both Debug and Release configurations
- Checks for compilation errors
- Reports warnings (if any)
- Uploads build artifacts for 7 days

### Branch Protection

Consider enabling branch protection rules for `main` branch:
- Require status checks to pass before merging
- Require pull request reviews
- Require branches to be up to date before merging

## Testing Releases

Before creating an official release:

1. **Test locally**:
   - Build in Release mode
   - Test on a development CS2 server
   - Verify all features work correctly

2. **Create a pre-release**:
   - Use version like `v1.0.0-beta.1` or `v1.0.0-rc.1`
   - Mark as "Pre-release" on GitHub
   - Get community feedback

3. **Promote to stable**:
   - Once tested, create the stable release
   - Use clean version number (e.g., `v1.0.0`)

## Troubleshooting

### Build Fails in GitHub Actions

1. Check the Actions tab in GitHub repository
2. View the failed job logs
3. Common issues:
   - Missing dependencies in `.csproj`
   - Invalid YAML syntax in workflow files
   - Build errors not caught locally

### Release Not Created

1. Verify tag follows `v*` pattern (e.g., `v1.0.0`, not `1.0.0`)
2. Check GitHub Actions permissions:
   - Settings → Actions → General → Workflow permissions
   - Enable "Read and write permissions"
3. Verify `GITHUB_TOKEN` has proper permissions

### Package Missing Files

1. Check the publish output in GitHub Actions logs
2. Verify files exist in repository
3. Update workflow if additional files needed

## GitHub Actions Status Badges

Add to README.md to show build status:

```markdown
![CI Build](https://github.com/JonneKahvila/CS2-MapDecals/workflows/CI%20Build/badge.svg)
![Build and Release](https://github.com/JonneKahvila/CS2-MapDecals/workflows/Build%20and%20Release/badge.svg)
```

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/)
- [Creating Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
