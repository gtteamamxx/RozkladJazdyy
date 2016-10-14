using RozkladJazdy.Model;
using RozkladJazdy.Model.RozkladJazy.Modelnet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainWindowLinesList : Page
    {
        private List<Linia> Linie;
        private ObservableCollection<Linia> LinieTramwaje;
        private ObservableCollection<Linia> LinieLotnisko;
        private ObservableCollection<Linia> LinieNocne;
        private ObservableCollection<Linia> LinieAutobusy;
        private ObservableCollection<Linia> LinieMini;

        public static Linia selectedLine;
        public static int selectedRozklad = new int();

        private static bool loaded = false;

        public MainWindowLinesList()
        {
            this.InitializeComponent();

            MainPage.OnTimeTableRefesh += () =>
            {
                Linie.Clear();
                LinieTramwaje.Clear();
                LinieLotnisko.Clear();
                LinieNocne.Clear();
                LinieAutobusy.Clear();
                LinieMini.Clear();
                selectedLine = null;
                selectedRozklad = -1;
                loaded = false;
            };

            
            Linie = new List<Linia>();
            LinieTramwaje = new ObservableCollection<Linia>();
            LinieLotnisko = new ObservableCollection<Linia>();
            LinieNocne = new ObservableCollection<Linia>();
            LinieAutobusy = new ObservableCollection<Linia>();
            LinieMini = new ObservableCollection<Linia>();

            selectedLine = new Linia();
        }
        private void MainWindowLinesListGirdView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!loaded)
                return;

            var linia = e.ClickedItem as Linia;

            selectedLine = linia;

            var liczba_rozkladow = SQLServices.getData<Rozklad>(0, "SELECT id FROM Rozklad where id_linia = ?", selectedLine.id).Count();

            if (liczba_rozkladow > 1)
                MainPage.gui.setViewPage = typeof(MainWindowLinesRozkladDzien);
            else
                MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);

            MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += (senders, es) =>
            {
                foreach (Linia l in Linie)
                    if ((l.pfm & 4) == 4)
                        worker.ReportProgress(0, l); // pierw tramwaje
                worker.ReportProgress(10);

                foreach (Linia l in Linie)
                    if ((l.pfm & 16) == 16)
                        worker.ReportProgress(1, l); // potem lotnisko
                worker.ReportProgress(20);

                foreach (Linia l in Linie)
                    if ((l.pfm & 256) == 256)
                        worker.ReportProgress(2, l); // potem nocne
                worker.ReportProgress(30);

                foreach (Linia l in Linie)
                    if ((l.pfm & 1) == 1)
                        worker.ReportProgress(3, l); // potem autobusy
                worker.ReportProgress(40);

                foreach (Linia l in Linie)
                    if ((l.pfm & 8) == 8)
                        worker.ReportProgress(4, l); // potem mini
                worker.ReportProgress(50);
            };

            /*Invoke*/
            worker.ProgressChanged += (senders, es) =>
            {
                if (es.ProgressPercentage == 10)
                    MainWindowLinesProgressRingTramwaje.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 20)
                    MainWindowLinesProgressRingLotnisko.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 30)
                    MainWindowLinesProgressRingNocne.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 40)
                    MainWindowLinesProgressRingAutobusy.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 50)
                    MainWindowLinesProgressRingMini.Visibility = Visibility.Collapsed;

                else if (es.ProgressPercentage == 0)
                    LinieTramwaje.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 1)
                    LinieLotnisko.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 2)
                    LinieNocne.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 3)
                    LinieAutobusy.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 4)
                    LinieMini.Add(es.UserState as Linia);
            };

            worker.RunWorkerCompleted += (sendesr, es) =>
            {
                if (LinieTramwaje.Count() == 0)
                {
                    MainWindowLinesListGirdViewTramwaje.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelTramwaje.Visibility = Visibility.Collapsed;
                }

                if (LinieAutobusy.Count() == 0)
                {
                    MainWindowLinesListGirdViewAutobusy.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelAutobusy.Visibility = Visibility.Collapsed;
                }

                if (LinieLotnisko.Count() == 0)
                {
                    MainWindowLinesListGirdViewLotnisko.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelLotnisko.Visibility = Visibility.Collapsed;
                }

                if (LinieMini.Count() == 0)
                {
                    MainWindowLinesListGirdViewMini.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelMini.Visibility = Visibility.Collapsed;
                }

                if (LinieNocne.Count() == 0)
                {
                    MainWindowLinesListGirdViewNocne.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelNocne.Visibility = Visibility.Collapsed;
                }

                loaded = true;

                MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
                MainWindow.refresh = false;

            };

            if (!loaded || (!loaded && MainWindow.isLoaded == true))
            {
                Linie = MainWindow.Lines;
                worker.RunWorkerAsync();
            }
        }

        private void MainWindowLinesStackPanel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var GridView = MainWindowLinesStackPanel1.Children.ElementAt(MainWindowLinesStackPanel1.Children.IndexOf(sender as UIElement) + 1) as GridView;

            if (GridView == null) // nie powinien występować, ale po prostu sprawdź na wszelki wypadek
                return;

            GridView.Visibility = GridView.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            string b = string.Empty;

            if (GridView.Visibility == Visibility.Collapsed)
                b = "\xE74A"; // gora
            else
                b = "\xE74B";

            var a = ((sender as StackPanel).Children[0] as Grid).Children[1] as TextBlock;

            a.Text = b;

        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainWindowLinesStackPanel1.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelTramwaje.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelTramwaje.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelLotnisko.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelLotnisko.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelNocne.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelNocne.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelAutobusy.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelAutobusy.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelMini.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelMini.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelWszystkieLinie.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelWszystkieLinie.Width = e.NewSize.Width;
        }

        private void MainWindowLinesAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var asg = (sender as AutoSuggestBox);

            if (asg.Text.Trim().Count() == 0)
            {
                MainWindowLinesStackPanelWszystkieLinie.Visibility = Visibility.Collapsed;
                MainWindowLinesListGirdViewWszystkieLinie.Visibility = Visibility.Collapsed;
                SzukaneLinieText.Visibility = Visibility.Collapsed;
                asg.Items.Clear();
            }
            else
            {
                MainWindowLinesStackPanelWszystkieLinie.Visibility = Visibility.Visible;
                MainWindowLinesListGirdViewWszystkieLinie.Visibility = Visibility.Visible;

                asg.Items.Clear();

                foreach (Linia l in Linie.Where(p => (p as Linia).name.ToLower().StartsWith(asg.Text.Trim().ToLower())))
                    asg.Items.Add(l);

                if (asg.Items.Count == 0)
                    SzukaneLinieText.Visibility = Visibility.Visible;
                else
                    SzukaneLinieText.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
            this.SizeChanged += Page_SizeChanged;
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Lista linii";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.SizeChanged -= Page_SizeChanged;
        }
    }
}
