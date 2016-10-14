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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowLinesInfo : Page
    {
        private ObservableCollection<Przystanek> stops_dest1;
        private ObservableCollection<Przystanek> stops_dest2;
        public static int selectedScheduleIndex;
        private double page_width;

        public static MainWindowLinesInfo gui;
        public static Linia line;
        public static List<MainWindowLinesInfoFirst> usercontrol_mainwindowlinesinfofirst_list;
        public static Przystanek selected_stop;
        public static Rozklad selected_schedule;
        public static double getPageWidth => gui.MainWindowLinesInfoStopsList2.Visibility == Visibility.Collapsed ? gui.page_width - 20.0 : gui.MainWindowLinesInfoStopsList2.Width;
        public static Visibility getSecondDestVisibility => gui.MainWindowLinesInfoStopsList2.Visibility;

        public bool isFirstTimePageOpened = false;
        public static bool isPageLoaded = false;

        public MainWindowLinesInfo()
        {
            this.InitializeComponent();

            MainPage.OnTimeTableRefesh += ClearDestsLists;

            gui = this;

            line = new Linia();
            selected_schedule = new Rozklad();

            MainPage.gui.setBackButtonVisibility = Visibility.Visible;

            stops_dest1 = new ObservableCollection<Przystanek>();
            stops_dest2 = new ObservableCollection<Przystanek>();

            usercontrol_mainwindowlinesinfofirst_list = new List<MainWindowLinesInfoFirst>();
            Color color = new Color();

            color = Colors.LightSlateGray;
            color.A = 30;

            MainWindowLinesInfoStopsList1.Background = new SolidColorBrush(color);
            MainWindowLinesInfoStopsList2.Background = new SolidColorBrush(color);

            isFirstTimePageOpened = true;
        }


        public void ClearDestsLists()
        {
            usercontrol_mainwindowlinesinfofirst_list.Clear();
            stops_dest1.Clear();
            stops_dest2.Clear();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainPage.gui.setFavouriteSubText = "linia";
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            if (MainPage.isFavourite(MainWindowLinesList.selectedLine))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            if (!isFirstTimePageOpened && (line.id == MainWindowLinesList.selectedLine.id && isPageLoaded) && stops_dest1.Count() > 0)
            {
                bool _return = false;
                int new_schedule_index = selectedScheduleIndex = MainWindowLinesList.selectedRozklad == -1 ? 0 : MainWindowLinesList.selectedRozklad;
                int old_schedule_index = -1;

                if (usercontrol_mainwindowlinesinfofirst_list.Count() > 0)
                {
                    old_schedule_index = usercontrol_mainwindowlinesinfofirst_list[0].getStop.rozkladzien_id;

                    if ((new_schedule_index == old_schedule_index))
                        _return = true;
                }

                if (!isPageLoaded || _return)
                    return;
            }

            isPageLoaded = false;
            isFirstTimePageOpened = false;

            ClearDestsLists();

            if (MainWindowLinesList.selectedLine.id != line.id && MainWindowLinesList.selectedLine.rozklad == null)
                MainWindowLinesList.selectedRozklad = -1;

            selectedScheduleIndex = MainWindowLinesList.selectedRozklad == -1 ? 0 : MainWindowLinesList.selectedRozklad;
            selected_schedule = new Rozklad();

            string bus_description = (MainWindowLinesList.selectedLine.getPfmText(MainWindowLinesList.selectedLine.pfm));

            MainWindowLinesInfoStatusProgressRing1.Visibility = Visibility.Visible;
            MainWindowLinesInfoStatusProgressRing2.Visibility = Visibility.Visible;

            MainWindowLinesInfoImage.Text =  (bus_description.Contains("bus") 
                || bus_description.Contains("ini") || bus_description.Contains("otni")) 
                ? "\uEB47" : "\uEB4D";

            MainWindowLinesInfoLineName.Text = MainWindowLinesList.selectedLine.name;
            MainWindowLinesInfoLineType.Text = bus_description;

            if (MainWindowLinesList.selectedLine.rozklad == null || MainWindowLinesList.selectedLine.rozklad.Count() == 0)
            {
                MainWindowLinesList.selectedLine.rozklad = new List<Rozklad>();
                MainWindowLinesList.selectedLine.rozklad = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", MainWindowLinesList.selectedLine.id);
                selected_schedule = MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex];
            }
            else
                selected_schedule = MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex];

            selected_schedule.track = new List<Trasa>();
            MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track = new List<Trasa>();

            BackgroundWorker worker1 = new BackgroundWorker();

            worker1.DoWork += (se, fe) => fe.Result = SQLServices.getData<Trasa>(0, "SELECT * FROM Trasa WHERE (id_rozklad = ? AND id_linia = ?) LIMIT 2",
                selectedScheduleIndex, MainWindowLinesList.selectedLine.id);

            worker1.RunWorkerCompleted += (se, fe) =>
            {
                MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track = selected_schedule.track =
                    fe.Result as List<Trasa>;

                if (selected_schedule.text.Contains("zawie"))
                {
                    MainWindowLinesInfoDestName1.Text = "Linia zawieszona, spróbuj zaaktualizować rozkład.";
                    return;
                }
                else
                {
                    MainWindowLinesInfoDestName1.Text = "Kierunek: " + selected_schedule.track[0].name;
                    MainWindowLinesInfoSelectedSchedule.Text = "Rozkład: " + selected_schedule.text;

                    selected_schedule.track[0].stops = new List<Przystanek>();
                    MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track[0].stops = new List<Przystanek>();

                    BackgroundWorker worker2 = new BackgroundWorker();

                    worker2.DoWork += (see, fee) => fee.Result = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE (id_rozklad = ? AND id_trasa = ?);",
                        selected_schedule.id, selected_schedule.track[0].id);

                    worker2.RunWorkerCompleted += (see, fee) =>
                    {
                        if (MainWindowLinesList.selectedLine.rozklad == null || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() == 0)
                            || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() > 0 && MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track == null)
                            || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() > 0 && MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track.Count() == 0))
                            return;

                        MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track[0].stops = selected_schedule.track[0].stops = fee.Result as List<Przystanek>;

                        foreach (Przystanek p in selected_schedule.track[0].stops)
                            stops_dest1.Add(p);

                        MainWindowLinesInfoStatusProgressRing1.Visibility = Visibility.Collapsed;

                        if (selected_schedule.track != null && selected_schedule.track.Count >= 2)
                        {
                            if (MainWindowLinesInfoStopsList2.Visibility == Visibility.Collapsed)
                            {
                                MainWindowLinesInfoStopsList2.Visibility = Visibility.Visible;

                                Grid.SetColumnSpan(MainWindowLinesInfoStopsList1, 1);
                                Grid.SetColumnSpan(MainWindowLinesInfoStackPanelDest1, 1);
                                MainWindowLinesInfoStackPanelDest2.Visibility = Visibility.Visible; ;

                                foreach (var uc in usercontrol_mainwindowlinesinfofirst_list)
                                    uc.setWidth(0, getPageWidth);
                            }

                            MainWindowLinesInfoDestName2.Text = "Kierunek: " + selected_schedule.track[1].name;

                            selected_schedule.track[1].stops = new List<Przystanek>();
                            MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track[1].stops = new List<Przystanek>();

                            BackgroundWorker worker3 = new BackgroundWorker();

                            worker3.DoWork += (sea, fea) =>
                                fea.Result = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE (id_rozklad = ? AND id_trasa = ?);",
                                    selected_schedule.id, selected_schedule.track[1].id);

                            worker3.RunWorkerCompleted += (sea, fea) =>
                            {
                                MainWindowLinesList.selectedLine.rozklad[selectedScheduleIndex].track[1].stops = selected_schedule.track[1].stops = fea.Result as List<Przystanek>;

                                foreach (Przystanek p in selected_schedule.track[1].stops)
                                    stops_dest2.Add(p);

                                MainWindowLinesInfoStatusProgressRing2.Visibility = Visibility.Collapsed;
                                line.id = MainWindowLinesList.selectedLine.id;
                                isPageLoaded = true;
                            };

                            worker3.RunWorkerAsync();
                        }
                        else
                        {
                            MainWindowLinesInfoStopsList2.Visibility = Visibility.Collapsed;
                            Grid.SetColumnSpan(MainWindowLinesInfoStopsList1, 2);
                            Grid.SetColumnSpan(MainWindowLinesInfoStackPanelDest1, 2);

                            MainWindowLinesInfoStatusProgressRing2.Visibility = Visibility.Collapsed;
                            MainWindowLinesInfoStackPanelDest2.Visibility = Visibility.Collapsed;

                            foreach (var uc in usercontrol_mainwindowlinesinfofirst_list)
                                uc.setWidth(1, getPageWidth);
                            line.id = MainWindowLinesList.selectedLine.id;
                            isPageLoaded = true;
                        }

                    };

                    worker2.RunWorkerAsync();
                }
            };

            worker1.RunWorkerAsync();
        }

        //kierunek 1
        private void MainWindowLinesInfoStopsList1_Click(object sender, ItemClickEventArgs e)
        {
            var clicked_stop = e.ClickedItem as Przystanek;

            selected_stop = clicked_stop;

            MainWindowFav.isNavigatedFromThisPage = false;

            if (MainPage.gui.stops_track == 2)
                MainPage.gui.clearStopListStops();

            MainPage.gui.setViewPage = typeof(MainWindowLinesInfoHours);

            if (MainWindowLinesInfoStopsList2.Visibility != Visibility.Collapsed)
            {
                List<object> dest2_stops = MainWindowLinesInfoStopsList2.Items.Where(d => (d as Przystanek).getName() == clicked_stop.getName()).ToList();

                if (dest2_stops != null && dest2_stops.Count > 0)
                {
                    var stoplist_listview2 = MainWindowLinesInfoStopsList2;

                    stoplist_listview2.SelectionChanged += (s, f) =>
                        stoplist_listview2.ScrollIntoView(stoplist_listview2.SelectedItem);

                    stoplist_listview2.SelectedIndex = stoplist_listview2.Items.IndexOf(dest2_stops.First());
                }
            }
        }

        //kierunek 2
        private void MainWindowLinesInfoStopsList2_Click(object sender, ItemClickEventArgs e)
        {
            var clicked_stop = e.ClickedItem as Przystanek;

            selected_stop = clicked_stop;

            MainWindowFav.isNavigatedFromThisPage = false;

            if (MainPage.gui.stops_track == 1)
                MainPage.gui.clearStopListStops();

            MainPage.gui.setViewPage = typeof(MainWindowLinesInfoHours);

            var dest1_stops = MainWindowLinesInfoStopsList1.Items.Where(d => (d as Przystanek).getName() == clicked_stop.getName()).ToList();

            if (dest1_stops != null && dest1_stops.Count() > 0)
            {
                var stoplist_listview1 = MainWindowLinesInfoStopsList1 as ListView;
                stoplist_listview1.SelectionChanged += (s, f) =>
                    stoplist_listview1.ScrollIntoView(stoplist_listview1.SelectedItem);

                stoplist_listview1.SelectedIndex = stoplist_listview1.Items.IndexOf(dest1_stops.First());
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            page_width = e.NewSize.Width;
            foreach (var uc in usercontrol_mainwindowlinesinfofirst_list)
                uc.setWidth(getSecondDestVisibility == Visibility.Collapsed ? 1 : 0, getPageWidth);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Linia: " + MainWindowLinesList.selectedLine.name;
            this.SizeChanged += Page_SizeChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
            => this.SizeChanged -= Page_SizeChanged;

    }
}
