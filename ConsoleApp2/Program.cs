using MusicMetadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConsoleApp2
{
    class Program
    {
        public static OpenFolderDialog fb = new OpenFolderDialog();
        //statistical variables
        public static int total = 0;
        public static int songsChanged = 0;
        public static int songsProcessed = 0;

        //misc
        public static string[] fileExtensions;
        public static string title;
        public static string path;
        public static List<string[]> directories;
        public static TagLib.File music;
        public static FileInfo fi;
        public static string artist;
        public static string response;
        public static string defaultPath;

        //time
        public static long[] times;
        public static long avgTime;
        public static Stopwatch totalTime = new Stopwatch();
        public static Stopwatch stopwatch = new Stopwatch();

        //edited database
        public static List<string> editedNames = new List<string>();
        public static List<string> editedArtists = new List<string>();

        //read/write from text file
        public static FileStream tw = new FileStream(@"Config.txt", FileMode.Open, FileAccess.Write,
                        FileShare.ReadWrite);
        public static FileStream tr = new FileStream(@"Config.txt", FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite);

        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Music Metadata Editor";


            promptUser();

            Console.WriteLine("____________________________________\n\n");

            processSongs();

            if (songsChanged > 0)
            {
                Console.WriteLine("\nSongs Edited:\n");

                for (int j = 0; j < editedArtists.Count; j++)
                {
                    Console.WriteLine("{0}\n{1}\n", editedNames[j], editedArtists[j]);
                }
                Console.WriteLine("____________________________________");
            }
            Console.Write("\nDone. {0}/{1} songs edited | {2} milliseconds per song | Time elapsed {3:hh\\:mm\\:ss}\nPress Enter to exit", songsChanged, songsProcessed, avgTime, totalTime.Elapsed);


            Console.ReadLine();
        }
        
        private static void processSongs()
        {
            totalTime.Reset();
            totalTime.Start();
            times = new long[total];
            foreach (string[] array in directories)
            {
                foreach (string file in array)
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    bool edited = false;
                    title = "";
                    artist = "";
                    string album = "";
                    fi = new FileInfo(file);

                    try
                    {
                        music = TagLib.File.Create(file);
                        string[] folders = fi.ToString().Substring(path.Length).Split('\\');
                        title = Path.GetFileNameWithoutExtension(fi.FullName);
                        if (folders[0] == "")
                        {
                            var l = new List<string>();
                            for (int i = 1; i < folders.Length; i++)
                                l.Add(folders[i]);
                            folders = l.ToArray();

                        }
                        if (folders[0].ToString()[0] == '_')
                        {
                            var l = new List<string>();
                            for (int i = 1; i < folders.Length; i++)
                                l.Add(folders[i]);
                            folders = l.ToArray();
                            
                        }
                        if (folders[0] != "Playlists")
                        {
                            switch (folders.Length)
                            {
                                case 2:
                                    artist = folders[0];
                                    title = "";
                                    string[] s = folders[1].Split('.');
                                    for (int i = 0; i < s.Length - 1; i++)
                                    {
                                        title += s[i] + ".";
                                    }
                                    title = title.TrimEnd('.');
                                    break;
                                case 3:
                                    artist = folders[0];
                                    album = folders[1];
                                    title = "";
                                    s = folders[2].Split('.');
                                    for (int i = 0; i < s.Length - 1; i++)
                                    {
                                        title += s[i] + ".";
                                    }
                                    title = title.TrimEnd('.');
                                    break;
                            }

                            if (!music.Tag.Performers.Contains(artist))
                            {
                                edited = true;
                                music.Tag.Performers = new string[] { artist };
                            }

                            if (music.Tag.Title != title)
                            {
                                edited = true;
                                music.Tag.Title = title;
                            }

                            if (music.Tag.Album != album)
                            {
                                edited = true;

                                if (album != "")
                                    music.Tag.Album = album;
                                else
                                    music.Tag.Album = artist;
                            }

                            music.Save();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Exception thrown: {e}\n\n{file}");
                    };
                    if (edited)
                    {
                        songsChanged++;
                        editedNames.Add(title);
                        editedArtists.Add(artist);
                    }

                    times[songsProcessed++] = stopwatch.ElapsedMilliseconds;

                    string loadingBar = "[";
                    int barRes = 50;

                    for (int i = 0; i < barRes; i++)
                    {
                        loadingBar += i*100/barRes < (((songsProcessed*100)/(double)total)) ? "■" : " ";
                    }
                    loadingBar += "]";

                    Console.Write($"\r{loadingBar} Processing song {songsProcessed}/{total}");
                }
            }

            totalTime.Stop();

            avgTime = 0;

            foreach (long time in times)
            {
                avgTime += time;
            }
            avgTime /= times.Length;

        }

        private static void promptUser()
        {
            directories = new List<string[]>();
            using (StreamReader streamreader = new StreamReader(tr, Encoding.UTF8))
            {
                defaultPath = streamreader.ReadLine();
                fileExtensions = streamreader.ReadLine().Split(',');
            }

            //Console.Write("Default Directory is {0}\nEnter directory:", defaultPath);
            fb.Title = "Choose a music folder";
            fb.InitialDirectory = defaultPath;
            fb.ShowDialog(IntPtr.Zero);
            response = fb.SelectedPath;
            
            path = response;

            if (response != defaultPath)
            {
                Console.WriteLine("make new default? Enter <y> to confirm");

                string r = Console.ReadLine();

                if (r == "y" || r == "Y")
                {
                    using (StreamWriter streamwriter = new StreamWriter(tw))
                    {
                        streamwriter.WriteLine(response);
                        Console.WriteLine("New default directory is {0}", response);
                    }
                }
            }

            string extensions = "";
            int i = 0;

            foreach (string ext in fileExtensions)
            {
                extensions += ", " + ext;
                string[] files = Directory.GetFiles(path, "*." + fileExtensions[i], SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    directories.Add(files);
                    total += files.Length;
                }
                i++;
            }

            extensions = extensions.TrimStart(',', ' ');

            Console.Title = "Reading files from " + path + " | " + extensions;
        }
    }
}
