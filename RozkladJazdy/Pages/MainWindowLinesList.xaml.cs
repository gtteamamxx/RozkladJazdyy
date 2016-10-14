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
        private List<Linia> lines;
        private ObservableCollection<Linia> lines_tram;
        private ObservableCollection<Linia> lines_air;
        private ObservableCollection<Linia> lines_night;
        private ObservableCollection<Linia> lines_bus;
        private ObservableCollection<Linia> lines_mini;

        public static Linia selected_line;
        public static int selected_schedule = new int();

        private static bool isPageLoaded = false;

        public MainWindowLinesList()
        {
            this.InitializeComponent();

            MainPage.OnTimeTableRefesh += () =>
            {
                lines.Clear();
                lines_tram.Clear();
                lines_air.Clear();
                lines_night.Clear();
                lines_bus.Clear();
                lines_mini.Clear();
                selected_line = null;
                selected_schedule = -1;
                isPageLoaded = false;
            };

            
            lines = new List<Linia>();
            lines_tram = new ObservableCollection<Linia>();
            lines_air = new ObservableCollection<Linia>();
            lines_night = new ObservableCollection<Linia>();
            lines_bus = new ObservableCollection<Linia>();
            lines_mini = new ObservableCollection<Linia>();

            selected_line = new Linia();
        }
        private void MainWindowLinesListGirdView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!isPageLoaded)
                return;

            var linia = e.ClickedItem as Linia;

            selected_line = linia;

            var liczba_rozkladow = SQLServices.getData<Rozklad>(0, "SELECT id FROM Rozklad where id_linia = ?", selected_line.id).Count();

            if (liczba_rozkladow > 1)
                MainPage.gui.setViewPage = typeof(MainWindowLinesSchedule);
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

                foreach (Linia l in lines)
                    if ((l.pfm & 4) == 4)
                        worker.ReportProgress(0, l); // pierw tramwaje
                worker.ReportProgress(10);

                foreach (Linia l in lines)
                    if ((l.pfm & 16) == 16)
                        worker.ReportProgress(1, l); // potem lotnisko
                worker.ReportProgress(20);

                foreach (Linia l in lines)
                    if ((l.pfm & 256) == 256)
                        worker.ReportProgress(2, l); // potem nocne
                worker.ReportProgress(30);

                foreach (Linia l in lines)
                    if ((l.pfm & 1) == 1)
                        worker.ReportProgress(3, l); // potem autobusy
                worker.ReportProgress(40);

                foreach (Linia l in lines)
                    if ((l.pfm & 8) == 8)
                        worker.ReportProgress(4, l); // potem mini
                worker.ReportProgress(50);
            };

            /*Invoke*/
            worker.ProgressChanged += (senders, es) =>
            {
                if (es.ProgressPercentage == 10)
                    MainWindowLinesProgressRingTrams.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 20)
                    MainWindowLinesProgressRingAir.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 30)
                    MainWindowLinesProgressRingNight.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 40)
                    MainWindowLinesProgressRingBus.Visibility = Visibility.Collapsed;
                else if (es.ProgressPercentage == 50)
                    MainWindowLinesProgressRingMini.Visibility = Visibility.Collapsed;

                else if (es.ProgressPercentage == 0)
                    lines_tram.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 1)
                    lines_air.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 2)
                    lines_night.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 3)
                    lines_bus.Add(es.UserState as Linia);
                else if (es.ProgressPercentage == 4)
                    lines_mini.Add(es.UserState as Linia);
            };

            worker.RunWorkerCompleted += (sendesr, es) =>
            {
                if (lines_tram.Count() == 0)
                {
                    MainWindowLinesListGirdViewTrams.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelTrams.Visibility = Visibility.Collapsed;
                }

                if (lines_bus.Count() == 0)
                {
                    MainWindowLinesListGirdViewBus.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelBus.Visibility = Visibility.Collapsed;
                }

                if (lines_air.Count() == 0)
                {
                    MainWindowLinesListGirdViewAir.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelAir.Visibility = Visibility.Collapsed;
                }

                if (lines_mini.Count() == 0)
                {
                    MainWindowLinesListGirdViewMini.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelMini.Visibility = Visibility.Collapsed;
                }

                if (lines_night.Count() == 0)
                {
                    MainWindowLinesListGirdViewNight.Visibility = Visibility.Collapsed;
                    MainWindowLinesStackPanelNight.Visibility = Visibility.Collapsed;
                }

                isPageLoaded = true;

                MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
                MainWindow.isTimetableRefreshing = false;

            };

            if (!isPageLoaded || (!isPageLoaded && MainWindow.isPageLoaded == true))
            {
                lines = MainWindow.lines;
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

            if (MainWindowLinesStackPanelTrams.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelTrams.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelAir.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelAir.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelNight.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelNight.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelBus.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelBus.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelMini.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelMini.Width = e.NewSize.Width;

            if (MainWindowLinesStackPanelAllLines.Visibility == Visibility.Visible)
                MainWindowLinesStackPanelAllLines.Width = e.NewSize.Width;
        }

        private void MainWindowLinesAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var asg = (sender as AutoSuggestBox);

            if (asg.Text.Trim().Count() == 0)
            {
                MainWindowLinesStackPanelAllLines.Visibility = Visibility.Collapsed;
                MainWindowLinesListGirdViewAllLines.Visibility = Visibility.Collapsed;
                MainWindowLinesListSearchText.Visibility = Visibility.Collapsed;
                asg.Items.Clear();
            }
            else
            {
                MainWindowLinesStackPanelAllLines.Visibility = Visibility.Visible;
                MainWindowLinesListGirdViewAllLines.Visibility = Visibility.Visible;

                asg.Items.Clear();

                foreach (Linia l in lines.Where(p => (p as Linia).name.ToLower().StartsWith(asg.Text.Trim().ToLower())))
                    asg.Items.Add(l);

                if (asg.Items.Count == 0)
                    MainWindowLinesListSearchText.Visibility = Visibility.Visible;
                else
                    MainWindowLinesListSearchText.Visibility = Visibility.Collapsed;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
            this.SizeChanged += Page_SizeChanged;
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Lista linii";
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
            => this.SizeChanged -= Page_SizeChanged;

    }
}
