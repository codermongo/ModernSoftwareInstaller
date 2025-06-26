using Microsoft.Win32;
using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ModernSoftwareInstaller
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<CheckBox, string> wingetPackages;
        private Action<string> _currentUpdateAction;

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

            Loaded += async (s, e) => await CheckWinGetOnStartup();
        }

        private async Task CheckWinGetOnStartup()
        {
            if (!await IsWinGetInstalled())
            {
                var result = await ShowConfirmDialog("WinGet nicht gefunden",
                    "WinGet ist nicht auf diesem System installiert. Ohne WinGet können keine Programme installiert werden.\n\nMöchten Sie WinGet jetzt installieren?");

                if (result)
                {
                    await EnsureWinGetInstalled();
                }
            }
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

        #region WinGet Installation Check
        private async Task<bool> EnsureWinGetInstalled()
        {
            if (await IsWinGetInstalled())
            {
                return true;
            }

            var installDialog = new ContentDialog
            {
                Title = "WinGet Installation",
                Content = "WinGet ist nicht installiert. Soll es jetzt installiert werden?",
                PrimaryButtonText = "Ja, installieren",
                CloseButtonText = "Abbrechen"
            };

            var result = await installDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return false;
            }

            var progressDialog = new ContentDialog
            {
                Title = "WinGet wird installiert...",
                Content = new ModernWpf.Controls.ProgressBar { IsIndeterminate = true },
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            _ = progressDialog.ShowAsync();

            bool installSuccess = await InstallWinGet();
            progressDialog.Hide();

            if (installSuccess)
            {
                await ShowMessageDialog("Installation erfolgreich", "WinGet wurde erfolgreich installiert.");
                return true;
            }
            else
            {
                await ShowMessageDialog("Installation fehlgeschlagen", "WinGet konnte nicht installiert werden. Bitte installieren Sie es manuell.");
                return false;
            }
        }

        private async Task<bool> IsWinGetInstalled()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                // WinGet not found
            }
            return false;
        }

        private async Task<bool> InstallWinGet()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ms-appinstaller:?source=https://aka.ms/getwinget",
                    UseShellExecute = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    await Task.Delay(5000);
                    return await IsWinGetInstalled();
                }
            }
            catch
            {
                try
                {
                    var psScript = @"
                $progressPreference = 'silentlyContinue'
                $latest = Invoke-RestMethod -Uri 'https://api.github.com/repos/microsoft/winget-cli/releases/latest'
                $latestVersion = $latest.tag_name
                $url = $latest.assets | Where-Object { $_.name -match 'msixbundle' } | Select-Object -ExpandProperty browser_download_url
                $output = '$env:TEMP\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle'
                Invoke-WebRequest -Uri $url -OutFile $output
                Add-AppxPackage $output
            ";

                    var psi2 = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -Command \"{psScript}\"",
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    using var process2 = Process.Start(psi2);
                    if (process2 != null)
                    {
                        await process2.WaitForExitAsync();
                        await Task.Delay(3000);
                        return await IsWinGetInstalled();
                    }
                }
                catch
                {
                    // PowerShell method also failed
                }
            }
            return false;
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
            // First ensure WinGet is installed
            if (!await EnsureWinGetInstalled())
            {
                return;
            }

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
            var progressBar = new ModernWpf.Controls.ProgressBar
            {
                Maximum = packages.Count,
                Value = 0
            };

            var statusText = new TextBlock
            {
                Text = "Bereite Installation vor...",
                Margin = new Thickness(0, 10, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            var dialog = new ContentDialog
            {
                Title = "Installation läuft…",
                Content = new StackPanel
                {
                    Children = { progressBar, statusText }
                },
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            _ = dialog.ShowAsync();

            // Status-Update-Funktion für Fortschritt
            int currentPackage = 0;
            Action<string> updateStatus = (status) =>
            {
                Dispatcher.Invoke(() =>
                {
                    statusText.Text = status;
                    progressBar.Value = currentPackage++;
                });
            };

            try
            {
                // Globale Update-Funktion setzen
                _currentUpdateAction = updateStatus;

                await InstallPackagesAsync(packages);
                dialog.Hide();

                await ShowMessageDialog("Installation erfolgreich",
                    "Alle Programme wurden erfolgreich installiert!");
            }
            catch (InstallationSummaryException ex)
            {
                dialog.Hide();

                // Detaillierte Ergebnis-Anzeige
                var resultDialog = new ContentDialog
                {
                    Title = ex.SuccessCount > 0 ? "Installation teilweise erfolgreich" : "Installation fehlgeschlagen",
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = ex.Message,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new System.Windows.Media.FontFamily("Consolas")
                        },
                        MaxHeight = 300
                    },
                    CloseButtonText = "OK"
                };

                await resultDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                dialog.Hide();
                await ShowMessageDialog("Installation fehlgeschlagen",
                    $"Unerwarteter Fehler:\n{ex.Message}");
            }
        }

        private async Task InstallPackagesAsync(IReadOnlyCollection<string> packages)
        {
            var packageList = packages.ToList();
            var successCount = 0;
            var failedPackages = new List<string>();

            for (int i = 0; i < packageList.Count; i++)
            {
                var package = packageList[i];
                try
                {
                    UpdateInstallationStatus($"Installiere {package} ({i + 1}/{packageList.Count})...");
                    await InstallSinglePackage(package);
                    successCount++;

                    // Kurze Pause zwischen Installationen
                    if (i < packageList.Count - 1)
                    {
                        await Task.Delay(1500);
                    }
                }
                catch (Exception ex)
                {
                    failedPackages.Add($"{package}: {ex.Message}");
                }
            }

            // Ergebnis-Summary anzeigen
            var summary = $"Installation abgeschlossen!\n\n";
            summary += $"✅ Erfolgreich: {successCount}/{packageList.Count}\n";

            if (failedPackages.Any())
            {
                summary += $"❌ Fehlgeschlagen: {failedPackages.Count}\n\n";
                summary += "Fehlgeschlagene Pakete:\n";
                summary += string.Join("\n", failedPackages.Take(5)); // Nur erste 5 zeigen

                if (failedPackages.Count > 5)
                {
                    summary += $"\n... und {failedPackages.Count - 5} weitere";
                }
            }

            throw new InstallationSummaryException(summary, successCount, failedPackages.Count);
        }

        private async Task InstallSinglePackage(string packageId)
        {
            await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install --id \"{packageId}\" --silent --accept-package-agreements --accept-source-agreements --disable-interactivity",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        Verb = null
                    };

                    using var process = Process.Start(psi);
                    if (process == null)
                        throw new Exception("Prozess konnte nicht gestartet werden");

                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        var errorMessage = !string.IsNullOrEmpty(error) ? error : output;
                        throw new Exception($"Exit Code: {process.ExitCode}. {errorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Installation fehlgeschlagen: {ex.Message}");
                }
            });
        }

        private void UpdateInstallationStatus(string status)
        {
            _currentUpdateAction?.Invoke(status);
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

                // WinGet prüfen
                Task.Run(async () =>
                {
                    if (await IsWinGetInstalled())
                        results.Add("✅ WinGet ist installiert");
                    else
                        results.Add("❌ WinGet ist nicht installiert");
                }).Wait();

                // Administrator-Rechte prüfen
                var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator);

                if (isAdmin)
                    results.Add("✅ Administrator-Rechte verfügbar");
                else
                    results.Add("❌ Administrator-Rechte erforderlich");

                // Internet-Verbindung prüfen
                try
                {
                    using var client = new System.Net.WebClient();
                    client.DownloadString("https://www.google.com");
                    results.Add("✅ Internet-Verbindung verfügbar");
                }
                catch
                {
                    results.Add("❌ Keine Internet-Verbindung");
                }

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

        #region Enhanced Uninstall Functions
        private readonly ObservableCollection<InstalledProgram> _allPrograms = new();
        private readonly ObservableCollection<InstalledProgram> _filteredPrograms = new();

        private async void LoadInstalledPrograms_Click(object sender, RoutedEventArgs e)
        {
            UninstallStatus.Text = "Lade installierte Programme...";
            InstalledProgramsGrid.ItemsSource = null;

            _allPrograms.Clear();
            _filteredPrograms.Clear();

            await Task.Run(() =>
            {
                var programs = new List<InstalledProgram>();

                // 1. WinGet-Programme laden
                var wingetPrograms = LoadWinGetPrograms();
                programs.AddRange(wingetPrograms);

                // 2. Registry-Programme laden
                var registryPrograms = LoadRegistryPrograms();
                programs.AddRange(registryPrograms);

                // Duplikate entfernen (basierend auf DisplayName)
                var uniquePrograms = programs
                    .GroupBy(p => p.DisplayName.ToLower())
                    .Select(g => g.OrderBy(p => p.Source == "WinGet" ? 0 : 1).First())
                    .OrderBy(p => p.DisplayName)
                    .ToList();

                Dispatcher.Invoke(() =>
                {
                    foreach (var program in uniquePrograms)
                    {
                        _allPrograms.Add(program);
                        _filteredPrograms.Add(program);
                    }

                    InstalledProgramsGrid.ItemsSource = _filteredPrograms;
                    UninstallStatus.Text = $"{_allPrograms.Count} Programme gefunden";
                });
            });
        }

        private List<InstalledProgram> LoadWinGetPrograms()
        {
            var programs = new List<InstalledProgram>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "list --source winget --accept-source-agreements",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                var output = process?.StandardOutput.ReadToEnd() ?? "";
                process?.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n').Skip(2).Where(l => !string.IsNullOrWhiteSpace(l));

                    foreach (var line in lines)
                    {
                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            programs.Add(new InstalledProgram
                            {
                                DisplayName = parts[0],
                                PackageId = parts.Length > 1 ? parts[1] : parts[0],
                                Version = parts.Length > 2 ? parts[2] : "Unknown",
                                Publisher = "Unknown",
                                Source = "WinGet",
                                IsSilentUninstall = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // WinGet nicht verfügbar oder Fehler
                Dispatcher.Invoke(() => UninstallStatus.Text += $" (WinGet-Fehler: {ex.Message})");
            }

            return programs;
        }

        private List<InstalledProgram> LoadRegistryPrograms()
        {
            var programs = new List<InstalledProgram>();

            try
            {
                // 64-bit Programme
                programs.AddRange(GetProgramsFromRegistry(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));

                // 32-bit Programme auf 64-bit System
                if (Environment.Is64BitOperatingSystem)
                {
                    programs.AddRange(GetProgramsFromRegistry(Registry.LocalMachine, @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
                }

                // User-spezifische Programme
                programs.AddRange(GetProgramsFromRegistry(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => UninstallStatus.Text += $" (Registry-Fehler: {ex.Message})");
            }

            return programs;
        }

        private List<InstalledProgram> GetProgramsFromRegistry(RegistryKey rootKey, string subKeyPath)
        {
            var programs = new List<InstalledProgram>();

            try
            {
                using var key = rootKey.OpenSubKey(subKeyPath);
                if (key == null) return programs;

                foreach (var subkeyName in key.GetSubKeyNames())
                {
                    using var subkey = key.OpenSubKey(subkeyName);
                    if (subkey == null) continue;

                    var displayName = subkey.GetValue("DisplayName") as string;
                    var uninstallString = subkey.GetValue("UninstallString") as string;

                    // Nur Programme mit Namen und Uninstall-String
                    if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(uninstallString))
                        continue;

                    // System-Updates und Windows-Komponenten ausschließen
                    if (displayName.StartsWith("Hotfix") ||
                        displayName.StartsWith("Security Update") ||
                        displayName.StartsWith("Update for") ||
                        displayName.Contains("Microsoft Visual C++ 2") && displayName.Contains("Redistributable"))
                        continue;

                    var version = subkey.GetValue("DisplayVersion") as string ?? "Unknown";
                    var publisher = subkey.GetValue("Publisher") as string ?? "Unknown";
                    var quietUninstallString = subkey.GetValue("QuietUninstallString") as string;

                    programs.Add(new InstalledProgram
                    {
                        DisplayName = displayName,
                        Version = version,
                        Publisher = publisher,
                        UninstallString = !string.IsNullOrEmpty(quietUninstallString) ? quietUninstallString : uninstallString,
                        Source = "Registry",
                        IsSilentUninstall = !string.IsNullOrEmpty(quietUninstallString)
                    });
                }
            }
            catch
            {
                // Registry-Fehler ignorieren
            }

            return programs;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();

            _filteredPrograms.Clear();

            var filtered = string.IsNullOrEmpty(searchText)
                ? _allPrograms
                : _allPrograms.Where(p =>
                    p.DisplayName.ToLower().Contains(searchText) ||
                    p.Publisher.ToLower().Contains(searchText));

            foreach (var program in filtered)
            {
                _filteredPrograms.Add(program);
            }

            UninstallStatus.Text = $"{_filteredPrograms.Count} von {_allPrograms.Count} Programme angezeigt";
        }

        private void SelectAllUninstall_Click(object sender, RoutedEventArgs e)
        {
            foreach (var program in _filteredPrograms)
            {
                program.IsSelected = true;
            }
            InstalledProgramsGrid.Items.Refresh();
        }

        private void DeselectAllUninstall_Click(object sender, RoutedEventArgs e)
        {
            foreach (var program in _filteredPrograms)
            {
                program.IsSelected = false;
            }
            InstalledProgramsGrid.Items.Refresh();
        }

        private async void UninstallSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedPrograms = _filteredPrograms.Where(p => p.IsSelected).ToList();

            if (!selectedPrograms.Any())
            {
                await ShowMessageDialog("Keine Auswahl", "Bitte wählen Sie mindestens ein Programm aus.");
                return;
            }

            var result = await ShowConfirmDialog("Mehrfache Deinstallation bestätigen",
                $"Möchten Sie wirklich {selectedPrograms.Count} Programme deinstallieren?\n\n" +
                string.Join("\n", selectedPrograms.Take(5).Select(p => $"• {p.DisplayName}")) +
                (selectedPrograms.Count > 5 ? $"\n... und {selectedPrograms.Count - 5} weitere" : ""));

            if (result)
            {
                await UninstallMultiplePrograms(selectedPrograms);
            }
        }

        private async void UninstallSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is InstalledProgram program)
            {
                var result = await ShowConfirmDialog("Deinstallation bestätigen",
                    $"Möchten Sie '{program.DisplayName}' wirklich deinstallieren?");

                if (result)
                {
                    await UninstallSingleProgram(program);
                    LoadInstalledPrograms_Click(sender, e); // Refresh
                }
            }
        }

        private async Task UninstallMultiplePrograms(List<InstalledProgram> programs)
        {
            var progressDialog = new ContentDialog
            {
                Title = "Deinstallation läuft...",
                Content = new StackPanel
                {
                    Children =
            {
                new ModernWpf.Controls.ProgressBar { Maximum = programs.Count, Value = 0, Name = "ProgressBar" },
                new TextBlock { Text = "Bereite Deinstallation vor...", Margin = new Thickness(0, 10, 0, 0), Name = "StatusText" }
            }
                },
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false
            };

            _ = progressDialog.ShowAsync();

            var successCount = 0;
            var failedPrograms = new List<string>();

            for (int i = 0; i < programs.Count; i++)
            {
                var program = programs[i];

                Dispatcher.Invoke(() =>
                {
                    var progressBar = (ModernWpf.Controls.ProgressBar)((StackPanel)progressDialog.Content).Children[0];
                    var statusText = (TextBlock)((StackPanel)progressDialog.Content).Children[1];

                    progressBar.Value = i;
                    statusText.Text = $"Deinstalliere {program.DisplayName} ({i + 1}/{programs.Count})...";
                });

                try
                {
                    await UninstallSingleProgram(program);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedPrograms.Add($"{program.DisplayName}: {ex.Message}");
                }

                await Task.Delay(500); // Kurze Pause zwischen Deinstallationen
            }

            progressDialog.Hide();

            var summary = $"Deinstallation abgeschlossen!\n\n";
            summary += $"✅ Erfolgreich: {successCount}/{programs.Count}\n";

            if (failedPrograms.Any())
            {
                summary += $"❌ Fehlgeschlagen: {failedPrograms.Count}\n\n";
                summary += "Fehlgeschlagene Programme:\n";
                summary += string.Join("\n", failedPrograms.Take(5));

                if (failedPrograms.Count > 5)
                {
                    summary += $"\n... und {failedPrograms.Count - 5} weitere";
                }
            }

            await ShowMessageDialog("Deinstallation abgeschlossen", summary);
            LoadInstalledPrograms_Click(null, null); // Refresh
        }

        private async Task UninstallSingleProgram(InstalledProgram program)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (program.Source == "WinGet")
                    {
                        // WinGet Deinstallation
                        var psi = new ProcessStartInfo
                        {
                            FileName = "winget",
                            Arguments = $"uninstall --id \"{program.PackageId}\" --silent --disable-interactivity",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(psi);
                        process?.WaitForExit();

                        if (process?.ExitCode != 0)
                        {
                            throw new Exception($"WinGet-Deinstallation fehlgeschlagen (Exit Code: {process?.ExitCode})");
                        }
                    }
                    else
                    {
                        // Registry-basierte Deinstallation
                        var uninstallCommand = program.UninstallString;

                        // MSI-Installer erkennen und silent parameter hinzufügen
                        if (uninstallCommand.StartsWith("MsiExec", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!uninstallCommand.Contains("/quiet") && !uninstallCommand.Contains("/q"))
                            {
                                uninstallCommand += " /quiet";
                            }
                        }

                        ProcessStartInfo psi;

                        if (uninstallCommand.Contains("\""))
                        {
                            // Executable mit Parametern
                            var parts = uninstallCommand.Split('"');
                            var executable = parts.Length > 1 ? parts[1] : parts[0];
                            var arguments = parts.Length > 2 ? string.Join("\"", parts.Skip(2)) : "";

                            psi = new ProcessStartInfo
                            {
                                FileName = executable,
                                Arguments = arguments,
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                        }
                        else
                        {
                            // Direkter Befehl
                            psi = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"{uninstallCommand}\"",
                                UseShellExecute = true,
                                Verb = "runas"
                            };
                        }

                        using var process = Process.Start(psi);
                        process?.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Deinstallation fehlgeschlagen: {ex.Message}");
                }
            });
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

    // Custom Exception für bessere Fehlerbehandlung
    public class InstallationSummaryException : Exception
    {
        public int SuccessCount { get; }
        public int FailedCount { get; }

        public InstallationSummaryException(string message, int successCount, int failedCount)
            : base(message)
        {
            SuccessCount = successCount;
            FailedCount = failedCount;
        }
    }
}

public class InstalledProgram
{
    public bool IsSelected { get; set; }
    public string DisplayName { get; set; } = "";
    public string Version { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string UninstallString { get; set; } = "";
    public string Source { get; set; } = ""; // "WinGet" oder "Registry"
    public string PackageId { get; set; } = ""; // Für WinGet
    public bool IsSilentUninstall { get; set; }
}
