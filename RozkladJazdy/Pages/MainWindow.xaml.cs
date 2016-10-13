using RozkladJazdy.Model;
using RozkladJazdy.Model.RozkladJazy.Modelnet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Page
    {
        public static List<Linia> Lines;
        public static bool? isLoaded = null;
        public static bool offine = false;
        public static bool refresh = false;
        public static MainWindow gui;
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

        public MainWindow()
        {
            this.InitializeComponent();
            Lines = new List<Linia>();
            gui = this;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if ((refresh || statusdo == true) && sender != null)
                return;

            var files = await localFolder.GetFilesAsync();

            foreach (var a in files)
                if (a.Name.Contains("temp"))
                    await a.DeleteAsync();

            BackgroundWorker wor = new BackgroundWorker();
            wor.WorkerReportsProgress = true;

            MainWindowTextBlock.Text = "Wczytywanie rozkładu jazdy...";
            MainWindowProgressRing.Visibility = Visibility.Visible;
            MainWindowButton.Visibility = Visibility.Collapsed;
            MainWindowProgressBar.Visibility = Visibility.Collapsed;

            wor.DoWork += (s, f) =>
            {
                try
                {
                    if (File.Exists(string.Format(@"{0}\\{1}", localFolder.Path, "RozkladJazdy.sqlite")))
                    {
                        new SQLServices();

                        bool table_exist = SQLServices.getData<Linia>(0, "SELECT * FROM sqlite_master WHERE name LIKE 'Linia'").Count() > 0 ? true : false;

                        if (!table_exist) return;

                        var list = SQLServices.getData<Linia>(0, "SELECT * FROM Linia");

                        foreach (Linia a in list)
                        {
                            int num = (int)(0.5f + ((100f * list.IndexOf(a)) / list.Count()));

                            if ((a.pfm & 4) == 4) list[list.IndexOf(a)].name = a.name.Insert(0, "T");

                            wor.ReportProgress(num, a);
                        }

                    }
                }
                catch
                {
                    //blad  todo
                }
            };
            wor.ProgressChanged += (s, f) =>
            {
                MainWindowProgressBar.Value = f.ProgressPercentage;
                MainWindowTextBlock.Text = "[" + f.ProgressPercentage + "%]" + " Dodawanie linii: " + (f.UserState as Linia).name;

                Lines.Add(f.UserState as Linia);

            };
            wor.RunWorkerCompleted += (s, f) =>
            {
                if (Lines.Count() == 0)
                {
                    MainWindowTextBlock.Text = "Do przeglądania rozkładu jazdy potrzebna jest wersja offine, chcesz ją teraz pobrać?";
                    MainWindowButton.Visibility = Visibility.Visible;
                    MainWindowProgressRing.Visibility = Visibility.Collapsed;
                }
                else
                {
                    offine = true;

                    MainPage.gui.setPage = typeof(MainWindowSelect);

                    statusdo = true;
                    refresh = false;
                }
            };
            wor.RunWorkerAsync();
        }

        BackgroundDownloader downloader = new BackgroundDownloader();
        DownloadOperation downloadOperation;
        CancellationTokenSource backgroundDownloader = new CancellationTokenSource();
        StorageFile file;

        int pr;
        bool statusdo = false;

        public async Task getfile(int filee)
        {
            string filename = "", url = "";

            if (filee == 1)
            {
                pr = 1;
                filename = "rozklad_temp.sqlite";
                url = "http://www.ball3d.pl/mroczek/RozkladJazdy.sqlite";
            }

            try
            {
                file = AsyncHelpers.RunSync<StorageFile>(async () => await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting));
            }
            catch { }

            downloadOperation = downloader.CreateDownload(new Uri(@url, UriKind.Absolute), file);
            Progress<DownloadOperation> progress = new Progress<DownloadOperation>(progresschanged);

            try
            {
                await downloadOperation.StartAsync().AsTask(backgroundDownloader.Token, progress);

                if (downloadOperation.Progress.Status == BackgroundTransferStatus.Completed)
                {
                    MainWindowTextBlock.Text = "Trwa zapisywanie pliku...";

                    if (file.Name.Contains("rozklad_temp"))
                    {
                        try
                        {
                            if ((await file.GetBasicPropertiesAsync()).Size < 10 * 1024 * 1024) // if size < 10 mb
                            {
                                statusdo = true;
                                return;
                            }

                            var files = await localFolder.GetFilesAsync();

                            if (SQLServices.getConnection() != null)
                                SQLServices.closeConnection();

                            foreach (StorageFile fille in files)
                                if (fille.Name.ToLower().Contains("rozklad") && !fille.Name.Contains("temp") && !fille.Name.ToLower().Contains("fav"))
                                    await fille.DeleteAsync();

                            await file.RenameAsync("RozkladJazdy.sqlite", NameCollisionOption.ReplaceExisting);
                        }
                        catch
                        {
                            statusdo = true;
                            return;
                        }
                        statusdo = false;

                        Page_Loaded(null, new RoutedEventArgs());
                    }

                    file = null;
                    downloadOperation = null;
                }
                else
                {
                    downloadOperation = null;
                    file = null;
                    statusdo = true;
                }
            }
            catch
            {
                ;
            }
        }
        private ulong ttemp;
        private void progresschanged(DownloadOperation downloadOperation)
        {
            int progress = (int)(100 * ((double)downloadOperation.Progress.BytesReceived / (double)downloadOperation.Progress.TotalBytesToReceive));
            string txt = String.Format("Pobrano {0} z {1} kb. - {2}%. (plik {3}/1) | {4} kb/s", downloadOperation.Progress.BytesReceived / 1024, downloadOperation.Progress.TotalBytesToReceive / 1024, progress, pr, (downloadOperation.Progress.BytesReceived - ttemp) / 1024);

            ttemp = downloadOperation.Progress.BytesReceived;

            MainWindowProgressBar.Value = progress;
            MainWindowTextBlock.Text = txt;

            if (downloadOperation.Progress.BytesReceived == downloadOperation.Progress.TotalBytesToReceive)
            {
                if (downloadOperation.Progress.BytesReceived == 0)
                    throw new Exception();

                if (pr != 3)
                    ttemp = 0;
            }
        }
        public static void refreshList()
        {
            MainPage.gui.setPage = MainWindow.gui.GetType();

            isLoaded = null;
            offine = false;
            refresh = true;

            Lines.Clear();
            MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
            MainPage.OnRefreshRozklady?.Invoke();
            MainPage.gui.clearPrzystanki();

            gui.MainWindowButton_Click(new object(), new RoutedEventArgs());
        }
        public async void MainWindowButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoaded == true)
            {
                MainPage.gui.setPage = typeof(MainWindowSelect);
                return;
            }

            if (!MainPage.IsInternetConnection())
            {
                gui.MainWindowTextBlock.Text = "Wygląda na to, że nie jesteś podłączony do internetu. Sprawdź połączenie i spróbuj ponownie";
                MainWindowProgressRing.Visibility = Visibility.Collapsed;
                MainWindowButton.Visibility = Visibility.Visible;
                MainWindowProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            MainWindowTextBlock.Text = "Wczytywanie...";

            MainWindowProgressRing.Visibility = Visibility.Visible;
            MainWindowButton.Visibility = Visibility.Collapsed;
            MainWindowProgressBar.Visibility = Visibility.Visible;

            isLoaded = false;

            if (MainPage.isAdmin == false)
            {
                await getfile(1);

                if (statusdo == false)
                    return;
            }

            if (SQLServices.getConnection() != null)
                SQLServices.closeConnection();

            HTMLServices.getLinesInfo(false); //false update

            HTMLServices.OnGetLinesDetailProgressChange += (percent, state, update) =>
            {
                MainWindowProgressBar.Value = percent;
                MainWindowTextBlock.Text = "[" + percent + "%]" + " " + (update==true?"Aktualizowanie":"Dodawanie") + " linii: " + state.ToString();
            };
            HTMLServices.OnGetLinesBackgroundFinish += async (a, update) =>
            {

                foreach (StorageFile fille in await localFolder.GetFilesAsync())
                    if (fille.Name.ToLower().Contains("rozklad") && !fille.Name.Contains("temp") && !fille.Name.ToLower().Contains("fav"))
                        await fille.DeleteAsync();
                Lines = a;

                MainWindowTextBlock.Text = "Pomyślnie dodano: " + Lines.Count() + " linii. Trwa zapisywanie rozkładu.. proszę czkeać";

                BackgroundWorker wor = new BackgroundWorker();

                var str = string.Empty;

                wor.DoWork += (s, f) =>
                {
                    var plp = new List<PrzystanekListaPrzystanków>();

                    for (int j = 0; j < HTMLServices.przystankinames.Count(); j++)
                    {
                        for (int i = 0; i < Lines.Count(); i++)
                        {
                            for (int k = 0; k < Lines[i].rozklad.Count(); k++)
                            {
                                if (Lines[i].rozklad[k].track != null)
                                {
                                    for (int b = 0; b < Lines[i].rozklad[k].track.Count(); b++)
                                    {
                                        for (int c = 0; c < Lines[i].rozklad[k].track[b].stops.Count(); c++)
                                        {
                                            if (Lines[i].rozklad[k].track[b].stops[c].nid == HTMLServices.przystankinames[j].id)
                                            {
                                                bool ct = false;
                                                foreach (var pp in plp)
                                                {
                                                    if (pp.linia_id == i && pp.rozklad_id == k && pp.przystanek_id == j && pp.trasa_id ==
                                                        Lines[i].rozklad[k].track[b].stops[c].track_id)
                                                    {
                                                        ct = true;
                                                        break;
                                                    }
                                                }
                                                if (ct) break;

                                                plp.Add(new PrzystanekListaPrzystanków()
                                                {
                                                    linia_id = i,
                                                    przystanek_id = j,
                                                    przystanek_id2 = c,
                                                    rozklad_id = k,
                                                    trasa_id = Lines[i].rozklad[k].track[b].stops[c].track_id
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    new SQLServices();

                    SQLServices.createDatabases();

                    SQLServices.addAllToDataBase<Linia>(Lines);
                    SQLServices.addAllToDataBase<NazwaPrzystanku>(HTMLServices.przystankinames);
                    SQLServices.addAllToDataBase<NazwaGodziny>(HTMLServices.godzinynames);
                    SQLServices.addAllToDataBase<Literka>(HTMLServices.literkiinfo);
                    SQLServices.addAllToDataBase<PrzystanekListaPrzystanków>(plp);

                    List<Przystanek> przystanki = new List<Przystanek>();
                    List<Rozklad> rozklady = new List<Rozklad>();
                    List<Trasa> trasy = new List<Trasa>();
                    List<Godzina> godziny = new List<Godzina>();

                    foreach (var l in MainWindow.Lines)
                        foreach (var r in l.rozklad)
                        {
                            rozklady.Add(r);

                            if (r.track != null)
                                foreach (var t in r.track)
                                {
                                    trasy.Add(t);
                                    foreach (var d in t.stops)
                                    {
                                        przystanki.Add(d);
                                        foreach (var g in d.godziny)
                                            godziny.Add(g);
                                    }
                                }
                        }

                    SQLServices.addAllToDataBase<Rozklad>(rozklady);
                    SQLServices.addAllToDataBase<Trasa>(trasy);
                    SQLServices.addAllToDataBase<Przystanek>(przystanki);
                    SQLServices.addAllToDataBase<Godzina>(godziny);
                };

                wor.RunWorkerCompleted += (s, f) =>
                {
                    MainWindowTextBlock.Text = "Pomyślnie dodano: " + Lines.Count() + " linii. Kliknij przycisk poniżej aby przejść do rozkładu";
                    MainWindowButton.Visibility = Visibility.Visible;
                    isLoaded = true;
                };

                wor.RunWorkerAsync();

                Color color = Colors.LightGreen;
                color.A = 21;

                MainWindowStackPanel.Background = new SolidColorBrush(color);
                MainWindowProgressBar.Visibility = Visibility.Collapsed;
                MainWindowProgressRing.Visibility = Visibility.Collapsed;
            };
        }

        private int temp_num = 0;
        private void MainWindowTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MainPage.isAdmin == false)
            {
                temp_num++;

                if (temp_num == 10)
                {
                    MainPage.isAdmin = true;
                    MainPage.showInfo("Okeeej, już jesteś adminem :)");
                };
            }
            else
                MainPage.showInfo("już jesteś przecież adminem :)");
        }
    }
}
