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
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private Button checkUpdateButton = null!;
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
            this.Text = "iMorph Manager v1.2.1 by Watchlord";
            this.Size = new System.Drawing.Size(920, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(920, 550);
            
            // Set application icon from PNG logo
            SetApplicationIcon();

            // Menu Strip with Help menu - must be added first to appear at top
            MenuStrip mainMenu = new MenuStrip
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Stretch = true
            };
            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem checkUpdateMenuItem = new ToolStripMenuItem("Check for Updates");
            checkUpdateMenuItem.Click += CheckUpdateButton_Click;
            helpMenu.DropDownItems.Add(checkUpdateMenuItem);
            mainMenu.Items.Add(helpMenu);
            this.MainMenuStrip = mainMenu;
            this.Controls.Add(mainMenu);
            mainMenu.BringToFront();

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

            checkUpdateButton = new Button
            {
                Text = "Check for Updates",
                Size = new System.Drawing.Size(140, 35),
                Location = new System.Drawing.Point(750, 8),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Visible = true
            };
            checkUpdateButton.Click += CheckUpdateButton_Click;
            buttonPanel.Controls.Add(checkUpdateButton);

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

        private string GetWowVersionFolderName()
        {
            // Check if China region is selected
            bool isChina = chinaVersionRadio != null && chinaVersionRadio.Checked;
            
            if (isChina)
            {
                return "China iMorph";
            }
            
            // Regular region - get WoW version folder name
            if (classicEraVersionRadio != null && classicEraVersionRadio.Checked)
            {
                return "Classic Era iMorph";
            }
            else if (tbcVersionRadio != null && tbcVersionRadio.Checked)
            {
                return "TBC Classic iMorph";
            }
            else if (mopVersionRadio != null && mopVersionRadio.Checked)
            {
                return "MoP Classic iMorph";
            }
            else if (retailVersionRadio != null && retailVersionRadio.Checked)
            {
                return "Retail iMorph";
            }
            
            // Default to Retail if somehow none is selected
            return "Retail iMorph";
        }

        private async Task DownloadFile(INode file)
        {
            try
            {
                if (!Directory.Exists(downloadDirectory))
                {
                    Directory.CreateDirectory(downloadDirectory);
                }

                // Get the WoW version-specific folder name
                string versionFolderName = GetWowVersionFolderName();
                string versionDirectory = Path.Combine(downloadDirectory, versionFolderName);
                
                // Create version-specific directory if it doesn't exist
                if (!Directory.Exists(versionDirectory))
                {
                    Directory.CreateDirectory(versionDirectory);
                }

                string destinationPath = Path.Combine(versionDirectory, file.Name);

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
                            // Extract to the version-specific directory (reuse variables from outer scope)
                            ExtractZipFile(destinationPath, versionDirectory);
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
                    // Open the version-specific folder (reuse variables from outer scope)
                    System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath(versionDirectory));
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

                // Check in version-specific folders first (most likely location)
                string[] versionFolders = { "Classic Era iMorph", "TBC Classic iMorph", "MoP Classic iMorph", "Retail iMorph", "China iMorph" };
                foreach (string versionFolder in versionFolders)
                {
                    string versionDir = Path.Combine(downloadDirectory, versionFolder);
                    if (Directory.Exists(versionDir))
                    {
                        // Search in this version folder and its subdirectories
                        var directories = Directory.GetDirectories(versionDir, "*", SearchOption.AllDirectories);
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
                        // Also check directly in the version folder
                        string versionExePath = Path.Combine(versionDir, "RuniMorph.exe");
                        if (File.Exists(versionExePath))
                        {
                            runiMorphExePath = versionExePath;
                            UpdateLaunchButtonVisibility();
                            return;
                        }
                    }
                }

                // Search in all other subdirectories (for backwards compatibility)
                var otherDirectories = Directory.GetDirectories(downloadDirectory, "*", SearchOption.AllDirectories);
                foreach (var dir in otherDirectories)
                {
                    // Skip version-specific folders (already checked)
                    string dirName = Path.GetFileName(dir);
                    if (versionFolders.Contains(dirName))
                    {
                        continue;
                    }
                    
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

        private Dictionary<string, string> FindInstalledIMorphVersions()
        {
            var installedVersions = new Dictionary<string, string>();
            
            if (!Directory.Exists(downloadDirectory))
            {
                return installedVersions;
            }

            // Map version folder names to display names
            var versionMap = new Dictionary<string, string>
            {
                { "Classic Era iMorph", "Classic Era (1.x.x)" },
                { "TBC Classic iMorph", "TBC Classic (2.x.x)" },
                { "MoP Classic iMorph", "MoP Classic (5.x.x)" },
                { "Retail iMorph", "Retail (12.x.x)" },
                { "China iMorph", "China" }
            };

            string[] versionFolders = { "Classic Era iMorph", "TBC Classic iMorph", "MoP Classic iMorph", "Retail iMorph", "China iMorph" };
            
            foreach (string versionFolder in versionFolders)
            {
                string versionDir = Path.Combine(downloadDirectory, versionFolder);
                if (Directory.Exists(versionDir))
                {
                    // Check directly in the version folder
                    string versionExePath = Path.Combine(versionDir, "RuniMorph.exe");
                    if (File.Exists(versionExePath))
                    {
                        if (versionMap.TryGetValue(versionFolder, out string? displayName))
                        {
                            installedVersions[displayName] = versionExePath;
                        }
                        continue;
                    }
                    
                    // Search in subdirectories
                    try
                    {
                        var directories = Directory.GetDirectories(versionDir, "*", SearchOption.AllDirectories);
                        foreach (var dir in directories)
                        {
                            string exePath = Path.Combine(dir, "RuniMorph.exe");
                            if (File.Exists(exePath))
                            {
                                if (versionMap.TryGetValue(versionFolder, out string? displayName))
                                {
                                    installedVersions[displayName] = exePath;
                                }
                                break; // Found one in this version folder, move to next
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors searching subdirectories
                    }
                }
            }
            
            // Also check root directory for backwards compatibility
            string rootPath = Path.Combine(downloadDirectory, "RuniMorph.exe");
            if (File.Exists(rootPath))
            {
                installedVersions["Root Directory"] = rootPath;
            }

            return installedVersions;
        }

        private void UpdateLaunchButtonVisibility()
        {
            if (launchIMorphButton != null)
            {
                // Show button if any version is installed
                var installedVersions = FindInstalledIMorphVersions();
                bool shouldShow = installedVersions.Count > 0;
                launchIMorphButton.Visible = shouldShow;
                launchIMorphButton.Enabled = shouldShow;
            }
        }

        private void LaunchIMorphButton_Click(object? sender, EventArgs e)
        {
            var installedVersions = FindInstalledIMorphVersions();
            
            if (installedVersions.Count == 0)
            {
                MessageBox.Show("No iMorph installations found. Please download and extract an iMorph zip file first.",
                    "iMorph Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                CheckForRuniMorphExe(); // Re-check
                return;
            }

            // If only one version is installed, launch it directly
            if (installedVersions.Count == 1)
            {
                var version = installedVersions.First();
                LaunchIMorphVersion(version.Value, version.Key);
                return;
            }

            // Show selection dialog for multiple versions
            using (Form selectionForm = new Form())
            {
                selectionForm.Text = "Select iMorph Version to Launch";
                selectionForm.Size = new System.Drawing.Size(350, 200);
                selectionForm.StartPosition = FormStartPosition.CenterParent;
                selectionForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                selectionForm.MaximizeBox = false;
                selectionForm.MinimizeBox = false;
                selectionForm.ShowInTaskbar = false;

                Label label = new Label
                {
                    Text = "Select which version of iMorph you want to launch:",
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(320, 20),
                    AutoSize = false
                };
                selectionForm.Controls.Add(label);

                ListBox versionListBox = new ListBox
                {
                    Location = new System.Drawing.Point(10, 35),
                    Size = new System.Drawing.Size(320, 100),
                    SelectionMode = SelectionMode.One
                };
                
                foreach (var version in installedVersions.Keys)
                {
                    versionListBox.Items.Add(version);
                }
                
                versionListBox.SelectedIndex = 0;
                selectionForm.Controls.Add(versionListBox);

                Button okButton = new Button
                {
                    Text = "Launch",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(175, 145),
                    Size = new System.Drawing.Size(75, 25)
                };
                selectionForm.Controls.Add(okButton);
                selectionForm.AcceptButton = okButton;

                Button cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(255, 145),
                    Size = new System.Drawing.Size(75, 25)
                };
                selectionForm.Controls.Add(cancelButton);
                selectionForm.CancelButton = cancelButton;

                if (selectionForm.ShowDialog(this) == DialogResult.OK && versionListBox.SelectedItem != null)
                {
                    string selectedVersion = versionListBox.SelectedItem.ToString()!;
                    if (installedVersions.TryGetValue(selectedVersion, out string? exePath))
                    {
                        LaunchIMorphVersion(exePath, selectedVersion);
                    }
                }
            }
        }

        private void LaunchIMorphVersion(string exePath, string versionName)
        {
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(processInfo);
                statusLabel.Text = $"iMorph ({versionName}) launched successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching iMorph ({versionName}):\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                statusLabel.Text = $"Error launching iMorph ({versionName}): {ex.Message}";
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
                // Map version folder names to display names
                var versionMap = new Dictionary<string, string>
                {
                    { "Classic Era iMorph", "Classic Era (1.x.x)" },
                    { "TBC Classic iMorph", "TBC Classic (2.x.x)" },
                    { "MoP Classic iMorph", "MoP Classic (5.x.x)" },
                    { "Retail iMorph", "Retail (12.x.x)" },
                    { "China iMorph", "China" }
                };

                string[] versionFolders = { "Classic Era iMorph", "TBC Classic iMorph", "MoP Classic iMorph", "Retail iMorph", "China iMorph" };
                
                // Find which version folders exist
                var availableVersions = new List<string>();
                foreach (string versionFolder in versionFolders)
                {
                    string versionDir = Path.Combine(downloadDirectory, versionFolder);
                    if (Directory.Exists(versionDir))
                    {
                        // Check if folder has any content (folders or zip files)
                        bool hasContent = false;
                        try
                        {
                            var subDirs = Directory.GetDirectories(versionDir);
                            var zipFiles = Directory.GetFiles(versionDir, "*.zip", SearchOption.TopDirectoryOnly);
                            hasContent = subDirs.Length > 0 || zipFiles.Length > 0;
                        }
                        catch { }
                        
                        if (hasContent)
                        {
                            availableVersions.Add(versionFolder);
                        }
                    }
                }

                if (availableVersions.Count == 0)
                {
                    MessageBox.Show("No iMorph folders or zip files found in the selected directory.",
                        "No Items Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // Show selection dialog
                List<string>? selectedVersions = ShowVersionSelectionDialog(availableVersions, versionMap);
                
                if (selectedVersions == null || selectedVersions.Count == 0)
                {
                    return; // User cancelled
                }

                bool deleteAll = selectedVersions.Contains("ALL");
                
                // Collect items to delete from selected versions
                var iMorphFolders = new List<string>();
                var iMorphZipFiles = new List<string>();
                
                if (deleteAll)
                {
                    // Delete from all available versions
                    selectedVersions = availableVersions.ToList();
                }

                foreach (string versionFolder in selectedVersions)
                {
                    string versionDir = Path.Combine(downloadDirectory, versionFolder);
                    if (Directory.Exists(versionDir))
                    {
                        // Find folders in this version directory
                        try
                        {
                            var subDirs = Directory.GetDirectories(versionDir);
                            foreach (var dir in subDirs)
                            {
                                string dirName = Path.GetFileName(dir).ToLowerInvariant();
                                if (dirName.Contains("imorph"))
                                {
                                    iMorphFolders.Add(dir);
                                }
                            }
                        }
                        catch { }

                        // Find zip files in this version directory
                        try
                        {
                            var zipFiles = Directory.GetFiles(versionDir, "*.zip", SearchOption.TopDirectoryOnly);
                            foreach (var zipFile in zipFiles)
                            {
                                string fileName = Path.GetFileName(zipFile);
                                string fileNameLower = fileName.ToLowerInvariant();
                                if (fileNameLower.Contains("imorph"))
                                {
                                    iMorphZipFiles.Add(zipFile);
                                }
                            }
                        }
                        catch { }
                    }
                }

                int totalItems = iMorphFolders.Count + iMorphZipFiles.Count;
                if (totalItems == 0)
                {
                    MessageBox.Show("No iMorph folders or zip files found in the selected version(s).",
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
                string selectedVersionsText = deleteAll ? "All versions" : string.Join(", ", selectedVersions.Select(v => versionMap.GetValueOrDefault(v, v)));
                var result = MessageBox.Show(
                    $"This will delete {totalItems} iMorph item(s) from:\n{selectedVersionsText}\n\n{itemsListText}\n\n" +
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

        private List<string>? ShowVersionSelectionDialog(List<string> availableVersions, Dictionary<string, string> versionMap)
        {
            using (Form selectionForm = new Form())
            {
                selectionForm.Text = "Select iMorph Versions to Delete";
                selectionForm.Size = new System.Drawing.Size(400, 300);
                selectionForm.StartPosition = FormStartPosition.CenterParent;
                selectionForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                selectionForm.MaximizeBox = false;
                selectionForm.MinimizeBox = false;
                selectionForm.ShowInTaskbar = false;

                Label label = new Label
                {
                    Text = "Select which version(s) of iMorph you want to delete:",
                    Location = new System.Drawing.Point(10, 10),
                    Size = new System.Drawing.Size(370, 20),
                    AutoSize = false
                };
                selectionForm.Controls.Add(label);

                CheckedListBox versionListBox = new CheckedListBox
                {
                    Location = new System.Drawing.Point(10, 35),
                    Size = new System.Drawing.Size(370, 180),
                    CheckOnClick = true
                };
                
                // Add "Delete All" option first
                versionListBox.Items.Add("Delete All", false);
                
                // Add available versions
                foreach (string versionFolder in availableVersions)
                {
                    string displayName = versionMap.GetValueOrDefault(versionFolder, versionFolder);
                    versionListBox.Items.Add(displayName, false);
                }
                
                selectionForm.Controls.Add(versionListBox);

                // Handle "Delete All" checkbox logic
                versionListBox.ItemCheck += (s, e) =>
                {
                    if (e.Index == 0) // "Delete All" is first item
                    {
                        if (e.NewValue == CheckState.Checked)
                        {
                            // Uncheck all other items
                            for (int i = 1; i < versionListBox.Items.Count; i++)
                            {
                                versionListBox.SetItemChecked(i, false);
                            }
                        }
                    }
                    else
                    {
                        // If any individual version is checked, uncheck "Delete All"
                        if (e.NewValue == CheckState.Checked)
                        {
                            versionListBox.SetItemChecked(0, false);
                        }
                    }
                };

                Button okButton = new Button
                {
                    Text = "Delete",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(230, 225),
                    Size = new System.Drawing.Size(75, 25)
                };
                selectionForm.Controls.Add(okButton);
                selectionForm.AcceptButton = okButton;

                Button cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(310, 225),
                    Size = new System.Drawing.Size(75, 25)
                };
                selectionForm.Controls.Add(cancelButton);
                selectionForm.CancelButton = cancelButton;

                if (selectionForm.ShowDialog(this) == DialogResult.OK)
                {
                    var selected = new List<string>();
                    
                    if (versionListBox.GetItemChecked(0))
                    {
                        // "Delete All" selected
                        selected.Add("ALL");
                    }
                    else
                    {
                        // Get selected individual versions
                        for (int i = 1; i < versionListBox.Items.Count; i++)
                        {
                            if (versionListBox.GetItemChecked(i))
                            {
                                // Map display name back to folder name
                                string displayName = versionListBox.Items[i].ToString()!;
                                string? folderName = versionMap.FirstOrDefault(x => x.Value == displayName).Key;
                                if (!string.IsNullOrEmpty(folderName))
                                {
                                    selected.Add(folderName);
                                }
                            }
                        }
                    }
                    
                    return selected;
                }
                
                return null; // User cancelled
            }
        }

        private async void CheckUpdateButton_Click(object? sender, EventArgs e)
        {
            try
            {
                checkUpdateButton.Enabled = false;
                statusLabel.Text = "Checking for updates...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                // Get current version from assembly
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.2.1";
                // Remove build and revision numbers if present (e.g., "1.2.0.0" -> "1.2.0")
                if (currentVersion.Split('.').Length > 3)
                {
                    var parts = currentVersion.Split('.');
                    currentVersion = $"{parts[0]}.{parts[1]}.{parts[2]}";
                }
                Version currentVer = new Version(currentVersion);

                // Check GitHub releases
                string latestVersion = await GetLatestVersionFromGitHub();
                
                if (string.IsNullOrEmpty(latestVersion))
                {
                    statusLabel.Text = "Unable to check for updates. Please try again later.";
                    MessageBox.Show("Unable to check for updates. Please check your internet connection and try again.",
                        "Update Check Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                Version latestVer = new Version(latestVersion);

                if (latestVer <= currentVer)
                {
                    statusLabel.Text = "You have the latest version installed.";
                    MessageBox.Show("Latest version already installed!",
                        "Up to Date",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                // New version available - ask user if they want to update
                var updateResult = MessageBox.Show(
                    $"A new version is available!\n\nCurrent version: {currentVersion}\nLatest version: {latestVersion}\n\nWould you like to download and install the update?",
                    "Update Available",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (updateResult != DialogResult.Yes)
                {
                    statusLabel.Text = "Update cancelled.";
                    return;
                }

                // Download and install update
                await DownloadAndInstallUpdate(latestVersion);
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Update check error: {ex.Message}";
                MessageBox.Show($"Error checking for updates:\n\n{ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                checkUpdateButton.Enabled = true;
                progressBar.Visible = false;
            }
        }

        private async Task<string> GetLatestVersionFromGitHub()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "iMorph-Manager");
                    string apiUrl = "https://api.github.com/repos/Watchlord/iMorph-Manager/releases/latest";
                    
                    string response = await client.GetStringAsync(apiUrl);
                    
                    // Parse JSON response
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        JsonElement root = doc.RootElement;
                        string tagName = root.GetProperty("tag_name").GetString() ?? "";
                        
                        // Remove 'v' prefix if present
                        if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            tagName = tagName.Substring(1);
                        }
                        
                        return tagName;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting latest version: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<string> GetLatestReleaseDownloadUrl()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "iMorph-Manager");
                    string apiUrl = "https://api.github.com/repos/Watchlord/iMorph-Manager/releases/latest";
                    
                    string response = await client.GetStringAsync(apiUrl);
                    
                    // Parse JSON response to get download URL
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        JsonElement root = doc.RootElement;
                        JsonElement assets = root.GetProperty("assets");
                        
                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            string name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                return asset.GetProperty("browser_download_url").GetString() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting download URL: {ex.Message}");
                return string.Empty;
            }
            
            return string.Empty;
        }

        private async Task DownloadAndInstallUpdate(string version)
        {
            try
            {
                statusLabel.Text = "Downloading update...";
                progressBar.Visible = true;
                progressBar.Style = ProgressBarStyle.Marquee;

                // Get download URL
                string downloadUrl = await GetLatestReleaseDownloadUrl();
                if (string.IsNullOrEmpty(downloadUrl))
                {
                    MessageBox.Show("Unable to get download URL. Please try again later.",
                        "Update Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Get application directory
                string appDirectory = Path.GetDirectoryName(Application.ExecutablePath) ?? Application.StartupPath;
                string tempZipPath = Path.Combine(Path.GetTempPath(), $"iMorphManager_Update_{version}.zip");
                string tempExtractPath = Path.Combine(Path.GetTempPath(), $"iMorphManager_Update_{version}_Extract");

                // Download the update
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "iMorph-Manager");
                    byte[] fileData = await client.GetByteArrayAsync(downloadUrl);
                    await File.WriteAllBytesAsync(tempZipPath, fileData);
                }

                statusLabel.Text = "Extracting update...";

                // Extract to temp directory
                if (Directory.Exists(tempExtractPath))
                {
                    Directory.Delete(tempExtractPath, true);
                }
                Directory.CreateDirectory(tempExtractPath);
                ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath, overwriteFiles: true);

                statusLabel.Text = "Installing update...";

                // Close the application gracefully
                // We'll use a batch file to handle the replacement and restart
                string batchFile = Path.Combine(Path.GetTempPath(), "iMorphManager_Update.bat");
                string exeName = Path.GetFileName(Application.ExecutablePath);
                string exePath = Application.ExecutablePath;

                // Create batch script to replace files and restart
                StringBuilder batchScript = new StringBuilder();
                batchScript.AppendLine("@echo off");
                batchScript.AppendLine("timeout /t 2 /nobreak >nul");
                batchScript.AppendLine($"taskkill /F /IM \"{exeName}\" >nul 2>&1");
                batchScript.AppendLine("timeout /t 1 /nobreak >nul");
                
                // Copy all files from temp extract to app directory
                batchScript.AppendLine($"xcopy /E /Y /I \"{tempExtractPath}\\*\" \"{appDirectory}\"");
                
                // Clean up temp files
                batchScript.AppendLine($"del /F /Q \"{tempZipPath}\"");
                batchScript.AppendLine($"rmdir /S /Q \"{tempExtractPath}\"");
                batchScript.AppendLine($"del /F /Q \"%~f0\"");
                
                // Restart the application
                batchScript.AppendLine($"start \"\" \"{exePath}\"");
                
                File.WriteAllText(batchFile, batchScript.ToString());

                // Show completion message
                MessageBox.Show("iMorph Manager update complete!",
                    "Update Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Start the batch file and close the application
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batchFile,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Update error: {ex.Message}";
                MessageBox.Show($"Error installing update:\n\n{ex.Message}",
                    "Update Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
