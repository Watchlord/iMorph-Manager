/*
 * MIT License
 *
 * Copyright (c) 2026 Watchlord1225
 * Original author: Watchlord1225
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using CG.Web.MegaApiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IMorphMegaDownloader
{
    public class MainForm : Form
    {
        private const string MegaFolderLink = "https://mega.nz/folder/XQdwFJTR#X8VNWdap7eKtIvmPbpW6sA/folder/qEcAmZJQ";
        private const string SettingsFileName = "download_path.txt";
        private const string NoDirectorySet = "No directory set - Please select a download folder";
        private string downloadDirectory = string.Empty;

        private ListView fileListView = null!;
        private Button connectButton = null!;
        private Button downloadButton = null!;
        private Button launchIMorphButton = null!;
        private Button deleteIMorphFoldersButton = null!;
        private Label statusLabel = null!;
        private Label fileCountLabel = null!;
        private ProgressBar progressBar = null!;
        private TextBox latestFileInfo = null!;
        private Label downloadPathLabel = null!;
        private Button browseFolderButton = null!;

        // Region filter (Regular vs China)
        private RadioButton regularVersionRadio = null!;
        private RadioButton chinaVersionRadio = null!;

        // WoW version filter (client version)
        // Regular region: 1.x.x (Classic Era), 2.x.x (TBC Classic), 5.x.x (MoP Classic), 12.x.x (Retail)
        // China region: No WoW version filtering (only filters by China in name)
        private RadioButton classicEraVersionRadio = null!;  // 1.x.x (Classic Era) - for Regular
        private RadioButton tbcVersionRadio = null!;        // 2.x.x (TBC Classic) - for Regular
        private RadioButton mopVersionRadio = null!;         // 5.x.x (MoP Classic) - for Regular
        private RadioButton retailVersionRadio = null!;      // 12.x.x (Retail) - for Regular

        // iMorph type filter
        private RadioButton regularTypeRadio = null!;   // Base iMorph
        private RadioButton menuTypeRadio = null!;      // (Menu)
        private RadioButton netTypeRadio = null!;       // (Net)
        private MegaApiClient? client;
        private List<INode> allFiles; // All files before filtering
        private List<INode> files; // Filtered files
        private string? runiMorphExePath; // Path to RuniMorph.exe if found

        public MainForm()
        {
            // Load saved download directory (if any)
            downloadDirectory = LoadDownloadDirectory();
            
            InitializeComponent();
            allFiles = new List<INode>();
            files = new List<INode>();
            
            // Update UI based on whether directory is set
            UpdateUIForDirectoryState();
            
            // Check for RuniMorph.exe on startup (only if directory is set)
            if (IsDirectorySet())
            {
                CheckForRuniMorphExe();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "iMorph Manager v1.1.0 by Watchlord";
            this.Size = new System.Drawing.Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(800, 550);
            
            // Set application icon from PNG logo
            SetApplicationIcon();

            // Status Label
            statusLabel = new Label
            {
                Text = "Ready. Click 'Connect to Mega.nz' to load files.",
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10, 5, 10, 5),
                AutoSize = false
            };
            this.Controls.Add(statusLabel);

            // Filter Selection Panel
            Panel filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                Padding = new Padding(10, 5, 10, 5)
            };

            // Region Selection Panel (Regular vs China)
            Panel regionPanel = new Panel
            {
                Location = new System.Drawing.Point(0, 0),
                Size = new System.Drawing.Size(500, 25)
            };

            Label regionLabel = new Label
            {
                Text = "Region:",
                Location = new System.Drawing.Point(0, 8),
                AutoSize = true
            };
            regionPanel.Controls.Add(regionLabel);

            // Region radio buttons
            regularVersionRadio = new RadioButton
            {
                Text = "Regular (Non-China)",
                Location = new System.Drawing.Point(70, 6),
                AutoSize = true,
                Checked = true,
                TabStop = true
            };
            regularVersionRadio.CheckedChanged += Filter_CheckedChanged;
            regionPanel.Controls.Add(regularVersionRadio);

            chinaVersionRadio = new RadioButton
            {
                Text = "China/Chinese",
                Location = new System.Drawing.Point(220, 6),
                AutoSize = true,
                TabStop = true
            };
            chinaVersionRadio.CheckedChanged += Filter_CheckedChanged;
            regionPanel.Controls.Add(chinaVersionRadio);

            filterPanel.Controls.Add(regionPanel);

            // WoW Version Selection Panel (only for Regular region)
            // Regular region: 1.x.x (Classic Era), 2.x.x (TBC Classic), 5.x.x (MoP Classic), 12.x.x (Retail)
            // China region: No WoW version filtering
            Panel wowVersionPanel = new Panel
            {
                Location = new System.Drawing.Point(0, 30),
                Size = new System.Drawing.Size(650, 25)
            };

            Label wowVersionLabel = new Label
            {
                Text = "WoW Version:",
                Location = new System.Drawing.Point(0, 8),
                AutoSize = true
            };
            wowVersionPanel.Controls.Add(wowVersionLabel);

            classicEraVersionRadio = new RadioButton
            {
                Text = "1.x.x (Classic Era)",
                Location = new System.Drawing.Point(90, 6),
                AutoSize = true
            };
            classicEraVersionRadio.CheckedChanged += Filter_CheckedChanged;
            wowVersionPanel.Controls.Add(classicEraVersionRadio);

            tbcVersionRadio = new RadioButton
            {
                Text = "2.x.x (TBC Classic)",
                Location = new System.Drawing.Point(220, 6),
                AutoSize = true
            };
            tbcVersionRadio.CheckedChanged += Filter_CheckedChanged;
            wowVersionPanel.Controls.Add(tbcVersionRadio);

            mopVersionRadio = new RadioButton
            {
                Text = "5.x.x (MoP Classic)",
                Location = new System.Drawing.Point(360, 6),
                AutoSize = true
            };
            mopVersionRadio.CheckedChanged += Filter_CheckedChanged;
            wowVersionPanel.Controls.Add(mopVersionRadio);

            retailVersionRadio = new RadioButton
            {
                Text = "12.x.x (Retail)",
                Location = new System.Drawing.Point(500, 6),
                AutoSize = true,
                Checked = true
            };
            retailVersionRadio.CheckedChanged += Filter_CheckedChanged;
            wowVersionPanel.Controls.Add(retailVersionRadio);

            filterPanel.Controls.Add(wowVersionPanel);

            // Type Selection Panel (separate group for type radio buttons)
            Panel typePanel = new Panel
            {
                Location = new System.Drawing.Point(0, 60),
                Size = new System.Drawing.Size(400, 25)
            };

            Label typeLabel = new Label
            {
                Text = "Type:",
                Location = new System.Drawing.Point(0, 8),
                AutoSize = true
            };
            typePanel.Controls.Add(typeLabel);

            regularTypeRadio = new RadioButton
            {
                Text = "Regular (No Menu/Net)",
                Location = new System.Drawing.Point(70, 6),
                AutoSize = true,
                Checked = true
            };
            regularTypeRadio.CheckedChanged += Filter_CheckedChanged;
            typePanel.Controls.Add(regularTypeRadio);

            menuTypeRadio = new RadioButton
            {
                Text = "(Menu)",
                Location = new System.Drawing.Point(220, 6),
                AutoSize = true
            };
            menuTypeRadio.CheckedChanged += Filter_CheckedChanged;
            typePanel.Controls.Add(menuTypeRadio);

            netTypeRadio = new RadioButton
            {
                Text = "(Net)",
                Location = new System.Drawing.Point(320, 6),
                AutoSize = true
            };
            netTypeRadio.CheckedChanged += Filter_CheckedChanged;
            typePanel.Controls.Add(netTypeRadio);

            filterPanel.Controls.Add(typePanel);

            this.Controls.Add(filterPanel);

            // File Count Label
            fileCountLabel = new Label
            {
                Text = "Files: 0",
                Dock = DockStyle.Top,
                Height = 25,
                Padding = new Padding(10, 0, 10, 5),
                AutoSize = false
            };
            this.Controls.Add(fileCountLabel);

            // Download Path Selection Panel
            TableLayoutPanel downloadPathPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(10, 8, 10, 5)
            };
            downloadPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            downloadPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            downloadPathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            downloadPathPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label downloadPathLabelTitle = new Label
            {
                Text = "Download to:",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            downloadPathPanel.Controls.Add(downloadPathLabelTitle, 0, 0);

            downloadPathLabel = new Label
            {
                Text = IsDirectorySet() ? downloadDirectory : NoDirectorySet,
                AutoEllipsis = true,
                ForeColor = IsDirectorySet() ? System.Drawing.Color.Blue : System.Drawing.Color.Red,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Dock = DockStyle.Fill
            };
            downloadPathPanel.Controls.Add(downloadPathLabel, 1, 0);

            browseFolderButton = new Button
            {
                Text = "Browse...",
                Size = new System.Drawing.Size(80, 26),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Dock = DockStyle.Fill
            };
            browseFolderButton.Click += BrowseFolderButton_Click;
            downloadPathPanel.Controls.Add(browseFolderButton, 2, 0);

            this.Controls.Add(downloadPathPanel);

            // Recommendation Note
            Label recommendationLabel = new Label
            {
                Text = "⚠️ Recommendation: Delete existing iMorph folders and zip files before downloading the latest version to avoid conflicts.",
                Dock = DockStyle.Bottom,
                Height = 30,
                Padding = new Padding(10, 5, 10, 5),
                AutoSize = false,
                ForeColor = System.Drawing.Color.OrangeRed,
                Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic)
            };
            this.Controls.Add(recommendationLabel);

            // Latest File Info TextBox
            latestFileInfo = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Bottom,
                Height = 80,
                ScrollBars = ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 9F)
            };
            this.Controls.Add(latestFileInfo);

            // Progress Bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 25,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            this.Controls.Add(progressBar);

            // Button Panel
            Panel buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            connectButton = new Button
            {
                Text = "Connect to Mega.nz",
                Size = new System.Drawing.Size(150, 35),
                Location = new System.Drawing.Point(10, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Enabled = false
            };
            connectButton.Click += ConnectButton_Click;
            buttonPanel.Controls.Add(connectButton);

            downloadButton = new Button
            {
                Text = "Download Latest File",
                Size = new System.Drawing.Size(150, 35),
                Location = new System.Drawing.Point(170, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Enabled = false
            };
            downloadButton.Click += DownloadButton_Click;
            buttonPanel.Controls.Add(downloadButton);

            Button refreshButton = new Button
            {
                Text = "Refresh List",
                Size = new System.Drawing.Size(120, 35),
                Location = new System.Drawing.Point(330, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Enabled = false,
                Name = "refreshButton"
            };
            refreshButton.Click += RefreshButton_Click;
            buttonPanel.Controls.Add(refreshButton);

            deleteIMorphFoldersButton = new Button
            {
                Text = "Delete iMorph Items",
                Size = new System.Drawing.Size(140, 35),
                Location = new System.Drawing.Point(460, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Enabled = false,
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            deleteIMorphFoldersButton.FlatAppearance.BorderSize = 0;
            deleteIMorphFoldersButton.Click += DeleteIMorphFoldersButton_Click;
            buttonPanel.Controls.Add(deleteIMorphFoldersButton);

            launchIMorphButton = new Button
            {
                Text = "Launch iMorph",
                Size = new System.Drawing.Size(120, 35),
                Location = new System.Drawing.Point(610, 8),
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Enabled = false,
                Visible = false
            };
            launchIMorphButton.Click += LaunchIMorphButton_Click;
            buttonPanel.Controls.Add(launchIMorphButton);

            this.Controls.Add(buttonPanel);

            // File List View
            fileListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Sorting = SortOrder.Descending
            };

            fileListView.Columns.Add("File Name", 400);
            fileListView.Columns.Add("Size", 100);
            fileListView.Columns.Add("Date Added", 180);
            fileListView.Columns.Add("Type", 80);

            fileListView.DoubleClick += FileListView_DoubleClick;

            this.Controls.Add(fileListView);
        }

        private async void ConnectButton_Click(object? sender, EventArgs e)
        {
            try
            {
                connectButton.Enabled = false;
                downloadButton.Enabled = false;
                progressBar.Visible = true;
                statusLabel.Text = "Connecting to Mega.nz...";
                fileListView.Items.Clear();
                allFiles.Clear();
                files.Clear();

                await Task.Run(() =>
                {
                    client = new MegaApiClient();
                    client.LoginAnonymous();
                });

                statusLabel.Text = "Retrieving file list...";
                Uri folderUri = new Uri(MegaFolderLink);

                IEnumerable<INode> nodes = await Task.Run(() => client!.GetNodesFromLink(folderUri));

                // Store all files (before filtering - no exclusions)
                allFiles = nodes
                    .Where(n => n.Type == NodeType.File)
                    .OrderByDescending(n => n.CreationDate)
                    .ToList();

                this.Invoke(new Action(() =>
                {
                    ApplyFilters();
                    PopulateFileList();
                    fileCountLabel.Text = $"Files: {files.Count}";
                    statusLabel.Text = $"Successfully loaded {files.Count} files.";
                    connectButton.Enabled = true;
                    downloadButton.Enabled = files.Count > 0;
                    progressBar.Visible = false;

                    if (files.Count > 0)
                    {
                        UpdateLatestFileInfo();
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    statusLabel.Text = $"Error: {ex.Message}";
                    MessageBox.Show($"Error connecting to Mega.nz:\n\n{ex.Message}", "Connection Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    connectButton.Enabled = true;
                    progressBar.Visible = false;
                }));
            }
        }

        private void PopulateFileList()
        {
            fileListView.Items.Clear();
            fileListView.BeginUpdate();

            foreach (var file in files)
            {
                ListViewItem item = new ListViewItem(file.Name);
                item.SubItems.Add(FormatFileSize(file.Size));
                item.SubItems.Add(file.CreationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown");
                item.SubItems.Add(Path.GetExtension(file.Name).ToUpper().TrimStart('.'));
                item.Tag = file;
                fileListView.Items.Add(item);
            }

            fileListView.EndUpdate();
        }

        private void UpdateLatestFileInfo()
        {
            if (files.Count > 0)
            {
                var latestFile = files.First();
                latestFileInfo.Text = $"Latest File: {latestFile.Name}\r\n" +
                                     $"Size: {FormatFileSize(latestFile.Size)}\r\n" +
                                     $"Date Added: {latestFile.CreationDate:yyyy-MM-dd HH:mm:ss}\r\n" +
                                     $"Download Location: {Path.GetFullPath(downloadDirectory)}";
            }
        }

        private async void DownloadButton_Click(object? sender, EventArgs e)
        {
            if (!IsDirectorySet())
            {
                MessageBox.Show("Please select a download folder before downloading files.",
                    "Directory Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (files.Count == 0 || client == null)
            {
                MessageBox.Show("No files available to download.", "Download Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var latestFile = files.First();
            await DownloadFile(latestFile);
        }

        private async void FileListView_DoubleClick(object? sender, EventArgs e)
        {
            if (!IsDirectorySet())
            {
                MessageBox.Show("Please select a download folder before downloading files.",
                    "Directory Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (fileListView.SelectedItems.Count > 0 && client != null)
            {
                var selectedItem = fileListView.SelectedItems[0];
                if (selectedItem.Tag is INode file)
                {
                    await DownloadFile(file);
                }
            }
        }

        private async Task DownloadFile(INode file)
        {
            try
            {
                if (!Directory.Exists(downloadDirectory))
                {
                    Directory.CreateDirectory(downloadDirectory);
                }

                string destinationPath = Path.Combine(downloadDirectory, file.Name);

                if (File.Exists(destinationPath))
                {
                    var result = MessageBox.Show(
                        $"File '{file.Name}' already exists.\n\nDo you want to overwrite it?",
                        "File Exists",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        statusLabel.Text = "Download cancelled.";
                        return;
                    }
                }

                downloadButton.Enabled = false;
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;
                statusLabel.Text = $"Downloading: {file.Name}...";

                await Task.Run(() =>
                {
                    using (var stream = client!.Download(file))
                    using (var fileStream = new FileStream(destinationPath, FileMode.Create))
                    {
                        stream.CopyTo(fileStream);
                    }
                });

                statusLabel.Text = $"Download completed: {file.Name}";
                
                // Check if downloaded file is a ZIP and extract it
                if (Path.GetExtension(destinationPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    statusLabel.Text = $"Extracting: {file.Name}...";
                    progressBar.Visible = true;
                    
                    try
                    {
                        await Task.Run(() =>
                        {
                            ExtractZipFile(destinationPath, downloadDirectory);
                        });
                        
                        statusLabel.Text = $"Extraction completed: {file.Name}";
                    }
                    catch (Exception extractEx)
                    {
                        statusLabel.Text = $"Extraction error: {extractEx.Message}";
                        MessageBox.Show($"Error extracting zip file:\n\n{extractEx.Message}",
                            "Extraction Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                
                // Check for RuniMorph.exe after extraction
                CheckForRuniMorphExe();
                
                progressBar.Visible = false;
                downloadButton.Enabled = true;

                var openResult = MessageBox.Show(
                    $"File downloaded successfully!\n\nSaved to: {Path.GetFullPath(destinationPath)}\n\nOpen download folder?",
                    "Download Complete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (openResult == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(downloadDirectory));
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Download error: {ex.Message}";
                progressBar.Visible = false;
                downloadButton.Enabled = true;
                MessageBox.Show($"Error downloading file:\n\n{ex.Message}", "Download Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            if (!IsDirectorySet())
            {
                MessageBox.Show("Please select a download folder before refreshing the file list.",
                    "Directory Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (client != null)
            {
                ConnectButton_Click(sender, e);
            }
            else
            {
                MessageBox.Show("Please connect to Mega.nz first.", "Not Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BrowseFolderButton_Click(object? sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select download folder";
                folderDialog.SelectedPath = downloadDirectory;
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    downloadDirectory = folderDialog.SelectedPath;
                    
                    // Ensure the directory exists
                    EnsureDownloadDirectoryExists();
                    
                    // Save the selected path
                    SaveDownloadDirectory(downloadDirectory);
                    
                    // Update UI
                    UpdateUIForDirectoryState();
                    
                    // Check for RuniMorph.exe in the new directory
                    CheckForRuniMorphExe();
                    
                    // Update latest file info if files are loaded
                    if (files.Count > 0)
                    {
                        UpdateLatestFileInfo();
                    }
                }
            }
        }

        private void ExtractZipFile(string zipPath, string extractToDirectory)
        {
            // Get the base name of the zip file (without extension) for extraction folder
            string zipFileName = Path.GetFileNameWithoutExtension(zipPath);
            string extractPath = Path.Combine(extractToDirectory, zipFileName);

            // Create extraction directory if it doesn't exist
            if (!Directory.Exists(extractPath))
            {
                Directory.CreateDirectory(extractPath);
            }

            // Extract the zip file (overwrite existing files)
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
        }

        private void CheckForRuniMorphExe()
        {
            runiMorphExePath = null;
            
            if (!Directory.Exists(downloadDirectory))
            {
                UpdateLaunchButtonVisibility();
                return;
            }

            // Search for RuniMorph.exe in the download directory and subdirectories
            try
            {
                // First check the root download directory
                string rootPath = Path.Combine(downloadDirectory, "RuniMorph.exe");
                if (File.Exists(rootPath))
                {
                    runiMorphExePath = rootPath;
                    UpdateLaunchButtonVisibility();
                    return;
                }

                // Search in subdirectories
                var directories = Directory.GetDirectories(downloadDirectory, "*", SearchOption.AllDirectories);
                foreach (var dir in directories)
                {
                    string exePath = Path.Combine(dir, "RuniMorph.exe");
                    if (File.Exists(exePath))
                    {
                        runiMorphExePath = exePath;
                        UpdateLaunchButtonVisibility();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error searching for RuniMorph.exe: {ex.Message}");
            }

            UpdateLaunchButtonVisibility();
        }

        private void UpdateLaunchButtonVisibility()
        {
            if (launchIMorphButton != null)
            {
                bool shouldShow = !string.IsNullOrEmpty(runiMorphExePath) && File.Exists(runiMorphExePath);
                launchIMorphButton.Visible = shouldShow;
                launchIMorphButton.Enabled = shouldShow;
            }
        }

        private void LaunchIMorphButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(runiMorphExePath) || !File.Exists(runiMorphExePath))
            {
                MessageBox.Show("RuniMorph.exe not found. Please download and extract an iMorph zip file first.",
                    "iMorph Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                CheckForRuniMorphExe(); // Re-check
                return;
            }

            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = runiMorphExePath,
                    WorkingDirectory = Path.GetDirectoryName(runiMorphExePath),
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(processInfo);
                statusLabel.Text = "iMorph launched successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching iMorph:\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                statusLabel.Text = $"Error launching iMorph: {ex.Message}";
            }
        }

        private string GetSettingsFilePath()
        {
            // Store settings file in the application's local data folder
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IMorphDownloader");
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            return Path.Combine(appDataPath, SettingsFileName);
        }

        private string LoadDownloadDirectory()
        {
            string settingsPath = GetSettingsFilePath();
            
            try
            {
                if (File.Exists(settingsPath))
                {
                    string savedPath = File.ReadAllText(settingsPath, Encoding.UTF8).Trim();
                    if (!string.IsNullOrEmpty(savedPath) && Directory.Exists(savedPath))
                    {
                        return savedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading download directory: {ex.Message}");
            }

            // Return empty string if no saved path - user must select one
            return string.Empty;
        }

        private void SaveDownloadDirectory(string path)
        {
            try
            {
                string settingsPath = GetSettingsFilePath();
                File.WriteAllText(settingsPath, path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving download directory: {ex.Message}");
                MessageBox.Show($"Error saving download folder path:\n\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void EnsureDownloadDirectoryExists()
        {
            if (string.IsNullOrEmpty(downloadDirectory))
            {
                return; // No directory set yet
            }

            try
            {
                if (!Directory.Exists(downloadDirectory))
                {
                    Directory.CreateDirectory(downloadDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating download directory:\n\n{ex.Message}\n\nPlease select a different folder.",
                    "Directory Creation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                // Reset to empty if creation fails
                downloadDirectory = string.Empty;
                UpdateUIForDirectoryState();
            }
        }

        private bool IsDirectorySet()
        {
            return !string.IsNullOrEmpty(downloadDirectory) && Directory.Exists(downloadDirectory);
        }

        private void UpdateUIForDirectoryState()
        {
            bool directorySet = IsDirectorySet();
            
            // Update label
            if (downloadPathLabel != null)
            {
                downloadPathLabel.Text = directorySet ? downloadDirectory : NoDirectorySet;
                downloadPathLabel.ForeColor = directorySet ? System.Drawing.Color.Blue : System.Drawing.Color.Red;
            }
            
            // Enable/disable buttons
            if (connectButton != null)
            {
                connectButton.Enabled = directorySet;
            }
            
            // Enable delete button if directory is set
            if (deleteIMorphFoldersButton != null)
            {
                deleteIMorphFoldersButton.Enabled = directorySet;
            }
            
            // Download and refresh buttons stay disabled until files are loaded (handled elsewhere)
            // But we can enable refresh if directory is set and client is connected
            if (client != null && directorySet)
            {
                var refreshButton = this.Controls.Find("refreshButton", true).FirstOrDefault() as Button;
                if (refreshButton != null)
                {
                    refreshButton.Enabled = directorySet;
                }
            }
        }

        private bool IsChinaVersion(string fileName)
        {
            string lowerName = fileName.ToLowerInvariant();
            return lowerName.Contains("china") || lowerName.Contains("chinese");
        }

        private bool IsMenuType(string fileName)
        {
            string lowerName = fileName.ToLowerInvariant();
            return lowerName.Contains("(menu)");
        }

        private bool IsNetType(string fileName)
        {
            string lowerName = fileName.ToLowerInvariant();
            return lowerName.Contains("(net)");
        }

        private bool IsRegularType(string fileName)
        {
            return !IsMenuType(fileName) && !IsNetType(fileName);
        }

        private int? GetWowMajorVersion(string fileName)
        {
            try
            {
                int start = fileName.IndexOf('[');
                int end = fileName.IndexOf(']', start + 1);
                if (start >= 0 && end > start + 1)
                {
                    string inside = fileName.Substring(start + 1, end - start - 1);
                    var parts = inside.Split('.');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int major))
                    {
                        return major;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors and treat as unknown
            }
            return null;
        }

        private bool IsWowVersionMatch(string fileName, int targetMajor)
        {
            var major = GetWowMajorVersion(fileName);
            return major.HasValue && major.Value == targetMajor;
        }

        private void ApplyFilters()
        {
            if (allFiles == null || allFiles.Count == 0)
            {
                files = new List<INode>();
                return;
            }

            var filtered = allFiles.AsEnumerable();

            // Apply region filter (China vs Regular) - ensure one is always selected
            bool isChinaSelected = chinaVersionRadio != null && chinaVersionRadio.Checked;
            bool isRegularSelected = regularVersionRadio != null && regularVersionRadio.Checked;

            // If neither is selected (shouldn't happen), default to Regular
            if (!isChinaSelected && !isRegularSelected && regularVersionRadio != null)
            {
                regularVersionRadio.Checked = true;
                isRegularSelected = true;
            }

            if (isChinaSelected)
            {
                // China region: only filter by China in name, ignore WoW version filters
                filtered = filtered.Where(f => IsChinaVersion(f.Name));
            }
            else
            {
                // Regular region: exclude China versions
                filtered = filtered.Where(f => !IsChinaVersion(f.Name));

                // Apply WoW version filter for Regular (1.x.x Classic Era, 2.x.x TBC, 5.x.x MoP, 12.x.x Retail)
                bool classicEraSelected = classicEraVersionRadio != null && classicEraVersionRadio.Checked;
                bool tbcSelected = tbcVersionRadio != null && tbcVersionRadio.Checked;
                bool mopSelected = mopVersionRadio != null && mopVersionRadio.Checked;
                bool retailSelected = retailVersionRadio != null && retailVersionRadio.Checked;

                // Ensure at least one WoW version is selected, default to Retail
                if (!classicEraSelected && !tbcSelected && !mopSelected && !retailSelected && retailVersionRadio != null)
                {
                    retailVersionRadio.Checked = true;
                    retailSelected = true;
                }

                if (classicEraSelected)
                {
                    filtered = filtered.Where(f => IsWowVersionMatch(f.Name, 1));
                }
                else if (tbcSelected)
                {
                    filtered = filtered.Where(f => IsWowVersionMatch(f.Name, 2));
                }
                else if (mopSelected)
                {
                    filtered = filtered.Where(f => IsWowVersionMatch(f.Name, 5));
                }
                else // retailSelected
                {
                    filtered = filtered.Where(f => IsWowVersionMatch(f.Name, 12));
                }
            }

            // Apply type filter (Regular vs Menu vs Net)
            if (menuTypeRadio.Checked)
            {
                // Filter for (Menu) type
                filtered = filtered.Where(f => IsMenuType(f.Name));
            }
            else if (netTypeRadio.Checked)
            {
                // Filter for (Net) type
                filtered = filtered.Where(f => IsNetType(f.Name));
            }
            else // regularTypeRadio.Checked
            {
                // Filter for Regular type (no Menu/Net)
                filtered = filtered.Where(f => IsRegularType(f.Name));
            }

            files = filtered.ToList();
        }

        private void Filter_CheckedChanged(object? sender, EventArgs e)
        {
            // Ensure at least one version radio button is always selected
            if (sender == regularVersionRadio || sender == chinaVersionRadio)
            {
                // Version radio button changed - ensure one is always selected
                if (regularVersionRadio != null && chinaVersionRadio != null)
                {
                    if (!regularVersionRadio.Checked && !chinaVersionRadio.Checked)
                    {
                        // If somehow both are unchecked, default to Regular
                        regularVersionRadio.Checked = true;
                    }
                }
            }

            // Enable/disable WoW version radios based on region selection
            bool isChina = chinaVersionRadio != null && chinaVersionRadio.Checked;
            if (classicEraVersionRadio != null && tbcVersionRadio != null && mopVersionRadio != null && retailVersionRadio != null)
            {
                classicEraVersionRadio.Enabled = !isChina;
                tbcVersionRadio.Enabled = !isChina;
                mopVersionRadio.Enabled = !isChina;
                retailVersionRadio.Enabled = !isChina;
            }

            if (allFiles != null && allFiles.Count > 0)
            {
                // Apply all filters together (Region, WoW Version, Type)
                ApplyFilters();
                PopulateFileList();
                
                // Update file count with filter information for clarity
                string regionFilter = isChina ? "China" : "Regular";

                string wowFilter;
                if (isChina)
                {
                    wowFilter = "N/A (China region)";
                }
                else
                {
                    // Regular region WoW versions
                    if (classicEraVersionRadio != null && classicEraVersionRadio.Checked)
                        wowFilter = "1.x.x (Classic Era)";
                    else if (tbcVersionRadio != null && tbcVersionRadio.Checked)
                        wowFilter = "2.x.x (TBC Classic)";
                    else if (mopVersionRadio != null && mopVersionRadio.Checked)
                        wowFilter = "5.x.x (MoP Classic)";
                    else if (retailVersionRadio != null && retailVersionRadio.Checked)
                        wowFilter = "12.x.x (Retail)";
                    else
                        wowFilter = "Unknown";
                }

                string typeFilter = menuTypeRadio != null && menuTypeRadio.Checked ? "Menu" : 
                                   (netTypeRadio != null && netTypeRadio.Checked ? "Net" : "Regular");

                fileCountLabel.Text = $"Files: {files.Count} ({regionFilter} - {wowFilter} - {typeFilter})";
                
                if (files.Count > 0)
                {
                    UpdateLatestFileInfo();
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = true;
                    }
                }
                else
                {
                    latestFileInfo.Text = $"No files match the selected filters.\nRegion: {regionFilter}\nWoW Version: {wowFilter}\nType: {typeFilter}";
                    if (downloadButton != null)
                    {
                        downloadButton.Enabled = false;
                    }
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private void DeleteIMorphFoldersButton_Click(object? sender, EventArgs e)
        {
            if (!IsDirectorySet())
            {
                MessageBox.Show("Please select a download folder first.",
                    "Directory Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Find all iMorph-related folders and zip files
                var iMorphFolders = new List<string>();
                var iMorphZipFiles = new List<string>();
                
                if (Directory.Exists(downloadDirectory))
                {
                    // Find folders
                    var directories = Directory.GetDirectories(downloadDirectory);
                    foreach (var dir in directories)
                    {
                        string dirName = Path.GetFileName(dir).ToLowerInvariant();
                        // Check if folder name contains "imorph" (case-insensitive)
                        if (dirName.Contains("imorph"))
                        {
                            iMorphFolders.Add(dir);
                        }
                    }

                    // Find zip files - search for all .zip files and check if they contain "imorph"
                    try
                    {
                        var zipFiles = Directory.GetFiles(downloadDirectory, "*.zip", SearchOption.TopDirectoryOnly);
                        foreach (var zipFile in zipFiles)
                        {
                            string fileName = Path.GetFileName(zipFile);
                            string fileNameLower = fileName.ToLowerInvariant();
                            // Check if zip file name contains "imorph" (case-insensitive)
                            // This will match: iMorph, IMORPH, imorph, etc.
                            if (fileNameLower.Contains("imorph"))
                            {
                                iMorphZipFiles.Add(zipFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error searching for zip files: {ex.Message}");
                    }
                }

                int totalItems = iMorphFolders.Count + iMorphZipFiles.Count;
                if (totalItems == 0)
                {
                    MessageBox.Show("No iMorph folders or zip files found in the selected directory.",
                        "No Items Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Build confirmation message
                var itemsList = new List<string>();
                if (iMorphFolders.Count > 0)
                {
                    itemsList.Add($"Folders ({iMorphFolders.Count}):");
                    itemsList.AddRange(iMorphFolders.Select(f => $"  📁 {Path.GetFileName(f)}"));
                }
                if (iMorphZipFiles.Count > 0)
                {
                    itemsList.Add($"\nZip Files ({iMorphZipFiles.Count}):");
                    itemsList.AddRange(iMorphZipFiles.Select(f => $"  📦 {Path.GetFileName(f)}"));
                }

                string itemsListText = string.Join("\n", itemsList);
                var result = MessageBox.Show(
                    $"This will delete {totalItems} iMorph item(s):\n\n{itemsListText}\n\n" +
                    "This action cannot be undone. Continue?",
                    "Delete iMorph Folders and Zip Files",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    return;
                }

                // Delete folders and zip files
                int deletedFolders = 0;
                int deletedZipFiles = 0;
                List<string> failedItems = new List<string>();

                // Delete folders
                foreach (var folder in iMorphFolders)
                {
                    try
                    {
                        Directory.Delete(folder, true); // true = recursive delete
                        deletedFolders++;
                    }
                    catch (Exception ex)
                    {
                        failedItems.Add($"📁 {Path.GetFileName(folder)}: {ex.Message}");
                    }
                }

                // Delete zip files
                foreach (var zipFile in iMorphZipFiles)
                {
                    try
                    {
                        if (File.Exists(zipFile))
                        {
                            // Ensure file is not read-only
                            File.SetAttributes(zipFile, FileAttributes.Normal);
                            File.Delete(zipFile);
                            deletedZipFiles++;
                        }
                        else
                        {
                            failedItems.Add($"📦 {Path.GetFileName(zipFile)}: File not found");
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        failedItems.Add($"📦 {Path.GetFileName(zipFile)}: Access denied - {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        failedItems.Add($"📦 {Path.GetFileName(zipFile)}: File in use - {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        failedItems.Add($"📦 {Path.GetFileName(zipFile)}: {ex.Message}");
                    }
                }

                // Show results
                int totalDeleted = deletedFolders + deletedZipFiles;
                if (failedItems.Count == 0)
                {
                    string successMsg = $"Successfully deleted:\n" +
                                      $"  • {deletedFolders} folder(s)\n" +
                                      $"  • {deletedZipFiles} zip file(s)";
                    statusLabel.Text = $"Successfully deleted {totalDeleted} iMorph item(s).";
                    MessageBox.Show(successMsg,
                        "Deletion Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    string errorMsg = $"Deleted {totalDeleted} item(s) ({deletedFolders} folders, {deletedZipFiles} zip files).\n\n" +
                                    $"Failed to delete:\n\n" +
                                    string.Join("\n", failedItems);
                    statusLabel.Text = $"Partially completed: {totalDeleted} deleted, {failedItems.Count} failed.";
                    MessageBox.Show(errorMsg,
                        "Deletion Partially Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Re-check for RuniMorph.exe after deletion
                CheckForRuniMorphExe();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting iMorph folders:\n\n{ex.Message}",
                    "Deletion Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                statusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void SetApplicationIcon()
        {
            try
            {
                string logoPath = Path.Combine(Application.StartupPath, "iMorph Logo.ico");
                System.Drawing.Icon? icon = null;
                
                if (File.Exists(logoPath))
                {
                    icon = new System.Drawing.Icon(logoPath);
                }
                else
                {
                    // Try loading from embedded resources
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var resourceName = "IMorphMegaDownloader.iMorph Logo.ico";
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            icon = new System.Drawing.Icon(stream);
                        }
                    }
                }
                
                if (icon != null)
                {
                    this.Icon = icon;
                }
            }
            catch (Exception ex)
            {
                // If icon loading fails, continue without icon
                System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (client != null)
            {
                try
                {
                    client.Logout();
                }
                catch { }
            }
            base.OnFormClosing(e);
        }
    }
}
