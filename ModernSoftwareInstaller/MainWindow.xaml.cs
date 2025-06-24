using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ModernWpf.Controls;

namespace ModernSoftwareInstaller
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<CheckBox, string> wingetPackages;

        public MainWindow()
        {
            InitializeComponent();
            wingetPackages = new Dictionary<CheckBox, string>
            {
                // Gaming-Stores
                { chkEpic, "EpicGames.EpicGamesLauncher" },
                { chkSteam, "Valve.Steam" },
                { chkEA, "ElectronicArts.EADesktop" },
                { chkUbisoft, "Ubisoft.Connect" },
                { chkBattlenet, "Blizzard.BattleNet" },

                // Browser
                { chkChrome, "Google.Chrome" },
                { chkFirefox, "Mozilla.Firefox" },
                { chkBrave, "Brave.Brave" },
                { chkEdge, "Microsoft.Edge" },
                { chkOpera, "Opera.Opera" },
                { chkLibreWolf, "LibreWolf.LibreWolf" },
                { chkTor, "TorProject.TorBrowser" },

                // Treiber
                { chkIntel, "Intel.IntelGraphicsDriver" },
                { chkAMD, "AdvancedMicroDevices.AMDSoftware" },
                { chkNvidia, "Nvidia.GeForceExperience" },

                // Software
                { chkVSCode, "Microsoft.VisualStudioCode" },
                { chkDiscord, "Discord.Discord" },
                { chkLively, "rocksdanister.LivelyWallpaper" },
                { chkCrystalDisk, "CrystalDewWorld.CrystalDiskMark" },
                { chkOBS, "OBSProject.OBSStudio" },
                { chkWinRAR, "RARLab.WinRAR" },
                { chkProtonVPN, "ProtonTechnologies.ProtonVPN" },
                { chkWireGuard, "WireGuard.WireGuard" },
                { chkSpotify, "Spotify.Spotify" },
                { chkNotepadPlus, "Notepad++.Notepad++" },
                { chkVisualStudio, "Microsoft.VisualStudio.2022.Community" },
                { chkPyCharm, "JetBrains.PyCharm.Community" }
            };
        }

        #region Tab Navigation
        private void InstallTab_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab("Install");
        }

        private void UpdateTab_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab("Update");
        }

        private void UninstallTab_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab("Uninstall");
        }

        private void PrereqTab_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab("Prereq");
        }

        private void SetActiveTab(string tab)
        {
            // Hide all content
            InstallContent.Visibility = Visibility.Collapsed;
            UpdateContent.Visibility = Visibility.Collapsed;
            UninstallContent.Visibility = Visibility.Collapsed;
            PrereqContent.Visibility = Visibility.Collapsed;

            // Reset button styles
            ResetTabButtonStyles();

            // Show selected content and highlight button
            switch (tab)
            {
                case "Install":
                    InstallContent.Visibility = Visibility.Visible;
                    btnInstallTab.Background = (System.Windows.Media.Brush)FindResource("CustomAccentBrush");
                    break;
                case "Update":
                    UpdateContent.Visibility = Visibility.Visible;
                    btnUpdateTab.Background = (System.Windows.Media.Brush)FindResource("CustomAccentBrush");
                    break;
                case "Uninstall":
                    UninstallContent.Visibility = Visibility.Visible;
                    btnUninstallTab.Background = (System.Windows.Media.Brush)FindResource("CustomAccentBrush");
                    break;
                case "Prereq":
                    PrereqContent.Visibility = Visibility.Visible;
                    btnPrereqTab.Background = (System.Windows.Media.Brush)FindResource("CustomAccentBrush");
                    break;
            }
        }

        private void ResetTabButtonStyles()
        {
            var surfaceBrush = (System.Windows.Media.Brush)FindResource("CustomSurfaceBrush");
            btnInstallTab.Background = surfaceBrush;
            btnUpdateTab.Background = surfaceBrush;
            btnUninstallTab.Background = surfaceBrush;
            btnPrereqTab.Background = surfaceBrush;
        }
        #endregion

        #region Installation Functions
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in wingetPackages.Keys) cb.IsChecked = true;
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in wingetPackages.Keys) cb.IsChecked = false;
        }

        private async void StartInstallation_Click(object sender, RoutedEventArgs e)
        {
            var selected = wingetPackages
                .Where(kv => kv.Key.IsChecked == true)
                .Select(kv => kv.Value)
                .ToList();

            if (!selected.Any())
            {
                await ShowMessageDialog("Keine Auswahl", "Bitte wählen Sie mindestens ein Programm aus.");
                return;
            }

            await ShowInstallationProgress(selected);
        }

        private async Task ShowInstallationProgress(List<string> packages)
        {
            var dialog = new ContentDialog
            {
                Title = "Installation läuft…",
                Content = new ModernWpf.Controls.ProgressBar { IsIndeterminate = true },
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            _ = dialog.ShowAsync();
            await InstallPackagesAsync(packages);
            dialog.Hide();

            await ShowMessageDialog("Fertig", "Die Installation der ausgewählten Programme wurde abgeschlossen.");
        }

        private async Task InstallPackagesAsync(IReadOnlyCollection<string> packages)
        {
            await Task.Run(() =>
            {
                var ids = string.Join(" ", packages.Select(p => $"--id \"{p}\""));
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"install {ids} --silent --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                try
                {
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(async () =>
                        await ShowMessageDialog("Fehler", $"Die Installation ist fehlgeschlagen:\n{ex.Message}"));
                }
            });
        }
        #endregion

        #region Profile Export
        private async void ExportProfile_Click(object sender, RoutedEventArgs e)
        {
            var selected = wingetPackages
                .Where(kv => kv.Key.IsChecked == true)
                .Select(kv => kv.Value)
                .ToList();

            if (!selected.Any())
            {
                await ShowMessageDialog("Keine Auswahl", "Bitte wählen Sie mindestens ein Programm aus.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "Batch Script (*.bat)|*.bat|JSON Profile (*.json)|*.json",
                DefaultExt = "bat",
                FileName = "software-installer"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    if (dialog.FileName.EndsWith(".bat"))
                    {
                        await ExportAsBatch(selected, dialog.FileName);
                    }
                    else
                    {
                        await ExportAsJson(selected, dialog.FileName);
                    }

                    await ShowMessageDialog("Export erfolgreich", $"Profil wurde gespeichert als:\n{dialog.FileName}");
                }
                catch (Exception ex)
                {
                    await ShowMessageDialog("Export-Fehler", $"Fehler beim Speichern:\n{ex.Message}");
                }
            }
        }

        private async Task ExportAsBatch(List<string> packages, string fileName)
        {
            var batchContent = "@echo off\n";
            batchContent += "echo Modern Software Installer - Batch Script\n";
            batchContent += "echo =====================================\n\n";
            batchContent += "REM Generiert am: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n\n";

            foreach (var package in packages)
            {
                batchContent += $"echo Installing {package}...\n";
                batchContent += $"winget install --id \"{package}\" --silent --accept-package-agreements --accept-source-agreements\n\n";
            }

            batchContent += "echo.\necho Installation abgeschlossen!\npause\n";

            await File.WriteAllTextAsync(fileName, batchContent);
        }

        private async Task ExportAsJson(List<string> packages, string fileName)
        {
            var profile = new
            {
                Name = "Modern Software Installer Profile",
                Created = DateTime.Now,
                Packages = packages
            };

            var jsonContent = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fileName, jsonContent);
        }
        #endregion

        #region Prerequisites Check
        private async void CheckPrerequisites_Click(object sender, RoutedEventArgs e)
        {
            PrereqStatus.Text = "🔍 Überprüfe System-Voraussetzungen...";

            await Task.Run(() =>
            {
                var results = new List<string>();

                // .NET Framework Check
                var dotNetVersions = GetInstalledDotNetVersions();
                results.Add($"📦 .NET Framework: {dotNetVersions}");

                // Visual C++ Redistributables Check
                var vcRedist = CheckVCRedistributables();
                results.Add($"🔧 Visual C++ Redistributables: {vcRedist}");

                // .NET Core/5+ Check
                var dotNetCore = CheckDotNetCore();
                results.Add($"⚡ .NET Core/5+: {dotNetCore}");

                Dispatcher.Invoke(() =>
                {
                    PrereqStatus.Text = string.Join("\n", results);
                });
            });
        }

        private string GetInstalledDotNetVersions()
        {
            var versions = new List<string>();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP");
                if (key != null)
                {
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        if (subkeyName.StartsWith("v"))
                        {
                            versions.Add(subkeyName);
                        }
                    }
                }
            }
            catch
            {
                return "❌ Fehler beim Überprüfen";
            }
            return versions.Any() ? $"✅ {string.Join(", ", versions)}" : "❌ Nicht gefunden";
        }

        private string CheckVCRedistributables()
        {
            var found = new List<string>();
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (key != null)
                {
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using var subkey = key.OpenSubKey(subkeyName);
                        var displayName = subkey?.GetValue("DisplayName") as string;
                        if (displayName?.Contains("Microsoft Visual C++") == true &&
                            displayName.Contains("Redistributable"))
                        {
                            found.Add(displayName);
                        }
                    }
                }
            }
            catch
            {
                return "❌ Fehler beim Überprüfen";
            }
            return found.Any() ? $"✅ {found.Count} gefunden" : "❌ Nicht gefunden";
        }

        private string CheckDotNetCore()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--list-runtimes",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                var output = process?.StandardOutput.ReadToEnd() ?? "";
                process?.WaitForExit();

                if (process?.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n').Length - 1;
                    return $"✅ {lines} Runtimes installiert";
                }
            }
            catch
            {
                // Ignored
            }
            return "❌ Nicht verfügbar";
        }
        #endregion

        #region Update Functions
        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            UpdatesList.Items.Clear();
            UpdatesList.Items.Add("🔍 Suche nach Updates...");

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    var output = process?.StandardOutput.ReadToEnd() ?? "";
                    process?.WaitForExit();

                    Dispatcher.Invoke(() =>
                    {
                        UpdatesList.Items.Clear();
                        if (!string.IsNullOrEmpty(output))
                        {
                            var lines = output.Split('\n');
                            foreach (var line in lines.Skip(2).Where(l => !string.IsNullOrWhiteSpace(l)))
                            {
                                UpdatesList.Items.Add(line.Trim());
                            }
                        }

                        // Add Update All button
                        var updateAllButton = new Button
                        {
                            Content = "🔄 Alle Updates installieren",
                            Background = (System.Windows.Media.Brush)FindResource("CustomAccentBrush"),
                            Foreground = System.Windows.Media.Brushes.White,
                            Padding = new Thickness(15, 10, 15, 10),
                            Margin = new Thickness(5)
                        };
                        updateAllButton.Click += UpdateAll_Click;
                        UpdatesList.Items.Insert(0, updateAllButton);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdatesList.Items.Clear();
                        UpdatesList.Items.Add($"❌ Fehler: {ex.Message}");
                    });
                }
            });
        }

        private async void UpdateAll_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Updates werden installiert…",
                Content = new ModernWpf.Controls.ProgressBar { IsIndeterminate = true },
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            _ = dialog.ShowAsync();

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "upgrade --all --silent --accept-package-agreements --accept-source-agreements",
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using var process = Process.Start(psi);
                    process?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(async () =>
                        await ShowMessageDialog("Fehler", $"Update fehlgeschlagen:\n{ex.Message}"));
                }
            });

            dialog.Hide();
            await ShowMessageDialog("Updates abgeschlossen", "Alle verfügbaren Updates wurden installiert.");
        }
        #endregion

        #region Uninstall Functions
        private async void LoadInstalledPrograms_Click(object sender, RoutedEventArgs e)
        {
            InstalledList.Items.Clear();
            InstalledList.Items.Add("🔍 Lade installierte Programme...");

            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "list --source winget",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(psi);
                    var output = process?.StandardOutput.ReadToEnd() ?? "";
                    process?.WaitForExit();

                    Dispatcher.Invoke(() =>
                    {
                        InstalledList.Items.Clear();
                        if (!string.IsNullOrEmpty(output))
                        {
                            var lines = output.Split('\n');
                            foreach (var line in lines.Skip(2).Where(l => !string.IsNullOrWhiteSpace(l)))
                            {
                                var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                                    stackPanel.Children.Add(new TextBlock
                                    {
                                        Text = line.Trim(),
                                        Width = 400,
                                        Foreground = (System.Windows.Media.Brush)FindResource("CustomTextBrush")
                                    });

                                    var uninstallBtn = new Button
                                    {
                                        Content = "🗑️ Entfernen",
                                        Background = System.Windows.Media.Brushes.DarkRed,
                                        Foreground = System.Windows.Media.Brushes.White,
                                        Padding = new Thickness(10, 5, 10, 5),
                                        Tag = parts[1] // Package ID
                                    };
                                    uninstallBtn.Click += UninstallPackage_Click;
                                    stackPanel.Children.Add(uninstallBtn);

                                    InstalledList.Items.Add(stackPanel);
                                }
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        InstalledList.Items.Clear();
                        InstalledList.Items.Add($"❌ Fehler: {ex.Message}");
                    });
                }
            });
        }

        private async void UninstallPackage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string packageId)
            {
                var result = await ShowConfirmDialog("Deinstallation bestätigen",
                    $"Möchten Sie '{packageId}' wirklich deinstallieren?");

                if (result)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "winget",
                                Arguments = $"uninstall --id \"{packageId}\" --silent",
                                UseShellExecute = true,
                                Verb = "runas"
                            };

                            using var process = Process.Start(psi);
                            process?.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(async () =>
                                await ShowMessageDialog("Fehler", $"Deinstallation fehlgeschlagen:\n{ex.Message}"));
                        }
                    });

                    await ShowMessageDialog("Deinstallation abgeschlossen", $"'{packageId}' wurde entfernt.");
                    LoadInstalledPrograms_Click(sender, e); // Refresh list
                }
            }
        }
        #endregion

        #region Dialog Helpers
        private static async Task ShowMessageDialog(string title, string message)
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK"
            };
            await dlg.ShowAsync();
        }

        private static async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Ja",
                CloseButtonText = "Nein"
            };
            var result = await dlg.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
        #endregion
    }
}
