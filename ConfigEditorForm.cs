using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

public partial class ConfigEditorForm : Form
{
    private ConfigManager configManager;
    private ListBox rulesList;
    private Button addButton, removeButton, saveButton, browseButton, testButton, browseSourceButton;
    private TextBox extBox, destBox, sourceBox;

    public ConfigEditorForm(ConfigManager configManager)
    {
        this.configManager = configManager;
        InitializeComponent();
        ApplyDarkMode();
        LoadRules();
    }

    private void InitializeComponent()
    {
        this.Text = "Edit Rules";
        this.Size = new System.Drawing.Size(500, 450);

        rulesList = new ListBox { Location = new System.Drawing.Point(20, 20), Size = new System.Drawing.Size(440, 120) };

        Label extLabel = new Label { Text = "Extensions (comma separated):", Location = new System.Drawing.Point(20, 150), AutoSize = true };
        extBox = new TextBox { Location = new System.Drawing.Point(20, 170), Size = new System.Drawing.Size(440, 30) };
        extBox.Text = ".txt,.log";

        Label sourceLabel = new Label { Text = "Source Folder:", Location = new System.Drawing.Point(20, 210), AutoSize = true };
        sourceBox = new TextBox { Location = new System.Drawing.Point(20, 230), Size = new System.Drawing.Size(350, 30) };
        browseSourceButton = new Button { Text = "...", Location = new System.Drawing.Point(380, 230), Size = new System.Drawing.Size(80, 30) };
        browseSourceButton.Click += BrowseSourceFolder;

        Label destLabel = new Label { Text = "Destination Folder:", Location = new System.Drawing.Point(20, 270), AutoSize = true };
        destBox = new TextBox { Location = new System.Drawing.Point(20, 290), Size = new System.Drawing.Size(350, 30) };
        browseButton = new Button { Text = "...", Location = new System.Drawing.Point(380, 290), Size = new System.Drawing.Size(80, 30) };
        browseButton.Click += BrowseFolder;

        addButton = new Button { Text = "Add Rule", Location = new System.Drawing.Point(20, 330), Size = new System.Drawing.Size(100, 30) };
        removeButton = new Button { Text = "Remove Selected", Location = new System.Drawing.Point(130, 330), Size = new System.Drawing.Size(120, 30) };
        testButton = new Button { Text = "Test Rule", Location = new System.Drawing.Point(260, 330), Size = new System.Drawing.Size(90, 30) };
        saveButton = new Button { Text = "Save & Close", Location = new System.Drawing.Point(360, 330), Size = new System.Drawing.Size(100, 30) };

        addButton.Click += AddRule;
        removeButton.Click += RemoveRule;
        testButton.Click += TestRule;
        saveButton.Click += SaveAndClose;
        browseButton.Click += BrowseFolder;

        this.Controls.AddRange(new Control[] {
            rulesList, extLabel, extBox, sourceLabel, sourceBox, browseSourceButton, destLabel, destBox,
            browseButton, addButton, removeButton, testButton, saveButton
        });
    }

    private void LoadRules()
    {
        rulesList.Items.Clear();
        foreach (var rule in configManager.Config.Rules)
        {
            string exts = string.Join(", ", rule.Extensions);
            rulesList.Items.Add($"📄 {exts} | {rule.Source} → {rule.Destination} ({rule.Action})");
        }
    }

    private void AddRule(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(destBox.Text) || !Directory.Exists(destBox.Text))
        {
            MessageBox.Show("Please select a valid destination folder.");
            return;
        }
        if (string.IsNullOrWhiteSpace(sourceBox.Text) || !Directory.Exists(sourceBox.Text))
        {
            MessageBox.Show("Please select a valid source folder.");
            return;
        }

        var extensions = extBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var cleanExts = new List<string>();

        foreach (var ext in extensions)
        {
            string trimmed = ext.Trim().ToLower();
            if (!trimmed.StartsWith("."))
                trimmed = "." + trimmed;
            cleanExts.Add(trimmed);
        }

        configManager.Config.Rules.Add(new FileRule
        {
            Extensions = cleanExts,
            Source = sourceBox.Text,
            Destination = destBox.Text,
            Action = "move"
        });

        LoadRules();
        extBox.Text = "";
        destBox.Text = "";
        sourceBox.Text = "";
    }

    private void RemoveRule(object sender, EventArgs e)
    {
        if (rulesList.SelectedIndex == -1)
        {
            MessageBox.Show("Please select a rule to remove.");
            return;
        }

        configManager.Config.Rules.RemoveAt(rulesList.SelectedIndex);
        LoadRules();
    }

    private void BrowseFolder(object sender, EventArgs e)
    {
        using (var fbd = new FolderBrowserDialog())
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                destBox.Text = fbd.SelectedPath;
            }
        }
    }

    private void BrowseSourceFolder(object sender, EventArgs e)
    {
        using (var fbd = new FolderBrowserDialog())
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                sourceBox.Text = fbd.SelectedPath;
            }
        }
    }

    private void TestRule(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(extBox.Text) || string.IsNullOrWhiteSpace(destBox.Text))
        {
            MessageBox.Show("Please enter extensions and destination to test.");
            return;
        }

        string fakeFile = "test" + extBox.Text.Split(',')[0].Trim();
        string dest = Path.Combine(destBox.Text, fakeFile);

        DialogResult result = MessageBox.Show(
            $"Would move:\n{fakeFile}\n→\n{dest}\n\nDoes this look correct?",
            "Test Rule",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Logger.Info($"Tested rule: {fakeFile} → {dest}");
            MessageBox.Show("Rule test passed! Ready to save.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SaveAndClose(object sender, EventArgs e)
    {
        configManager.SaveConfig();
        Logger.Info("Configuration saved from UI");
        MessageBox.Show("Rules saved!", "Saved");
        this.DialogResult = DialogResult.OK;
        this.Close();
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
            if (c is Label || c is Button) continue;
            c.BackColor = btnBg;
            c.ForeColor = fg;
        }

        addButton.BackColor = Color.FromArgb(0, 122, 204); addButton.ForeColor = Color.White;
        removeButton.BackColor = Color.FromArgb(204, 69, 0); removeButton.ForeColor = Color.White;
        testButton.BackColor = Color.FromArgb(255, 128, 0); testButton.ForeColor = Color.White;
        saveButton.BackColor = Color.FromArgb(0, 192, 0); saveButton.ForeColor = Color.White;
    }
}