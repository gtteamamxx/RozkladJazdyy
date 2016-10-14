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
        private Linia selected_line;
        private ObservableCollection<GodzinaHours> line_hours;
        private ObservableCollection<string> letters_info;
        private Przystanek selected_stop = new Przystanek();
        private Trasa selected_track;

        private MainWindowLinesInfoHours gui;

        //public static List<MainWindowLinesInfoThird> lista_test = new List<MainWindowLinesInfoThird>();
        //public static List<Przystanek> lista_test2 = new List<Przystanek>();

        public static bool? isClosestHour_temp = null;
        public static int closest_hour;
        public static int closest_minute;
        public static bool navigated_from;

        //public static string selectedHour;
        //public static int[] temphour = { 0, 0 };
        //public static int[] temphour2 = { 0, 0 };
        //public static int[] temphour3 = { 0, 0 };

        //public delegate void eventClickHour(string hour, int godzid, int godzid2);
        //public static event eventClickHour OnClickHour;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (selected_line != null)
                MainPage.gui.setPageTitle = "Rozkład jazdy -> Linia: " + selected_line.name;
        }

        public MainWindowLinesInfoHours()
        {
            this.InitializeComponent();
            gui = this;

            selected_line = new Linia();
            line_hours = new ObservableCollection<GodzinaHours>();
            letters_info = new ObservableCollection<string>();

            selected_line = MainWindowLinesList.selected_line;
            selected_stop = MainWindowLinesInfo.selected_stop;

            selected_track = selected_line.rozklad[MainWindowLinesList.selected_schedule == -1 ? 0 : 
                MainWindowLinesList.selected_schedule].track[selected_stop.track_id];

            if (MainPage.gui.stops_track != selected_stop.track_id)
                MainPage.gui.clearStopListStops();

            MainPage.gui.stops_track = selected_stop.track_id;
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Linia: " + selected_line.name;
            MainPage.gui.setStopListDestName("Kierunek: " + selected_track.name);

            MainWindowLinesInfoHoursDestName.Text = "Kierunek: " + selected_track.name;

            MainWindowLinesInfoHoursLineName.Text = selected_line.name;
            MainWindowLinesInfoHoursStopName.Text = "Przystanek: " + selected_stop.getName();

            this.Loaded += MainWindowLinesInfoHours_Loaded;
            this.Unloaded += MainWindowLinesInfoHours_Unloaded;
            this.SizeChanged += MainWindowLinesInfoHours_SizeChanged;
        }

        private void MainWindowLinesInfoHours_Loaded(object sender, RoutedEventArgs e)
        {
            MainPage.gui.setFavouriteSubText = "przystanek";
            //check if stop is in favourtie
            if (MainPage.isFavourite(HTMLServices.stops_name[MainWindowLinesInfo.selected_stop.nid]))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;


            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            int temp_hours_num = 0;
            worker.DoWork += (s, ef) =>
            {
                if (selected_stop == null || selected_stop.godziny == null)
                    selected_stop.godziny = new List<Godzina>();

                var stop_index = MainWindowLinesList.selected_line.rozklad[selected_stop.rozkladzien_id].
                        track[selected_stop.track_id].stops.IndexOf(selected_stop);

                if(stop_index == -1)
                {
                    ef.Cancel = true;
                    return;
                }
                selected_stop.godziny = MainWindowLinesList.selected_line.rozklad[selected_stop.rozkladzien_id].track[selected_stop.track_id].
                    stops[stop_index].godziny = new List<Godzina>();

                selected_stop.godziny = MainWindowLinesList.selected_line.rozklad[selected_stop.rozkladzien_id].track[selected_stop.track_id].
                    stops[stop_index].godziny = SQLServices.getData<Godzina>(0, "SELECT * FROM Godzina WHERE id_przystanek = ?", selected_stop.id);

                if (selected_stop == null || selected_stop.godziny == null)
                {
                    ef.Cancel = true;
                    return;
                }

                foreach (Godzina hour in selected_stop.godziny)
                {
                    if (selected_stop == null || selected_stop.godziny == null)
                    {
                        ef.Cancel = true;
                        return;
                    }
                    worker.ReportProgress(0, hour);
                }
            };

            worker.ProgressChanged += (s, f) =>
            {
                var hour = f.UserState as Godzina;

                var temp = new GodzinaHours();

                temp.godziny = new List<string>();
                temp.id = temp_hours_num++;
                temp.name = hour.getName();
                temp.nid = hour.nid;

                temp.godziny = hour.godziny_full.Split('#').ToList();
                var index_last = temp.godziny.IndexOf(temp.godziny.Last());
                temp.godziny.RemoveAt(index_last);

                line_hours.Add(temp);
            };

            worker.RunWorkerCompleted += (senders, es) =>
            {
                if (es.Cancelled)
                    return;

                BackgroundWorker worker1 = new BackgroundWorker();

                worker1.DoWork += (se, fe) =>
                {
                    var letters = SQLServices.getData<Literka>(0, "SELECT info FROM Literka WHERE id_przystanku = ?", selected_stop.id);

                    selected_stop.literki_info = new List<string>();

                    foreach (Literka let in letters)
                        selected_stop.literki_info.Add(let.info);
                };

                worker1.RunWorkerCompleted += (se, fe) =>
                {
                    if (selected_stop.literki_info != null && selected_stop.literki_info.Count() > 0)
                        selected_stop.literki_info.ForEach(p => letters_info.Add(p));

                    if (selected_stop.godziny.Count() == 0)
                        MainWindowLinesInfoHoursAdditionalInfo.Text = "Dodatkowe informacje: Z przystanku " + selected_stop.getName() + " nie są realizowane żadne odjazdy.";
                    else
                    {
                        if (selected_track.stops.ElementAt(selected_track.stops.IndexOf(selected_track.stops.Last())) == selected_stop)
                        {
                            MainWindowLinesInfoHoursAdditionalInfo.Text = "Dodatkowe informacje: " + selected_stop.getName() + " jest ostatnim przystankiem na trasie linii nr " +
                            selected_line.name + ". Prezentowane godziny są godzinami przyjazdu.";
                        }
                    }

                    MainWindowLinesInfoHoursStatusProgressRing.Visibility = Visibility.Collapsed;

                    if (!MainPage.gui.isAnyStopInList())
                    {
                        MainPage.gui.setStopListStops(selected_track.stops);
                        MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(selected_stop);
                    }
                    else
                        MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(selected_stop);
                };

                worker1.RunWorkerAsync();
            };

            worker.RunWorkerAsync();
        }

        private void MainWindowLinesInfoHours_Unloaded(object sender, RoutedEventArgs e) => clear();

        protected override void OnNavigatedFrom(NavigationEventArgs e) => clear();

        private void clear()
        {
            selected_line = null; ;
            line_hours = null;
            selected_stop = null;
            letters_info = null;
            isClosestHour_temp = null;
            //selectedHour = null;
            gui = null;
            selected_track = null;

            this.SizeChanged -= MainWindowLinesInfoHours_SizeChanged;
            this.Unloaded -= MainWindowLinesInfoHours_Unloaded;
            this.Loaded -= MainWindowLinesInfoHours_Loaded;
        }
        private void MainWindowLinesInfoHours_SizeChanged(object sender, SizeChangedEventArgs e) => MainWindowLinesInfoHoursDestName.Width = e.NewSize.Width - 100;

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

            MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(selected_stop);

            /* selectedHour = e.ClickedItem.ToString();

             var a = HTMLServices.hours_name.Where(p => p.name == 
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

        private void MainWindowLinesInfoHoursLineName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            navigated_from = true;
            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
        }

        private void MainWindowLinesInfoHoursStopName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            navigated_from = true;
            MainPage.gui.setViewPage = typeof(MainWindowStopList);
            MainWindowStopList.preparefromfav(HTMLServices.stops_name.ElementAt(MainWindowLinesInfo.selected_stop.nid));
        }
    }
}
