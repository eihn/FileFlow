using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileFlow
{
    public partial class MainForm : Form
    {
        private FileSystemWatcher watcher;
        private ConfigManager configManager;
        private AppSettings settings;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private bool isPaused = false;
        private bool _startVisible = false; // For proper tray hiding
        private System.ComponentModel.IContainer components = new System.ComponentModel.Container();
        private FileSystemWatcher configWatcher;

        public MainForm()
        {
            InitializeComponents();
            ApplyDarkMode();
            SetupTrayIcon();
            InitializeApp();
            SetupConfigWatcher();
            this.Icon = LoadAppIcon();

            // Prevent closing, just hide to tray
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };
        }

        private void InitializeComponents()
        {
            // Form setup
            this.Text = "FileFlow – File Organizer";
            this.Size = new System.Drawing.Size(400, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;

            // Controls
            var label = new Label
            {
                Name = "statusLabel",
                Text = "Status: Running",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(360, 30),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var pauseBtn = new Button
            {
                Name = "pauseBtn",
                Text = "Pause",
                Location = new System.Drawing.Point(20, 70),
                Size = new System.Drawing.Size(100, 30)
            };
            pauseBtn.Click += (s, e) => TogglePause();

            var editBtn = new Button
            {
                Name = "editBtn",
                Text = "Edit Rules",
                Location = new System.Drawing.Point(150, 70),
                Size = new System.Drawing.Size(100, 30)
            };
            editBtn.Click += (s, e) => new ConfigEditorForm(configManager).ShowDialog(this);

            var closeBtn = new Button
            {
                Name = "closeBtn",
                Text = "Close",
                Location = new System.Drawing.Point(280, 70),
                Size = new System.Drawing.Size(80, 30)
            };
            closeBtn.Click += (s, e) => this.Hide();

            // 🔴 CRITICAL: Add controls to the form
            this.Controls.AddRange(new Control[] { label, pauseBtn, editBtn, closeBtn });
        }

        private void ApplyDarkMode()
        {
            Color bg = Color.FromArgb(32, 32, 32);
            Color fg = Color.FromArgb(220, 220, 220);
            Color btnBg = Color.FromArgb(48, 48, 48);

            this.BackColor = bg;
            this.ForeColor = fg;

            foreach (Control c in this.Controls)
            {
                if (c is Label)
                {
                    c.ForeColor = fg;
                    continue;
                }

                if (c is Button btn)
                {
                    btn.BackColor = btnBg;
                    btn.ForeColor = fg;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;

                    btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(0, 122, 204);
                    btn.MouseLeave += (s, e) => btn.BackColor = btnBg;
                }
            }
        }

        private void SetupTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Open", null, (s, e) => ShowWindow());
            trayMenu.Items.Add("Edit Rules", null, (s, e) => new ConfigEditorForm(configManager).ShowDialog(this));
            trayMenu.Items.Add("View Log", null, (s, e) => OpenLog());
            trayMenu.Items.Add(new ToolStripSeparator());
            var pauseItem = trayMenu.Items.Add("Pause", null, (s, e) => TogglePause());
            trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            trayIcon = new NotifyIcon(components)
            {
                Icon = LoadAppIcon(),
                Text = "FileFlow – File Organizer",
                Visible = true,
                ContextMenuStrip = trayMenu
            };

            trayIcon.DoubleClick += (s, e) => ShowWindow();
            UpdateTrayPauseText();
        }

        private void InitializeApp()
        {
            configManager = new ConfigManager();
            settings = AppSettings.Load();

            if (configManager.Config.CheckOnStartup)
                StartWatching();
            if (watcher != null)
                watcher.EnableRaisingEvents = !isPaused;
            UpdateStatusLabel();

            SetAutoStart(settings.AutoStart);
        }

        private void StartWatching()
        {
            string folder = configManager.Config.WatchFolder;
            if (!Directory.Exists(folder)) return;

            watcher = new FileSystemWatcher(folder);
            watcher.Created += OnFileCreated;
            watcher.EnableRaisingEvents = true;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (isPaused) return;

            try
            {
                System.Threading.Thread.Sleep(1000);

                foreach (var rule in configManager.Config.Rules)
                {
                    foreach (string ext in rule.Extensions)
                    {
                        if (e.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                        {
                            string dest = Path.Combine(rule.Destination, e.Name);
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));

                            if (File.Exists(dest))
                            {
                                DialogResult result = MessageBox.Show(
                                    $"File exists: {Path.GetFileName(dest)}\nReplace, Skip, or Rename?",
                                    "Conflict",
                                    MessageBoxButtons.YesNoCancel,
                                    MessageBoxIcon.Question
                                );

                                if (result == DialogResult.Yes)
                                {
                                    File.Delete(dest);
                                }
                                else if (result == DialogResult.Cancel)
                                {
                                    int i = 1;
                                    string dir = Path.GetDirectoryName(dest);
                                    string fn = Path.GetFileNameWithoutExtension(dest);
                                    string ext2 = Path.GetExtension(dest);
                                    string newDest = Path.Combine(dir, $"{fn}_{i}{ext2}");
                                    while (File.Exists(newDest)) newDest = Path.Combine(dir, $"{fn}_{++i}{ext2}");
                                    dest = newDest;
                                }
                                else
                                {
                                    return;
                                }
                            }

                            if (rule.Action?.ToLower() == "copy")
                            {
                                File.Copy(e.FullPath, dest);
                                Logger.Info($"Copied: {e.Name} → {dest}");
                            }
                            else
                            {
                                File.Move(e.FullPath, dest);
                                Logger.Info($"Moved: {e.Name} → {dest}");
                            }

                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Move failed: {ex.Message}");
            }
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
            Logger.Info("Main window opened from tray");
        }

        private void OpenLog()
        {
            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FileFlow.log");
            if (File.Exists(logPath))
                Process.Start("notepad.exe", logPath);
            else
                MessageBox.Show("No log file yet.", "Log Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void TogglePause()
        {
            isPaused = !isPaused;
            Logger.Info(isPaused ? "Paused by user" : "Resumed by user");
            UpdateTrayPauseText();
            UpdatePauseButtonText();
            UpdateStatusLabel();
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = !isPaused;
            }
        }

        private void UpdateStatusLabel()
        {
            var label = this.Controls["statusLabel"] as Label;
            if (label != null)
            {
                label.Text = isPaused ? "Status: Paused" : "Status: Running";
            }
        }

        private void UpdateTrayPauseText()
        {
            var item = trayMenu.Items[3];
            item.Text = isPaused ? "Resume" : "Pause";
        }

        private void UpdatePauseButtonText()
        {
            var btn = this.Controls["pauseBtn"] as Button;
            if (btn != null)
                btn.Text = isPaused ? "Resume" : "Pause";
        }

        private void SetAutoStart(bool enable)
        {
            string appName = "FileFlow";
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (enable)
                key.SetValue(appName, $"\"{Application.ExecutablePath}\"");
            else
                key.DeleteValue(appName, false);
        }

        private void ExitApplication()
        {
            trayIcon.Visible = false;
            Application.Exit();
        }

        // 🔧 Critical: Allows form to be hidden at startup but shown later
        protected override void SetVisibleCore(bool value)
        {
            if (!_startVisible)
            {
                base.SetVisibleCore(false);
                _startVisible = true;
            }
            else
            {
                base.SetVisibleCore(value);
            }
        }

        private void SetupConfigWatcher()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            string configDir = Path.GetDirectoryName(configPath);

            if (!Directory.Exists(configDir)) return;

            configWatcher = new FileSystemWatcher(configDir);
            configWatcher.Filter = "config.json";
            configWatcher.NotifyFilter = NotifyFilters.LastWrite;
            configWatcher.EnableRaisingEvents = true;

            configWatcher.Changed += (s, e) =>
            {
                // Debounce: wait a bit to avoid file lock
                System.Threading.Thread.Sleep(500);

                try
                {
                    if (File.Exists(e.FullPath))
                    {
                        // Reload config
                        configManager.LoadConfig();
                        Logger.Info("Configuration auto-reloaded from file change");
                        ShowReloadNotification();

                        // Optional: Restart watcher if rules changed
                        if (watcher != null)
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.Dispose();
                            StartWatching(); // Reapply any new folders
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to reload config: {ex.Message}");
                }
            };
        }
        private void ShowReloadNotification()
        {
            //trayIcon.BalloonTipTitle = "Config Updated";
            //trayIcon.BalloonTipText = "FileFlow has reloaded your rules.";
            //trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            //trayIcon.ShowBalloonTip(1000);
        }
        private Icon LoadAppIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                var names = assembly.GetManifestResourceNames();

                foreach (var name in names)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource: {name}");
                }

                var resourceName = "FileFlow.assets.logo.ico"; // ← Use the exact name from debug output

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    return stream != null ? new Icon(stream) : System.Drawing.SystemIcons.Shield;
                }
            }
            catch
            {
                return System.Drawing.SystemIcons.Shield;
            }
        }
    }
}