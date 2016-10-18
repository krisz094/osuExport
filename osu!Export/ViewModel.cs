using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;

namespace osu_Export
{
    public class NoOsuFoundException : Exception
    {
        public NoOsuFoundException()
        {

        }
    }
    public class ViewModel : INotifyPropertyChanged
    {
        public bool OperationStopping { get; set; }
        public bool OperationInProgress { get; set; }
        string source;
        public string Source
        {
            get
            {
                return source;
            }
            set
            {
                source = value;
                OnPropertyChanged();
            }
        }
        string destination;
        public string Destination
        {
            get
            {
                return destination;
            }
            set
            {
                destination = value;
                OnPropertyChanged();
            }
        }
        public List<OsuSong> SelectedSongs { get; set; }
        ObservableCollection<string> consoleBox;
        public ObservableCollection<string> ConsoleBox
        {
            get { return consoleBox; }
        }
        static ViewModel peldany;
        private ViewModel()
        {
            FilteredList = new ObservableCollection<OsuSong>();
            consoleBox = new ObservableCollection<string>();

                //TODO: clean this shit up, especially: if path.dat exists but it's bullshit then try to get the actual osu! directory

                //get source+destination
                //if file exists, import from it
                //it it doesn't, try to get default osu! folder
                //if all else fails, make the user browse
                //if no valid folder found, put message


                if (File.Exists("path.dat"))
                {
                    string[] paths = File.ReadAllText("path.dat",Encoding.Unicode).Split('?');

                    Source = paths[0];
                    ConsoleAdd("osu! folder found in cache");

                    try
                    {
                        FileList = OsuSong.ParseAll(Source);
                    }
                    catch (Exception e)
                    {
                        ConsoleAdd(e.Message);

                    }
                    Destination = paths[1];

                }
                else
                {
                    
                    Destination = Directory.GetLogicalDrives().First() + "osu!Export";
                    try
                    {
                        Source = tryGetSource();
                        ConsoleAdd("osu! folder found automatically at " + Source);

                        try
                        {
                            FileList = OsuSong.ParseAll(Source);
                        }
                        catch (Exception e)
                        {
                            ConsoleAdd(e.Message);
                        }

                        StreamWriter sw = new StreamWriter("path.dat",false,Encoding.Unicode);
                        sw.Write(Source + "?" + Destination);
                        sw.Close();
                    }
                    catch (NoOsuFoundException)
                    {
                        BrowseForSource();
                    }

                }
                FilteredList.Clear();
                foreach (OsuSong file in FileList)
                {
                    FilteredList.Add(file);
                }
            

        }
        public void BrowseForSource()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Source;
            dialog.Title = "Please select your osu! executable.";
            dialog.Filter = "osu! launcher (osu!.exe)|osu!.exe";
            DialogResult result = dialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                Source = dialog.FileName.Replace("osu!.exe", "");
                FileList = OsuSong.ParseAll(Source);
                FilteredList.Clear();
                foreach (OsuSong file in FileList)
                {
                    FilteredList.Add(file);
                }
                StreamWriter sw = new StreamWriter("path.dat", false, Encoding.Unicode);
                sw.Write(Source + "?" + Destination);
                sw.Close();
            }
        }
        string tryGetSource() // there is a much better way to do this
        {
            string path = "";
            string[] folders = new string[] { "osu", "osu!", "game\\osu", "game\\osu!", "Program Files\\osu!", "Program Files (x86)\\osu!" };
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            appdata = Path.Combine(Path.GetFullPath(Path.Combine(appdata, @"../")), "Local\\osu!");

            if (Directory.Exists(appdata + "\\") && File.Exists(appdata + "\\osu!.exe"))
            {
                return appdata;
            }
            foreach (string current in Directory.GetLogicalDrives())
            {
                foreach (string folder in folders)
                {
                    if (Directory.Exists(current + folder + "\\") && File.Exists(current + folder + "\\osu!.exe"))
                        return current + folder;
                }
            }

            if (path == "")
                throw new NoOsuFoundException();
            else
                return path;
        }
        public static ViewModel Get()
        {
            if (peldany == null)
                peldany = new ViewModel();
            return peldany;
        }
        string osuPath;
        public string OsuPath
        {
            get { return osuPath; }
            set { osuPath = value; OnPropertyChanged(); }
        }
        List<OsuSong> fileList;
        public List<OsuSong> FileList
        {
            get { return fileList; }
            set { fileList = value; OnPropertyChanged(); }
        }
        public ObservableCollection<OsuSong> FilteredList
        {
            get; set;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName]string nev = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(nev));
        }
       public  void ConsoleAdd(string message)
        {
            ConsoleBox.Insert(0, DateTime.Now.ToShortTimeString() + " | " + message);
        }
    }
    public class OsuSong : IComparable
    {
        string songPath;
        string artist;
        string title;
        string mp3Path;
        public string Artist
        {
            get { return artist; }
        }
        public string Title
        {
            get { return title; }
        }
        private OsuSong(string songPath, string mp3Path, string artist, string title)
        {
            this.songPath = songPath;
            this.mp3Path = mp3Path;
            this.artist = artist;
            this.title = title;
        }
        public string FullTitle
        {
            get { return artist + " - " + title; }
        }
        public string FullPath
        {
            get { return songPath + "\\" + mp3Path; }
        }
        public static OsuSong Parse(string path)
        {
            StreamReader sr = new StreamReader(Directory.EnumerateFiles(path, "*.osu").First());
            string currLine = "";
            while (!currLine.StartsWith("AudioFilename: "))
                currLine = sr.ReadLine();
            string mp3 = currLine.Substring("AudioFilename: ".Length);
            while (!currLine.StartsWith("Title:"))
                currLine = sr.ReadLine();
            string title= currLine.Substring("Title:".Length);
            while (!currLine.StartsWith("Artist:"))
                currLine = sr.ReadLine();
            string artist = currLine.Substring("Artist:".Length);

            return new OsuSong(path, mp3, artist, title);
        }
        public static List<OsuSong> ParseAll(string path)
        {
            List<OsuSong> osuList = new List<OsuSong>();
            foreach (string file in Directory.EnumerateDirectories(path + "\\Songs").ToList())
            {
                osuList.Add(OsuSong.Parse(file));
            }
            osuList.Sort();
            return osuList;
        }

        public int CompareTo(object obj)
        {
            return FullTitle.CompareTo((obj as OsuSong).FullTitle);
        }
    }
}
