using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TCRSaveEditor.Models;
using System.Linq;

namespace TCRSaveEditor
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<City> Cities { get; set; } = new();
        public ObservableCollection<City> FilteredCities { get; set; } = new();
        public ObservableCollection<string> Factions { get; set; } = new();
        public ObservableCollection<string> ResourceCatalog { get; set; } = new();
        public GameMeta GameMeta { get; set; } = new();
        private string? _currentSavePath;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void action_install_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSavePath == null)
            {
                MessageBox.Show("Open a save file first.");
                return;
            }
            var editedFileName = System.IO.Path.GetFileNameWithoutExtension(_currentSavePath) + "_edited.sav";
            var outPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_currentSavePath)!, editedFileName);
            if (!File.Exists(outPath))
            {
                MessageBox.Show("No edited save file. Did you save your changes?");
                return;
            }
            string[] TCRSEBack = { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotalConflictResistance", "Saved", "SaveGames", "TCRSEBackup" };
            string backPath = System.IO.Path.Combine(TCRSEBack);
            if (!Directory.Exists(backPath))
            {
                try
                {
                    Directory.CreateDirectory(backPath);
                    
                }
                catch (UnauthorizedAccessException err)
                {
                    MessageBox.Show("Cannot create TCRSE backup path: Access denied", backPath);
                    return;
                }
            }
            string backupFile = System.IO.Path.Combine(backPath, System.IO.Path.GetFileName(_currentSavePath));
            if (!File.Exists(backupFile))
            {
                File.Move(_currentSavePath, backupFile);
            }
            try
            {
                File.Move(outPath, _currentSavePath, true);
                MessageBox.Show("Modified file installed successfully.");
            }
            catch (Exception err)
            {
                MessageBox.Show("Error occurred while renaming file: ", err.Message);
            }
        }
        private void action_savechanges_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSavePath == null)
            {
                MessageBox.Show("Open a save file first.");
                return;
            }
            var editedFileName = System.IO.Path.GetFileNameWithoutExtension(_currentSavePath) + "_edited.sav";
            var outPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_currentSavePath)!, editedFileName);
            try
            {
                TCRSaveEditor.Services.SaveOrchestrator.SaveAll(_currentSavePath, outPath, Cities, GameMeta);
                MessageBox.Show($"Saved and verified: {outPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}");
            }
        }
        private void action_opensave_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            string[] paths = { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotalConflictResistance", "Saved", "SaveGames" };
            string fullPath = System.IO.Path.Combine(paths);
            if (Directory.Exists(fullPath))
                dlg.InitialDirectory = fullPath;

            dlg.DefaultExt = ".sav";
            dlg.Filter = "TCR Save Files (.sav)|*.sav";
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                try
                {
                    TCRSaveEditor.Services.GvasReader.LoadSaveFile(dlg.FileName, Cities, GameMeta);
                    _currentSavePath = dlg.FileName;
                    Factions.Clear();
                    Factions.Add("All");
                    foreach (var faction in Cities.Select(c => c.Faction).Distinct().OrderBy(f => f))
                        Factions.Add(faction);

                    FactionFilterCombo.SelectedIndex = 0; // triggers SelectionChanged -> populates FilteredCities
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Couldn't load that save file:\n{ex.Message}");
                }
            }
        }

        private void FactionFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FactionFilterCombo.SelectedItem is string selected)
            {
                FilteredCities.Clear();
                var matching = selected == "All" ? Cities : Cities.Where(c => c.Faction == selected);
                foreach (var city in matching)
                    FilteredCities.Add(city);
            }
        }
        private void AddResource_Click(object sender, RoutedEventArgs e)
        {
            if (CitiesGrid.SelectedItem is not City city)
            {
                MessageBox.Show("Select a city first.");
                return;
            }

            string name = NewResourceNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Enter a resource name.");
                return;
            }

            if (city.Resources.Any(r => r.Name == name))
            {
                MessageBox.Show("This city already has that resource. Edit its count instead, or remove it first.");
                return;
            }

            city.Resources.Add(new Resource { Name = name, Count = 0 });
            NewResourceNameBox.Text = string.Empty;
        }

        private void RemoveResource_Click(object sender, RoutedEventArgs e)
        {
            if (CitiesGrid.SelectedItem is not City city) return;
            if (((Button)sender).DataContext is not Resource resource) return;
            city.Resources.Remove(resource);
        }

        private void ViewHelp_Click(object sender, RoutedEventArgs e)
        {
            string helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help.md");
            if (!File.Exists(helpPath))
            {
                MessageBox.Show("Help file not found.");
                return;
            }

            string markdownText = File.ReadAllText(helpPath);
            var engine = new MdXaml.Markdown();
            var document = engine.Transform(markdownText);
            document.FontFamily = new System.Windows.Media.FontFamily("Calibri");

            var helpWindow = new Window
            {
                Title = "Help",
                Width = 600,
                Height = 500,
                Content = new System.Windows.Controls.FlowDocumentScrollViewer { Document = document }
            };
            helpWindow.ShowDialog();
        }
    }
}
