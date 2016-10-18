using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Forms;

namespace osu_Export
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel VM;
        public MainWindow()
        {
            InitializeComponent();
            VM = ViewModel.Get();
            DataContext = VM;
            SearchBox.Focus();
        }
        private async void ExportSongs(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VM.OperationInProgress)
                {
                    VM.ConsoleAdd("An operation is currently in progress.");

                    return;
                }
                string[] inFolder = Directory.GetFiles(VM.Destination);
                OpBtn.IsEnabled = true;
                VM.OperationInProgress = true;
                VM.OperationStopping = false;
                List<OsuSong> files = new List<OsuSong>();
                foreach (OsuSong file in SongSelector.SelectedItems)
                {
                    files.Add(file);
                }
                SongSelector.SelectedItem = null;
                Directory.CreateDirectory(VM.Destination);
                Progress.Value = 0;
                Progress.Maximum = files.Count;
                foreach (OsuSong file in files)
                {
                    bool success = false;
                    string newFullTitle = file.FullTitle;
                    if (VM.OperationStopping)
                    {
                        VM.ConsoleAdd("Stopped export operation.");
                        VM.OperationInProgress = false;
                        return;
                    }
                    using (FileStream SourceStream = File.Open(file.FullPath, FileMode.Open))
                    {
                        string invalid = new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars());

                        foreach (char c in invalid)
                        {
                            newFullTitle = newFullTitle.Replace(c, '_');
                        }
                        string destination = VM.Destination + "\\" + newFullTitle + ".mp3";
                        bool exists = await Task.Run(() => inFolder.Contains(destination));

                        if (!exists)
                            using (FileStream DestinationStream = File.Create(destination))
                            {
                                VM.ConsoleAdd("Copying " + file.FullTitle);
                                await SourceStream.CopyToAsync(DestinationStream);
                                success = true;
                            }
                        else
                        {
                            VM.ConsoleAdd("Not copying " + file.FullTitle + " because the file already exists.");
                            success = false;
                        }
                        Progress.Value++;
                    }
                    if (success)
                    {
                        TagLib.File f = TagLib.File.Create(VM.Destination + "\\" + newFullTitle + ".mp3");
                        
                        f.Tag.Album = "Osu Exported";
                        f.Tag.AlbumArtists = new string[] { "osu!Export" };
                        f.Tag.Performers = new string[] { file.Artist };
                        f.Tag.Title = file.Title;
                        f.Tag.Track = 0;
                        f.Tag.TrackCount = 0;
                        
                        TagLib.IPicture newArt = new TagLib.Picture("bck.png");
                        f.Tag.Pictures = new TagLib.IPicture[1] { newArt };
                        f.Save();
                    }
                }
                VM.ConsoleAdd("Done!");
                VM.OperationInProgress = false;
                OpBtn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                StreamWriter sw = new StreamWriter("errorlog.txt");
                sw.WriteLine(ex.Data);
                sw.WriteLine(ex.HelpLink);
                sw.WriteLine(ex.HResult);
                sw.WriteLine(ex.InnerException);
                sw.WriteLine(ex.Message);
                sw.WriteLine(ex.Source);
                sw.WriteLine(ex.StackTrace);
                sw.WriteLine(ex.TargetSite);
                sw.WriteLine(ex.ToString());
                sw.Close();
            }

        }
        private void SourceSelect(object sender, RoutedEventArgs e)
        {
            VM.BrowseForSource();
        }
        private void SelectAll(object sender, RoutedEventArgs e)
        {
            SongSelector.SelectAll();
            SongSelector.Focus();
        }
        private void ClearSelect(object sender, RoutedEventArgs e)
        {
            SongSelector.SelectedItem = null;
        }
        private void DestinationSelect(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.SelectedPath = VM.Destination;
            DialogResult result = dialog.ShowDialog();
            if (result.ToString() == "OK")
            {

                VM.Destination = dialog.SelectedPath;
                StreamWriter sw = new StreamWriter("path.dat", false, Encoding.Unicode);
                sw.Write(VM.Source + "?" + VM.Destination);
                sw.Close();
            }
        }
        private void DelCache(object sender, RoutedEventArgs e)
        {
            if (File.Exists("path.dat"))
                File.Delete("path.dat");
            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();

        }
        private void StopOperation(object sender, RoutedEventArgs e)
        {
            VM.OperationStopping = true;

        }

        private void UpdateResults(object sender, TextChangedEventArgs e)
        {
            if (SearchBox.Text == "")
            {
                VM.FilteredList.Clear();
                foreach (OsuSong file in VM.FileList)
                {
                    VM.FilteredList.Add(file);
                }
                //VM.FilteredList = VM.FileList;
            }
            else
            {
                VM.FilteredList.Clear();
                foreach (OsuSong file in VM.FileList)
                {
                    if (file.FullTitle.ToLower().Contains(SearchBox.Text.ToLower()))
                    {
                        VM.FilteredList.Add(file);
                    }
                }
            }
        }

        private void ClearSelection(object sender, RoutedEventArgs e)
        {

        }
    }
}
