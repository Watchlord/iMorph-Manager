# IMorph Mega.nz File Downloader

A Windows Forms application that connects to a Mega.nz folder, lists files sorted by creation date, and allows downloading files while excluding files containing "(Net)" or "(Menu)" in their names.

## Features

- ✅ **Modern Windows Forms UI** - Easy-to-use graphical interface
- ✅ Connects to Mega.nz folder using anonymous access
- ✅ Lists all files sorted by creation date (newest first) in a sortable table
- ✅ Automatically filters out files with "(Net)" or "(Menu)" in the name
- ✅ Displays detailed file information (name, size, creation date, type)
- ✅ Download latest file with one click
- ✅ Double-click any file in the list to download it
- ✅ Progress indicators and status updates
- ✅ Automatic download folder management

## Requirements

- .NET 8.0 SDK or later
- Windows operating system
- Internet connection

## Building the Project

### Option 1: Using Build Scripts (Recommended)

**PowerShell:**
```powershell
.\build.ps1
```

**Command Prompt:**
```cmd
build.bat
```

The build scripts will:
- Restore NuGet packages
- Build the project in Release configuration
- Output the executable to `bin\Release\net6.0-windows\`

### Option 2: Manual Build

1. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

2. Build the project:
   ```bash
   dotnet build -c Release
   ```

3. The executable will be in: `bin\Release\net8.0-windows\IMorphMegaDownloader.exe`

## Running the Application

### From Build Output:
```bash
cd bin\Release\net8.0-windows
.\IMorphMegaDownloader.exe
```

### From Development:
```bash
dotnet run
```

## Usage

1. **Launch the application** - The UI will open with a file list view

2. **Click "Connect to Mega.nz"** - This will:
   - Connect to the Mega.nz folder
   - Retrieve and filter all files
   - Display them in the list sorted by date (newest first)

3. **View files** - The list shows:
   - File Name
   - Size (formatted)
   - Date Added
   - File Type

4. **Download files** - You can:
   - Click "Download Latest File" to download the most recent file
   - Double-click any file in the list to download it
   - Files are saved to the `Downloads` folder

5. **Refresh** - Click "Refresh List" to reload files from Mega.nz

## Project Structure

```
IMorphMegaDownloader/
├── Program.cs                    # Application entry point
├── MainForm.cs                   # Windows Forms UI and logic
├── IMorphMegaDownloader.csproj   # Project file with dependencies
├── build.ps1                     # PowerShell build script
├── build.bat                     # Batch build script
├── README.md                     # This file
└── Downloads/                    # Download directory (created automatically)
```

## UI Features

- **File List View**: Sortable table showing all available files
- **Status Bar**: Shows current operation status
- **File Count**: Displays total number of filtered files
- **Latest File Info**: Shows details about the most recent file
- **Progress Bar**: Visual feedback during operations
- **Download Button**: Quick access to download the latest file
- **Refresh Button**: Reload files from Mega.nz

## Dependencies

- **MegaApiClient** (v1.10.5) - C# library for accessing Mega.nz cloud storage
- **Windows Forms** - Built-in .NET Windows Forms framework

## Notes

- The program uses anonymous login, so no Mega.nz account is required
- Files are automatically filtered to exclude any containing "(Net)" or "(Menu)" in their names
- If a file already exists in the Downloads folder, you'll be prompted to overwrite it
- Downloaded files are saved to a `Downloads` folder in the project directory
- The UI is responsive and handles errors gracefully

## Troubleshooting

**Build Issues:**
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Make sure you're on Windows (required for Windows Forms)

**Connection Issues:**
- Check your internet connection
- Verify the Mega.nz folder link is still valid
- Ensure the folder is publicly accessible

**Runtime Issues:**
- If the UI doesn't appear, check that you have .NET 6.0 Runtime installed
- For permission errors, run as administrator if needed
