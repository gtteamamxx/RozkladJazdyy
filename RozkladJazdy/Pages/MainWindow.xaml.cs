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
        public static List<Linia> lines;
        public static bool? isPageLoaded = null;
        public static bool isTimetableRefreshing = false;
        public static MainWindow gui;

        private bool loadOffineLines = false;

        StorageFolder application_folder = ApplicationData.Current.LocalFolder;

        public MainWindow()
        {
            this.InitializeComponent();
            lines = new List<Linia>();
            gui = this;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if ((isTimetableRefreshing || loadOffineLines == true) && sender != null)
                return;

            var local_files = await application_folder.GetFilesAsync();

            foreach (StorageFile file in local_files)
                if (file.Name.Contains("temp"))
                    await file.DeleteAsync();

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            MainWindowTextBlock.Text = "Wczytywanie rozkładu jazdy...";
            MainWindowStatusProgressRing.Visibility = Visibility.Visible;
            MainWindowAcceptButton.Visibility = Visibility.Collapsed;
            MainWindowDownloadProgressBar.Visibility = Visibility.Collapsed;

            worker.DoWork += (s, f) =>
            {
                try
                {
                    if (File.Exists(string.Format(@"{0}\\{1}", application_folder.Path, "RozkladJazdy.sqlite")))
                    {
                        new SQLServices();

                        bool isTableExist = SQLServices.getData<Linia>(0, "SELECT * FROM sqlite_master WHERE name LIKE 'Linia'").Count() > 0 ? true : false;

                        if (!isTableExist) return;

                        var lines_list = SQLServices.getData<Linia>(0, "SELECT * FROM Linia");

                        foreach (Linia line in lines_list)
                        {
                            int num = (int)(0.5f + ((100f * lines_list.IndexOf(line)) / lines_list.Count()));

                            if ((line.pfm & 4) == 4)
                                lines_list[lines_list.IndexOf(line)].name = line.name.Insert(0, "T");

                            worker.ReportProgress(num, line);
                        }

                    }
                }
                catch
                {
                    //blad  todo
                }
            };
            worker.ProgressChanged += (s, f) =>
            {
                MainWindowDownloadProgressBar.Value = f.ProgressPercentage;
                MainWindowTextBlock.Text = "[" + f.ProgressPercentage + "%]" + " Dodawanie linii: " + (f.UserState as Linia).name;

                lines.Add(f.UserState as Linia);

            };
            worker.RunWorkerCompleted += (s, f) =>
            {
                if (lines.Count() == 0)
                {
                    MainWindowTextBlock.Text = "Do przeglądania rozkładu jazdy potrzebna jest wersja offine, chcesz ją teraz pobrać?";
                    MainWindowAcceptButton.Visibility = Visibility.Visible;
                    MainWindowStatusProgressRing.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MainPage.gui.setViewPage = typeof(MainWindowSelect);

                    loadOffineLines = true;
                    isTimetableRefreshing = false;
                }
            };
            worker.RunWorkerAsync();
        }

        BackgroundDownloader downloader = new BackgroundDownloader();
        DownloadOperation downloadOperation;
        CancellationTokenSource backgroundDownloader = new CancellationTokenSource();
        StorageFile file;

        int file_to_download;
       
        public async Task getfile(int filee)
        {
            string filename = "", url = "";

            if (filee == 1)
            {
                file_to_download = 1;
                filename = "rozklad_temp.sqlite";
                url = "http://www.ball3d.pl/mroczek/RozkladJazdy.sqlite";
            }

            try
            {
                file = AsyncHelpers.RunSync<StorageFile>(async () => await application_folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting));
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
                                loadOffineLines = true;
                                return;
                            }

                            var files = await application_folder.GetFilesAsync();

                            if (SQLServices.getConnection() != null)
                                SQLServices.closeConnection();

                            foreach (StorageFile fille in files)
                                if (fille.Name.ToLower().Contains("rozklad") && !fille.Name.Contains("temp") && !fille.Name.ToLower().Contains("fav"))
                                    await fille.DeleteAsync();

                            await file.RenameAsync("RozkladJazdy.sqlite", NameCollisionOption.ReplaceExisting);
                        }
                        catch
                        {
                            loadOffineLines = true;
                            return;
                        }
                        loadOffineLines = false;

                        Page_Loaded(null, new RoutedEventArgs());
                    }

                    file = null;
                    downloadOperation = null;
                }
                else
                {
                    downloadOperation = null;
                    file = null;
                    loadOffineLines = true;
                }
            }
            catch
            {
                ;
            }
        }
        private ulong bytes_received_temp;
        private void progresschanged(DownloadOperation downloadOperation)
        {
            int progress = (int)(100 * ((double)downloadOperation.Progress.BytesReceived / (double)downloadOperation.Progress.TotalBytesToReceive));

            string message = String.Format("Pobrano {0} z {1} kb. - {2}%. (plik {3}/1) | {4} kb/s", 
                downloadOperation.Progress.BytesReceived / 1024, 
                downloadOperation.Progress.TotalBytesToReceive / 1024, progress, file_to_download, 
                (downloadOperation.Progress.BytesReceived - bytes_received_temp) / 1024);

            bytes_received_temp = downloadOperation.Progress.BytesReceived;

            MainWindowDownloadProgressBar.Value = progress;
            MainWindowTextBlock.Text = message;

            if (downloadOperation.Progress.BytesReceived == downloadOperation.Progress.TotalBytesToReceive)
            {
                if (downloadOperation.Progress.BytesReceived == 0)
                    throw new Exception();

                if (file_to_download != 1)
                    bytes_received_temp = 0;
            }
        }
        public static void refreshList()
        {
            MainPage.gui.setViewPage = MainWindow.gui.GetType();

            isPageLoaded = null;
            isTimetableRefreshing = true;

            lines.Clear();
            MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
            MainPage.OnTimeTableRefesh?.Invoke();
            MainPage.gui.clearStopListStops();

            gui.MainWindowAcceptButton_Click(null, null);
        }
        public async void MainWindowAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPageLoaded == true)
            {
                MainPage.gui.setViewPage = typeof(MainWindowSelect);
                return;
            }

            if (!MainPage.IsInternetConnection())
            {
                gui.MainWindowTextBlock.Text = "Wygląda na to, że nie jesteś podłączony do internetu. Sprawdź połączenie i spróbuj ponownie";
                MainWindowStatusProgressRing.Visibility = Visibility.Collapsed;
                MainWindowAcceptButton.Visibility = Visibility.Visible;
                MainWindowDownloadProgressBar.Visibility = Visibility.Collapsed;
                return;
            }

            MainWindowTextBlock.Text = "Wczytywanie...";

            MainWindowStatusProgressRing.Visibility = Visibility.Visible;
            MainWindowAcceptButton.Visibility = Visibility.Collapsed;
            MainWindowDownloadProgressBar.Visibility = Visibility.Visible;

            isPageLoaded = false;

            if (MainPage.isAdmin == false)
            {
                await getfile(1);

                if (loadOffineLines == false)
                    return;
            }

            if (SQLServices.getConnection() != null)
                SQLServices.closeConnection();

            HTMLServices.getLinesInfo(false); //false update

            HTMLServices.OnGetLinesDetailProgressChange += (percent, state, update) =>
            {
                MainWindowDownloadProgressBar.Value = percent;
                MainWindowTextBlock.Text = "[" + percent + "%]" + " " + (update==true?"Aktualizowanie":"Dodawanie") + " linii: " + state.ToString();
            };
            HTMLServices.OnGetLinesBackgroundFinish += async (new_lines, update) =>
            {
                foreach (StorageFile file in await application_folder.GetFilesAsync())
                    if (file.Name.ToLower().Contains("rozklad") && !file.Name.Contains("temp") && !file.Name.ToLower().Contains("fav"))
                        await file.DeleteAsync();

                lines = new_lines;

                MainWindowTextBlock.Text = "Pomyślnie dodano: " + lines.Count() + " linii. Trwa zapisywanie rozkładu.. proszę czkeać";

                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += (s, f) =>
                {
                    var stoplist_list= new List<PrzystanekListaPrzystanków>();

                    for (int j = 0; j < HTMLServices.stops_name.Count(); j++)
                    {
                        for (int i = 0; i < lines.Count(); i++)
                        {
                            for (int k = 0; k < lines[i].rozklad.Count(); k++)
                            {
                                if (lines[i].rozklad[k].track != null)
                                {
                                    for (int b = 0; b < lines[i].rozklad[k].track.Count(); b++)
                                    {
                                        for (int c = 0; c < lines[i].rozklad[k].track[b].stops.Count(); c++)
                                        {
                                            if (lines[i].rozklad[k].track[b].stops[c].nid == HTMLServices.stops_name[j].id)
                                            {
                                                bool isBreak = false;
                                                foreach (PrzystanekListaPrzystanków pp in stoplist_list)
                                                {
                                                    if (pp.linia_id == i && pp.rozklad_id == k && pp.przystanek_id == j && pp.trasa_id ==
                                                        lines[i].rozklad[k].track[b].stops[c].track_id)
                                                    {
                                                        isBreak = true;
                                                        break;
                                                    }
                                                }
                                                if (isBreak) break;

                                                stoplist_list.Add(new PrzystanekListaPrzystanków()
                                                {
                                                    linia_id = i,
                                                    przystanek_id = j,
                                                    przystanek_id2 = c,
                                                    rozklad_id = k,
                                                    trasa_id = lines[i].rozklad[k].track[b].stops[c].track_id
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

                    SQLServices.addAllToDataBase<Linia>(lines);
                    SQLServices.addAllToDataBase<NazwaPrzystanku>(HTMLServices.stops_name);
                    SQLServices.addAllToDataBase<NazwaGodziny>(HTMLServices.hours_name);
                    SQLServices.addAllToDataBase<Literka>(HTMLServices.letters_info);
                    SQLServices.addAllToDataBase<PrzystanekListaPrzystanków>(stoplist_list);

                    List<Przystanek> przystanki = new List<Przystanek>();
                    List<Rozklad> rozklady = new List<Rozklad>();
                    List<Trasa> trasy = new List<Trasa>();
                    List<Godzina> godziny = new List<Godzina>();

                    foreach (Linia l in lines)
                        foreach (Rozklad r in l.rozklad)
                        {
                            rozklady.Add(r);

                            if (r.track != null)
                                foreach (Trasa t in r.track)
                                {
                                    trasy.Add(t);
                                    foreach (Przystanek d in t.stops)
                                    {
                                        przystanki.Add(d);
                                        foreach (Godzina g in d.godziny)
                                            godziny.Add(g);
                                    }
                                }
                        }

                    SQLServices.addAllToDataBase<Rozklad>(rozklady);
                    SQLServices.addAllToDataBase<Trasa>(trasy);
                    SQLServices.addAllToDataBase<Przystanek>(przystanki);
                    SQLServices.addAllToDataBase<Godzina>(godziny);
                };

                worker.RunWorkerCompleted += (s, f) =>
                {
                    MainWindowTextBlock.Text = "Pomyślnie dodano: " + lines.Count() + " linii. Kliknij przycisk poniżej aby przejść do rozkładu";
                    MainWindowAcceptButton.Visibility = Visibility.Visible;
                    isPageLoaded = true;
                };


                Color color = Colors.LightGreen;
                color.A = 21;

                MainWindowStackPanel.Background = new SolidColorBrush(color);
                MainWindowDownloadProgressBar.Visibility = Visibility.Collapsed;
                MainWindowStatusProgressRing.Visibility = Visibility.Collapsed;

                worker.RunWorkerAsync();
            };
        }

        private int click_num = 0;
        private void MainWindowTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (MainPage.isAdmin == false)
            {
                click_num++;

                if (click_num == 10)
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
