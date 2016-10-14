using RozkladJazdy.Model;
using RozkladJazdy.Model.RozkladJazy.Modelnet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RozkladJazdy.Pages;
using System.ComponentModel;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowStopList : Page
    {
        private List<NazwaPrzystanku> stop_list => HTMLServices.przystankinames.OrderBy(p => p.name).ToList();
        private ObservableCollection<NazwaPrzystanku> searched_stop_list = new ObservableCollection<NazwaPrzystanku>();
        private List<PrzystanekListaPrzystanków> lines_list;
        private NazwaPrzystanku stop_name_temp = new NazwaPrzystanku();
        private ObservableCollection<PrzystanekListaPrzystanków2> clicked_lines_list;

        public delegate void eventLoaded();

        private static MainWindowStopList gui;

        public static bool isPageLoaded = false;
        public static bool isNavigatedFromThisPage = false;
        public static NazwaPrzystanku selected_stop;
        public static event eventLoaded OnPageLoaded;

        public MainWindowStopList()
        {
            this.InitializeComponent();

            gui = this;

            lines_list = new List<PrzystanekListaPrzystanków>();
            clicked_lines_list = new ObservableCollection<PrzystanekListaPrzystanków2>();
            selected_stop = new NazwaPrzystanku();

            lines_list = MainWindowSelect.temp_lista;

            MainWindowStopListStopsList.SelectionChanged += (se, fe) => (se as ListView).ScrollIntoView(stop_name_temp);

            this.Loaded += (s, f) =>
            {
                MainWindowSelect.temp_lista = null;

                MainWindowStopListStatusProgressRingLinesList.Visibility = Visibility.Collapsed;
                isPageLoaded = true;
                OnPageLoaded?.Invoke();
                MainWindowStopListStopsList.SelectionMode = ListViewSelectionMode.Single;
                MainWindowStopListSearchResultText.Text = "Przystanków: " + stop_list.Count().ToString();

                MainPage.gui.setFavouriteSubText = "przystanek";
                if (!string.IsNullOrEmpty(stop_name_temp.name))
                    MainWindowStopListStopsList.SelectedIndex = stop_list.IndexOf(stop_name_temp);
            }; 
        }

        private void MainWindowStopList_SizeChanged(object sender, SizeChangedEventArgs e) => changesize(sender, e.NewSize.Width);

        public void changesize(object _object, double _width = 0.00, bool autosuggestbox = false)
        {
            var width = _object == null ? this.Width : _width;

            if(autosuggestbox)
            {
                MainWindowStopListStopsList.Width = this.ActualWidth;
                MainWindowStopListSearchStopList.Width = this.ActualWidth;

                MainWindowStopListStopsList.HorizontalAlignment = HorizontalAlignment.Center;
                MainWindowStopListSearchStopList.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else if (MainWindowStopListLinesList.Visibility == Visibility.Collapsed)
            {
                MainWindowStopListStopsList.Width = width;
                MainWindowStopListSearchStopList.Width = width;

                MainWindowStopListStopsList.HorizontalAlignment = HorizontalAlignment.Center;
                MainWindowStopListSearchStopList.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                MainWindowStopListStopsList.Width = width / 2;
                MainWindowStopListSearchStopList.Width = width / 2;

                MainWindowStopListStopsList.HorizontalAlignment = HorizontalAlignment.Left;
                MainWindowStopListSearchStopList.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        public static void preparefromfav(NazwaPrzystanku stop)
        {
            if(gui.MainWindowStopListSearchStopList.Visibility == Visibility.Visible)
            {
                gui.MainWindowStopListSearchStopList.Visibility = Visibility.Collapsed;
                gui.MainWindowStopListStopsList.Visibility = Visibility.Visible;
            }

            Grid.SetColumnSpan(gui.MainWindowStopListStopsList, 1);
            gui.MainWindowStopListLinesList.Visibility = Visibility.Visible;

            gui.stop_name_temp = stop;
            gui.showListView2(stop);
        }
        //przystanek
        private void MainWindowStopListStopsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!isPageLoaded || (sender as ListView).SelectedItem == e.ClickedItem)
                return;

            Grid.SetColumnSpan(gui.MainWindowStopListStopsList, 1);
            changesize(null);

            MainWindowStopListLinesList.Visibility = Visibility.Visible;
            showListView2(e.ClickedItem as NazwaPrzystanku);
        }
        private void showListView2(NazwaPrzystanku clickeditem)
        {
            clicked_lines_list.Clear();

            if (MainWindowStopListStopsList.Visibility == Visibility.Visible)
            {
                MainWindowStopListStopsList.HorizontalAlignment = HorizontalAlignment.Left;
                MainWindowStopListStopsList.Width = this.ActualWidth / 2;
            }
            else
            {
                MainWindowStopListSearchStopList.HorizontalAlignment = HorizontalAlignment.Left;
                MainWindowStopListSearchStopList.Width = this.ActualWidth / 2;
            }

            var przystankinames = clickeditem as NazwaPrzystanku;

            //checks if stop is in favourites

            if (MainPage.isFavourite(przystankinames))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            MainWindowStopListStatusProgressRingLinesList.Visibility = Visibility.Visible;

            selected_stop = przystankinames;

            var filteredlist = lines_list.Where(p => p.przystanek_id == przystankinames.id).ToList().OrderBy(p => p.linia_id).ToList();

            BackgroundWorker worker = new BackgroundWorker();
            //worker.WorkerReportsProgress = true;

            worker.DoWork += (s, f) =>
            {
                List<PrzystanekListaPrzystanków2> temp_list = new List<PrzystanekListaPrzystanków2>();

                for (int i = 0; i < filteredlist.Count(); i++)
                {
                    var line_id = filteredlist[i].linia_id;
                    var schedule_id = filteredlist[i].rozklad_id;
                    var track_id = filteredlist[i].trasa_id;

                    var dest = SQLServices.getData<Trasa>(0, "SELECT name FROM Trasa WHERE (id_linia = ? AND id_rozklad = ?) LIMIT 2", line_id, schedule_id)[line_id].name;////MainWindow.lines[lid].rozklad[rid].track[tid].name;
                    var name = MainWindow.lines[track_id].name;

                    bool state = false;

                    for (int d = 0; d < temp_list.Count(); d++)
                    {
                        if (temp_list[d].nazwa_lini == name)
                        {
                            for (int y = 0; y < temp_list[d].rozklady.Count(); y++)
                            {
                                if (temp_list[d].rozklady[y].roz_id == schedule_id)
                                {
                                    var g = temp_list[d];

                                    if (g.rozklady[y].kierunki.Count() <= 1)
                                        g.rozklady[y].kierunki.Add(new PrzystanekListaPrzystanków4() { name = dest, rozk_id = schedule_id, track_id = track_id, line_id = line_id });

                                    temp_list[d] = g;
                                    state = true;
                                    break;
                                }
                            }

                            if (state)
                                break;

                            if (!state)
                            {
                                var g = temp_list[d];
                                var kierunkis = new List<PrzystanekListaPrzystanków4>();
                                kierunkis.Add(new PrzystanekListaPrzystanków4() { name = dest, rozk_id = schedule_id, track_id = track_id, line_id = line_id });

                                var text = SQLServices.getData<Rozklad>(0, "SELECT text FROM Rozklad WHERE id_linia = ?", line_id)[schedule_id].text;
                                g.rozklady.Add(new PrzystanekListaPrzystanków3() { kierunki = kierunkis, name = text, roz_id = schedule_id, line_id = line_id });

                                temp_list[d] = g;
                                state = true;
                                break;
                            }
                        }
                    }

                    if (state)
                        continue;

                    var h = new PrzystanekListaPrzystanków2();
                    var l = new List<PrzystanekListaPrzystanków4>();
                    var m = new PrzystanekListaPrzystanków3();

                    m.kierunki = new List<PrzystanekListaPrzystanków4>();
                    h.rozklady = new List<PrzystanekListaPrzystanków3>();

                    l.Add(new PrzystanekListaPrzystanków4() { name = dest, rozk_id = schedule_id, track_id = track_id, line_id = line_id });

                    m.roz_id = schedule_id;
                    m.name = SQLServices.getData<Rozklad>(0, "SELECT text FROM Rozklad WHERE id_linia = ?", line_id)[schedule_id].text;// MainWindow.lines[lid].rozklad[rid].text;
                    m.kierunki = l;
                    m.line_id = line_id;

                    h.rozklady.Add(m);

                    h.nazwa_lini = name;
                    h.line_id = line_id;
                    temp_list.Add(h);
                    // worker.ReportProgress(1, h);
                }

                f.Result = temp_list as List<PrzystanekListaPrzystanków2>;
            };

            /*worker.ProgressChanged += (s, f) =>
            {
                if (f.ProgressPercentage == 0)
                    lista3[(int)(f.UserState as List<object>)[0]] = (PrzystanekListaPrzystanków2)(f.UserState as List<object>)[1];
                else if(f.ProgressPercentage == 1)
                    lista3.Add(f.UserState as PrzystanekListaPrzystanków2);
            };*/

            worker.RunWorkerCompleted += (s, f) =>
            {
                (f.Result as List<PrzystanekListaPrzystanków2>).ForEach(p => clicked_lines_list.Add(p));
                MainWindowStopListStatusProgressRingLinesList.Visibility = Visibility.Collapsed;
            };

            worker.RunWorkerAsync();
        }
        //linia
        private void MainWindowStopListLinesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            reset(sender);
            changePage((e.ClickedItem as PrzystanekListaPrzystanków2).line_id);
        }
        private void changePage(int line_id, int schedule_id = -1, Przystanek stop_id = null)
        {
            if(MainWindowLinesList.selected_line == null || (MainWindowLinesList.selected_line != null && MainWindow.lines.IndexOf(MainWindowLinesList.selected_line) != line_id))
            {
                MainWindowLinesList.selected_line = new Linia();
                MainWindowLinesList.selected_line = MainWindow.lines[line_id];
            }

            MainWindowLinesList.selected_schedule = new int();
            MainWindowLinesList.selected_schedule = schedule_id;

            isNavigatedFromThisPage = true;

            MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;

            if (stop_id != null)
            {
                MainWindowLinesInfo.selected_stop = new Przystanek();
                MainWindowLinesInfo.selected_stop = stop_id;

                MainPage.gui.setViewPage = typeof(MainWindowLinesInfoHours);
                return;
            }
            else if(schedule_id == -1)
            {
                if (SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", line_id).Count() > 1)
                {
                    MainPage.gui.setViewPage = typeof(MainWindowLinesSchedule);
                    return;
                }
            }

            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
        }

        //rozklad
        private void MainWindowStopListLinesList_Schedule_ItemClick(object sender, ItemClickEventArgs e)
        {
            reset(sender);

            var schedule = e.ClickedItem as PrzystanekListaPrzystanków3;
            changePage(schedule.line_id, schedule.roz_id);
        }
        //kierunek
        private void MainWindowStopListLinesList_Stop_ItemClick(object sender, ItemClickEventArgs e)
        {
            reset(sender);

            var dest = e.ClickedItem as PrzystanekListaPrzystanków4;

            var line_id = dest.line_id;
            var schedule_id = dest.rozk_id;
            var track_id = dest.track_id;

            var listview = MainWindowStopListStopsList.Visibility == Visibility.Collapsed ? MainWindowStopListSearchStopList : MainWindowStopListStopsList;
            var temp = (listview.SelectedItem as NazwaPrzystanku);

            if (temp == null)
                throw new Exception();

            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (s, f) =>
            {
                var a = MainWindow.lines[line_id];
                var b = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", line_id);//a.rozklad[rozklad];

                MainWindowLinesList.selected_line = new Linia();
                MainWindowLinesList.selected_line = MainWindow.lines[line_id];

                MainWindowLinesList.selected_line.rozklad = new List<Rozklad>();
                MainWindowLinesList.selected_line.rozklad = b;

                var c = SQLServices.getData<Trasa>(0, "SELECT * FROM Trasa WHERE (id_linia = ? AND id_rozklad = ?) LIMIT 2", line_id, schedule_id);//b.track[trasa];
                MainWindowLinesList.selected_line.rozklad[schedule_id].track = new List<Trasa>();
                MainWindowLinesList.selected_line.rozklad[schedule_id].track = c;

                var d = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE id_trasa = ? AND id_rozklad = ?", c[track_id].id, b[schedule_id].id, temp.id);//c.stops.Where(p => p.nid == temp.id).ToList();
                MainWindowLinesList.selected_line.rozklad[schedule_id].track[track_id].stops = new List<Przystanek>();
                MainWindowLinesList.selected_line.rozklad[schedule_id].track[track_id].stops = d;

                f.Result = d.Where(p => p.nid == temp.id).First() as Przystanek;
            };
            worker.RunWorkerCompleted += (s, f) => changePage(line_id, schedule_id, f.Result as Przystanek);
            worker.RunWorkerAsync();
            
        }

        private void reset(object sender)
        {
            isNavigatedFromThisPage = false;
            foreach (ListView gr in MainWindowLinesInfoHours.FindVisualChildren<ListView>(this))
            {
                if (gr == sender || gr == MainWindowStopListStopsList || gr == MainWindowStopListSearchStopList)
                    continue;

                gr.SelectedIndex = -1; 
            }
        }

        private void MainWindowStopListSearchStopList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainWindowStopListLinesList.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(MainWindowStopListSearchStopList, 1);
            changesize(null);

            showListView2(e.ClickedItem as NazwaPrzystanku);
        }

        private void MainWindowStopListAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var message = sender.Text.Trim();

            clicked_lines_list.Clear();
            searched_stop_list.Clear();
            MainWindowStopListLinesList.Visibility = Visibility.Collapsed;
            MainWindowStopListNoStopsText.Visibility = Visibility.Collapsed;
            changesize(null, 0.00, true);

            if (message == "")
            {
                MainWindowStopListSearchResultText.Text = "Przystanków: " + stop_list.Count().ToString();
                
                Grid.SetColumnSpan(MainWindowStopListSearchStopList, 2);
                Grid.SetColumnSpan(MainWindowStopListStopsList, 2);
                MainWindowStopListStopsList.Visibility = Visibility.Visible;

                return;
            }

            Grid.SetColumnSpan(MainWindowStopListSearchStopList, 2);


            MainWindowStopListStopsList.Visibility = Visibility.Collapsed;
            MainWindowStopListSearchStopList.Visibility = Visibility.Visible;

            BackgroundWorker worker = new BackgroundWorker();

            var stops_name = HTMLServices.przystankinames as List<NazwaPrzystanku>;

            worker.DoWork += (s, f) => f.Result = stops_name.Where(p => normalize(p.name.ToLower()).Contains(normalize(message.ToLower()))).OrderBy(p => p.name).ToList();
            worker.RunWorkerCompleted += (s, f) =>
            {
                stops_name = null;
                foreach (var a in f.Result as List<NazwaPrzystanku>)
                    searched_stop_list.Add(new NazwaPrzystanku() { id = a.id, name = a.name });

                MainWindowStopListSearchResultText.Text = "Przystanków: " + searched_stop_list.Count().ToString();

                if(searched_stop_list.Count() == 0 )
                    MainWindowStopListNoStopsText.Visibility = Visibility.Visible;

                MainWindowStopListStatusProgressRingStopList.Visibility = Visibility.Collapsed;
            };

            MainWindowStopListStatusProgressRingStopList.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();

        }

        private string normalize(string input) => input.Replace('ą', 'a').Replace('ę', 'e').Replace('ó', 'o').Replace('ś', 's').Replace
                ('ł', 'l').Replace('ż', 'z').Replace('ź', 'z').Replace('ć', 'c').Replace('ń', 'n');

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(selected_stop != null && selected_stop.name != null)
                MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            if (MainPage.isFavourite(selected_stop))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setPageTitle = "Rozkład jazdy -> Lista przystanków";

            reset(new object());
            this.SizeChanged += MainWindowStopList_SizeChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            stop_name_temp = new NazwaPrzystanku();
            this.SizeChanged -= MainWindowStopList_SizeChanged;
        }
    }
}
