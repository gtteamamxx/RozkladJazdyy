using RozkladJazdy.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class MainWindowLinesInfoHours : Page
    {
        private Linia selectedLinia;
        private ObservableCollection<GodzinaHours> LiniaGodziny;
        private ObservableCollection<string> literki;
        private Przystanek przystanek = new Przystanek();

        //public static List<MainWindowLinesInfoThird> lista_test = new List<MainWindowLinesInfoThird>();
        //public static List<Przystanek> lista_test2 = new List<Przystanek>();

        public static bool? was = null;
        public static int hour;
        public static int minute;

        //public static string selectedHour;
        //public static int[] temphour = { 0, 0 };
        //public static int[] temphour2 = { 0, 0 };
        //public static int[] temphour3 = { 0, 0 };

        //public delegate void eventClickHour(string hour, int godzid, int godzid2);
        //public static event eventClickHour OnClickHour;

        private MainWindowLinesInfoHours gui;
        private Trasa trasa;

        public static bool navigated_from;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (selectedLinia != null)
                MainPage.gui.setPageTitle = "Rozkład jazdy -> Linia: " + selectedLinia.name;
        }

        public MainWindowLinesInfoHours()
        {
            this.InitializeComponent();
            gui = this;

            selectedLinia = new Linia();
            LiniaGodziny = new ObservableCollection<GodzinaHours>();
            literki = new ObservableCollection<string>();

            selectedLinia = MainWindowLinesList.selectedLine;
            przystanek = MainWindowLinesInfo.selectedPrzystanek;

            trasa = selectedLinia.rozklad[MainWindowLinesList.selectedRozklad == -1 ? 0 : 
                MainWindowLinesList.selectedRozklad].track[przystanek.track_id];

            if (MainPage.gui.stops_track != przystanek.track_id)
                MainPage.gui.clearStopListStops();

            MainPage.gui.stops_track = przystanek.track_id;
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Linia: " + selectedLinia.name;
            MainPage.gui.setStopListDestName("Kierunek: " + trasa.name);

            MainWindowLinesInfoHOursKierunek.Text = "Kierunek: " + trasa.name;

            MainWindowLinesInfoHOursLinia.Text = selectedLinia.name;
            MainWindowLinesInfoHOursLiniaStackPanel.Background = new SolidColorBrush(new Color() { A = 100, B = 1, R = 0, G = 0 });
            MainWindowLinesInfoHOursPrzystanek.Text = "Przystanek: " + przystanek.getName();

            this.Loaded += MainWindowLinesInfoHours_Loaded;
            this.Unloaded += MainWindowLinesInfoHours_Unloaded;
            this.SizeChanged += MainWindowLinesInfoHours_SizeChanged;
        }

        private void MainWindowLinesInfoHours_Loaded(object sender, RoutedEventArgs e)
        {
            MainPage.gui.setFavouriteSubText = "przystanek";
            //check if stop is in favourtie
            if (MainPage.isFavourite(HTMLServices.przystankinames[MainWindowLinesInfo.selectedPrzystanek.nid]))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;


            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            int num = 0;
            worker.DoWork += (senders, es) =>
            {
                if (przystanek == null || przystanek.godziny == null)
                    przystanek.godziny = new List<Godzina>();

                var index = MainWindowLinesList.selectedLine.rozklad[przystanek.rozkladzien_id].
                        track[przystanek.track_id].stops.IndexOf(przystanek);

                if(index == -1)
                {
                    es.Cancel = true;
                    return;
                }
                przystanek.godziny = MainWindowLinesList.selectedLine.rozklad[przystanek.rozkladzien_id].track[przystanek.track_id].
                    stops[index].godziny = new List<Godzina>();

                przystanek.godziny = MainWindowLinesList.selectedLine.rozklad[przystanek.rozkladzien_id].track[przystanek.track_id].
                    stops[index].godziny = SQLServices.getData<Godzina>(0, "SELECT * FROM Godzina WHERE id_przystanek = ?", przystanek.id);

                if (przystanek == null || przystanek.godziny == null)
                {
                    es.Cancel = true;
                    return;
                }

                foreach (var p in przystanek.godziny)
                {
                    if (przystanek == null || przystanek.godziny == null)
                    {
                        es.Cancel = true;
                        return;
                    }
                    worker.ReportProgress(0, p);
                }
            };

            worker.ProgressChanged += (sendesr, es) =>
            {
                var godz = es.UserState as Godzina;

                var temp = new GodzinaHours();

                temp.godziny = new List<string>();
                temp.id = num++;
                temp.name = godz.getName();
                temp.nid = godz.nid;

                temp.godziny = godz.godziny_full.Split('#').ToList();
                var index_last = temp.godziny.IndexOf(temp.godziny.Last());
                temp.godziny.RemoveAt(index_last);

                LiniaGodziny.Add(temp);
            };

            worker.RunWorkerCompleted += (senders, es) =>
            {
                if (es.Cancelled)
                    return;

                BackgroundWorker worker1 = new BackgroundWorker();

                worker1.DoWork += (se, fe) =>
                {
                    var literki = SQLServices.getData<Literka>(0, "SELECT info FROM Literka WHERE id_przystanku = ?", przystanek.id);

                    przystanek.literki_info = new List<string>();

                    foreach (var lit in literki)
                        przystanek.literki_info.Add(lit.info);
                };

                worker1.RunWorkerCompleted += (se, fe) =>
                {
                    if (przystanek.literki_info != null && przystanek.literki_info.Count() > 0)
                        przystanek.literki_info.ForEach(p => literki.Add(p));

                    if (przystanek.godziny.Count() == 0)
                        DodatkoweInformacje.Text = "Dodatkowe informacje: Z przystanku " + przystanek.getName() + " nie są realizowane żadne odjazdy.";
                    else
                    {
                        if (trasa.stops.ElementAt(trasa.stops.IndexOf(trasa.stops.Last())) == przystanek)
                        {
                            DodatkoweInformacje.Text = "Dodatkowe informacje: " + przystanek.getName() + " jest ostatnim przystankiem na trasie linii nr " +
                            selectedLinia.name + ". Prezentowane godziny są godzinami przyjazdu.";
                        }
                    }

                    MainWindowLinesINfoHoursProgressRing.Visibility = Visibility.Collapsed;

                    if (!MainPage.gui.isAnyStopInList())
                    {
                        MainPage.gui.setStopListStops(trasa.stops);
                        MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(przystanek);
                    }
                    else
                        MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(przystanek);
                };

                worker1.RunWorkerAsync();

            };

            worker.RunWorkerAsync();
        }

        private void MainWindowLinesInfoHours_Unloaded(object sender, RoutedEventArgs e) => clear();

        protected override void OnNavigatedFrom(NavigationEventArgs e) => clear();

        private void clear()
        {
            selectedLinia = null; ;
            LiniaGodziny = null;
            przystanek = null;
            selectedLinia = null;
            LiniaGodziny = null;
            literki = null;
            przystanek = null;
            was = null;
            //selectedHour = null;
            gui = null;
            trasa = null;

            this.SizeChanged -= MainWindowLinesInfoHours_SizeChanged;
            this.Unloaded -= MainWindowLinesInfoHours_Unloaded;
            this.Loaded -= MainWindowLinesInfoHours_Loaded;
        }
        private void MainWindowLinesInfoHours_SizeChanged(object sender, SizeChangedEventArgs e) => MainWindowLinesInfoHOursKierunek.Width = e.NewSize.Width - 100;

        //from stackoverflow
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                        yield return (T)child;

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                        yield return childOfChild;
                }
        }
        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            foreach (GridView gr in FindVisualChildren<GridView>(this))
            {
                if (gr == sender)
                    continue;

                gr.SelectedIndex = -1;
            }

            if (!MainPage.gui.isStopListPaneOpen)
                MainPage.gui.isStopListPaneOpen = true;

            MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(przystanek);

            /* selectedHour = e.ClickedItem.ToString();

             var a = HTMLServices.godzinynames.Where(p => p.name == 
                 (((sender as GridView).Parent as StackPanel).Children[0] as TextBlock).
                     Text).ToList();

             int num = LiniaGodziny.Where(p => p.nid == a[0].id).ToList()[0].nid;
             int num2 = LiniaGodziny.Where(p => p.nid == num).First().godziny.IndexOf(e.ClickedItem.ToString());

             var temp = selectedHour.Split(':');
             var n1 = int.Parse(temp[0]);

             int n = -1, n2 = -1;

             if (!int.TryParse(temp[1], out n))
             {
                 temp[1] = temp[1].Replace(temp[1].Last(), ' ').ToString().Trim();
                 n2 = int.Parse(temp[1]);
             }
             temphour = new int[] { 0, 0 };

             temphour[0] = n1;
             temphour[1] = n2 == -1 ? n : n2;

             temphour2[0] = -1;
             temphour2[1] = -1;

             temphour3[0] = num;
             temphour3[1] = num2;
             OnClickHour?.Invoke(e.ClickedItem.ToString(), num, num2);

             /*var lista_new = lista_test2.OrderBy(p => trasa.stops.IndexOf(p));
             var lista_neww = lista_test.OrderBy(p => trasa.stops.IndexOf((p.DataContext as Przystanek)));

             foreach (var item in lista_neww)
                 item.test();*/
        }

        private void MainWindowLinesInfoHOursLinia_Tapped(object sender, TappedRoutedEventArgs e)
        {
            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
            navigated_from = true;
        }

        private void MainWindowLinesInfoHOursPrzystanek_Tapped(object sender, TappedRoutedEventArgs e)
        {
            navigated_from = true;
            MainPage.gui.setViewPage = typeof(MainWindowStopList);
            MainWindowStopList.preparefromfav(HTMLServices.przystankinames.ElementAt(MainWindowLinesInfo.selectedPrzystanek.nid));
        }
    }
}
