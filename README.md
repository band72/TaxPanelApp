# MapSearchApp — Panel Grid Search

[![Download Latest](https://img.shields.io/badge/⬇%20Download-v1.0.3%20Installer-blue?style=for-the-badge)](https://github.com/band72/TaxPanelApp/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-informational?style=flat-square)](https://github.com/band72/TaxPanelApp)
[![Version](https://img.shields.io/badge/Version-1.0.3-green?style=flat-square)](https://github.com/band72/TaxPanelApp/releases)

A WPF desktop application for locating PLSS tax panel sections on a high-resolution county base map.  
Enter a 4-digit panel number → the app highlights the exact section on the map and runs an OCR readout.

**Repository:** https://github.com/band72/TaxPanelApp

---

## 📥 Download & Installation

### Option 1 — Installer (Recommended)
1. Go to the **[Releases page](https://github.com/band72/TaxPanelApp/releases/latest)**
2. Download **`MapSearchApp.1.0.3.exe`**
3. Run the installer — no .NET runtime required (fully self-contained)
4. A shortcut is created in the Start Menu; optional Desktop shortcut during install
5. Launch **MapSearchApp** from the Start Menu or Desktop

### Option 2 — Build from Source
```powershell
git clone https://github.com/band72/TaxPanelApp.git
cd TaxPanelApp
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
# Executable will be at: bin\Release\net8.0\win-x64\publish\MapSearchApp.exe
```

**Requirements (source build only):** .NET 8 SDK, Windows 10 build 19041+

---

## 🗺 User Manual

### 1 — Loading a Base Map
On first launch the app automatically loads the **Tile Map Index PDF** bundled in the `Tax-Maps\` folder.

To load a different map:
- Click **Base Map** in the toolbar
- Select any `.pdf`, `.png`, or `.jpg` file
- Large PDFs are rendered at 4000px wide for maximum optical zoom

### 2 — Searching for a Panel

| Step | Action |
|---|---|
| Type a 4-digit panel number into the **Panel Number** box | e.g. `6307` |
| Press **Enter** or click **Search** | |
| A **red bounding box** highlights the section on the map | |
| A detail window opens showing a zoomed crop + OCR text | |

### 3 — Panel Number Format

Panel numbers follow the PLSS grid coding used by the county:

```
R T SS
│ │ └─ Section  (01–36)
│ └─── Township (1=T2N, 2=T1N, 3=T1S, 4=T2S, 5=T3S, 6=T4S)
└───── Range    (3=R23E, 4=R24E, 5=R25E, 6=R26E, 7=R27E, 8=R28E, 9=R29E)
```

**Example:** `6307` → Range R26E, Township T1S, Section 7

| First digit (R) | Range |
|---|---|
| 3 | R23E |
| 4 | R24E |
| 5 | R25E |
| 6 | R26E |
| 7 | R27E |
| 8 | R28E |
| 9 | R29E |

| Second digit (T) | Township |
|---|---|
| 1 | T2N |
| 2 | T1N |
| 3 | T1S |
| 4 | T2S |
| 5 | T3S |
| 6 | T4S |

### 4 — Calibration Offsets

If the red highlight box doesn't align perfectly with your map, use the **Calibration Offsets** sliders in the toolbar:

| Slider | Effect |
|---|---|
| **Left** | Moves the grid origin right |
| **Top** | Moves the grid origin down |
| **Right** | Shrinks the grid from the right edge |
| **Bottom** | Shrinks the grid from the bottom edge |

Offsets are **automatically saved** to `offsets.txt` on close and reloaded on next launch.

### 5 — Reference Grid Overlay

Check **Show Reference Grid** to draw a visual township/section grid over the map.
- **Blue thick lines** = Township boundaries (every 6 sections)
- **Faint lines** = Individual section boundaries

### 6 — TIF Directory (Settings)

Click **⚙ Settings** to point the app at a folder containing `.TIF` map files for advanced lookups.  
The path is saved to `tif_directory.txt` in the app directory.

### 7 — Check for Updates

Click **↑ Updates** in the toolbar to check for a newer version.  
- The app fetches `version.json` from the GitHub repository
- If a newer version is available you'll be prompted to open the download page
- The button shows **Checking…** while the request is in flight and never freezes the UI

---

## 🔄 Update Manifest

The update checker reads:
```
https://raw.githubusercontent.com/band72/TaxPanelApp/main/version.json
```

Current `version.json` at the repo root:
```json
{
  "version": "1.0.3",
  "url": "https://github.com/band72/TaxPanelApp/releases/latest"
}
```

---

## 🛠 Developer Notes

### Build & Publish
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

### Compile Installer
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" MapSearchApp.iss
# Output: Installer\MapSearchApp.1.0.3.exe
```

### Version Bump Checklist
- [ ] `MapSearchApp.iss` → `AppVersion` and `OutputBaseFilename`
- [ ] `MainWindow.xaml.cs` → `CurrentVersion` constant
- [ ] `version.json` → `version` field
- [ ] Git tag + GitHub Release

### Error Reporting
Unhandled exceptions are automatically POSTed (fire-and-forget) to the configured back-end endpoint in `ErrorReporter.cs`:
```csharp
private const string ErrorEndpoint = "https://api.yourdomain.com/errors";
```
Payload includes: version, context, message, stack trace, OS, machine name, and UTC timestamp.

---

## Recent Changes (v1.0.3)

| Change | Detail |
|---|---|
| **App Icon** | Custom 256×256 `.ico` — embedded in exe, installer wizard, shortcuts, and Add/Remove Programs |
| **Installer** | No `.NET` prereq check — fully self-contained bundle |
| **↑ Updates button** | Toolbar button; HTTP manifest check; never blocks UI (Task.Run offload) |
| **Error Reporting** | Global `DispatcherUnhandledException` + `UnhandledException` + `UnobservedTaskException` hooks |
| **version.json** | Committed to repo root for live update notifications |
