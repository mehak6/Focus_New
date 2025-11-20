# Instructions to Create GitHub Release v1.4.0

## Option 1: Using GitHub Web Interface (Easiest)

1. **Go to GitHub Releases Page**
   - Visit: https://github.com/mehak6/Focus_New/releases
   - Click the **"Draft a new release"** button

2. **Configure the Release**
   - **Choose a tag**: Select `v1.4.0` from the dropdown (already created and pushed)
   - **Release title**: `Focus Voucher System v1.4.0`

3. **Add Release Description**
   Copy and paste the content from `RELEASE_NOTES_v1.4.0.md` file into the description box

4. **Upload Executable**
   - Click "Attach binaries by dropping them here or selecting them"
   - Upload: `FocusVoucherSystem.exe` from `d:\Projects\Focus\`

5. **Publish**
   - Click **"Publish release"** button

---

## Option 2: Install GitHub CLI Manually

1. **Download GitHub CLI**
   - Visit: https://cli.github.com/
   - Download the Windows installer (.msi file)
   - Run the installer

2. **Authenticate**
   Open Command Prompt or PowerShell and run:
   ```
   gh auth login
   ```
   Follow the prompts to authenticate with your GitHub account

3. **Create Release**
   Navigate to your project directory and run:
   ```
   cd d:\Projects\Focus
   gh release create v1.4.0 ./FocusVoucherSystem.exe --title "Focus Voucher System v1.4.0" --notes-file RELEASE_NOTES_v1.4.0.md
   ```

---

## Option 3: Using Git Bash (if available)

If you have Git Bash installed, you can manually install gh CLI:

```bash
# Download the latest release
curl -LO https://github.com/cli/cli/releases/latest/download/gh_windows_amd64.zip

# Extract
unzip gh_windows_amd64.zip

# Add to PATH or use directly
./gh_windows_amd64/bin/gh.exe auth login
./gh_windows_amd64/bin/gh.exe release create v1.4.0 ./FocusVoucherSystem.exe --title "Focus Voucher System v1.4.0" --notes-file RELEASE_NOTES_v1.4.0.md
```

---

## What's Already Done

✅ **Git Tag Created**: `v1.4.0` has been created and pushed to GitHub
✅ **Executable Built**: `FocusVoucherSystem.exe` is ready at `d:\Projects\Focus\`
✅ **Release Notes**: Complete release notes available in `RELEASE_NOTES_v1.4.0.md`

## Files Available

- **Executable**: `d:\Projects\Focus\FocusVoucherSystem.exe`
- **Release Notes**: `d:\Projects\Focus\RELEASE_NOTES_v1.4.0.md`
- **Git Tag**: `v1.4.0` (already on GitHub)

---

## Release Summary

**Version**: v1.4.0
**Tag**: v1.4.0
**Date**: November 19, 2025

**Key Features**:
- Transaction comparison feature with visual highlighting
- Fixed reports date filtering
- Fixed auto-backup status updates
- Fixed DatePicker input issues
- DD/MM/YYYY date format throughout application

**Recommended**: Use Option 1 (GitHub Web Interface) - it's the simplest and most reliable method.
