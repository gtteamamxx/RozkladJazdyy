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
        private List<NazwaPrzystanku> lista => HTMLServices.przystankinames.OrderBy(p => p.name).ToList();
        private ObservableCollection<NazwaPrzystanku> lista_searchable = new ObservableCollection<NazwaPrzystanku>();
        private List<PrzystanekListaPrzystanków> lista2;
        private NazwaPrzystanku tempPrzystankiNames = new NazwaPrzystanku();
        private ObservableCollection<PrzystanekListaPrzystanków2> lista3;

        public delegate void eventLoaded();

        private static MainWindowStopList gui;

        public static bool loaded = false;
        public static bool navigated_from = false;
        public static NazwaPrzystanku selectedPrzystanek;
        public static event eventLoaded OnLoaded;

        public MainWindowStopList()
        {
            this.InitializeComponent();

            gui = this;

            lista2 = new List<PrzystanekListaPrzystanków>();
            lista3 = new ObservableCollection<PrzystanekListaPrzystanków2>();
            selectedPrzystanek = new NazwaPrzystanku();

            lista2 = MainWindowSelect.temp_lista;

            ListView1.SelectionChanged += (se, fe) => (se as ListView).ScrollIntoView(tempPrzystankiNames);

            this.Loaded += (s, f) =>
            {
                MainWindowSelect.temp_lista = null;

                ProgressRing1.Visibility = Visibility.Collapsed;
                loaded = true;
                OnLoaded?.Invoke();
                ListView1.SelectionMode = ListViewSelectionMode.Single;
                ResultText.Text = "Przystanków: " + lista.Count().ToString();

                MainPage.gui.setFavouriteSubText = "przystanek";
                if (!string.IsNullOrEmpty(tempPrzystankiNames.name))
                    ListView1.SelectedIndex = lista.IndexOf(tempPrzystankiNames);
            }; 
        }

        private void MainWindowStopList_SizeChanged(object sender, SizeChangedEventArgs e) => changesize(sender, e.NewSize.Width);

        public void changesize(object obiekt, double widths = 0.00, bool ms = false)
        {
            var width = obiekt == null ? this.Width : widths;

            if(ms)
            {
                ListView1.Width = this.ActualWidth;
                ListViewSearchable.Width = this.ActualWidth;

                ListView1.HorizontalAlignment = HorizontalAlignment.Center;
                ListViewSearchable.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else if (ResultListView.Visibility == Visibility.Collapsed)
            {
                ListView1.Width = width;
                ListViewSearchable.Width = width;

                ListView1.HorizontalAlignment = HorizontalAlignment.Center;
                ListViewSearchable.HorizontalAlignment = HorizontalAlignment.Center;
            }
            else
            {
                ListView1.Width = width / 2;
                ListViewSearchable.Width = width / 2;

                ListView1.HorizontalAlignment = HorizontalAlignment.Left;
                ListViewSearchable.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        public static void preparefromfav(NazwaPrzystanku pr)
        {
            if(gui.ListViewSearchable.Visibility == Visibility.Visible)
            {
                gui.ListViewSearchable.Visibility = Visibility.Collapsed;
                gui.ListView1.Visibility = Visibility.Visible;
            }

            Grid.SetColumnSpan(gui.ListView1, 1);
            gui.ResultListView.Visibility = Visibility.Visible;

            gui.tempPrzystankiNames = pr;
            gui.showListView2(pr);
        }
        //przystanek
        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!loaded || (sender as ListView).SelectedItem == e.ClickedItem)
                return;

            Grid.SetColumnSpan(gui.ListView1, 1);
            changesize(null);

            ResultListView.Visibility = Visibility.Visible;
            showListView2(e.ClickedItem as NazwaPrzystanku);
        }
        private void showListView2(NazwaPrzystanku clickeditem)
        {
            lista3.Clear();

            if (ListView1.Visibility == Visibility.Visible)
            {
                ListView1.HorizontalAlignment = HorizontalAlignment.Left;
                ListView1.Width = this.ActualWidth / 2;
            }
            else
            {
                ListViewSearchable.HorizontalAlignment = HorizontalAlignment.Left;
                ListViewSearchable.Width = this.ActualWidth / 2;
            }

            var przystankinames = clickeditem as NazwaPrzystanku;

            //checks if stop is in favourites

            if (MainPage.isFavourite(przystankinames))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            ProgressRing2.Visibility = Visibility.Visible;

            selectedPrzystanek = przystankinames;

            var filteredlist = lista2.Where(p => p.przystanek_id == przystankinames.id).ToList().OrderBy(p => p.linia_id).ToList();

            BackgroundWorker worker = new BackgroundWorker();
            //worker.WorkerReportsProgress = true;

            worker.DoWork += (s, f) =>
            {
                List<PrzystanekListaPrzystanków2> temp_list = new List<PrzystanekListaPrzystanków2>();

                for (int i = 0; i < filteredlist.Count(); i++)
                {
                    var lid = filteredlist[i].linia_id;
                    var rid = filteredlist[i].rozklad_id;
                    var tid = filteredlist[i].trasa_id;

                    var kier = SQLServices.getData<Trasa>(0, "SELECT name FROM Trasa WHERE (id_linia = ? AND id_rozklad = ?) LIMIT 2", lid, rid)[tid].name;////MainWindow.lines[lid].rozklad[rid].track[tid].name;
                    var name = MainWindow.lines[lid].name;

                    bool state = false;

                    for (int d = 0; d < temp_list.Count(); d++)
                    {
                        if (temp_list[d].nazwa_lini == name)
                        {
                            for (int y = 0; y < temp_list[d].rozklady.Count(); y++)
                            {
                                if (temp_list[d].rozklady[y].roz_id == rid)
                                {
                                    var g = temp_list[d];

                                    if (g.rozklady[y].kierunki.Count() <= 1)
                                        g.rozklady[y].kierunki.Add(new PrzystanekListaPrzystanków4() { name = kier, rozk_id = rid, track_id = tid, line_id = lid });

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
                                kierunkis.Add(new PrzystanekListaPrzystanków4() { name = kier, rozk_id = rid, track_id = tid, line_id = lid });

                                var text = SQLServices.getData<Rozklad>(0, "SELECT text FROM Rozklad WHERE id_linia = ?", lid)[rid].text;
                                g.rozklady.Add(new PrzystanekListaPrzystanków3() { kierunki = kierunkis, name = text, roz_id = rid, line_id = lid });

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

                    l.Add(new PrzystanekListaPrzystanków4() { name = kier, rozk_id = rid, track_id = tid, line_id = lid });

                    m.roz_id = rid;
                    m.name = SQLServices.getData<Rozklad>(0, "SELECT text FROM Rozklad WHERE id_linia = ?", lid)[rid].text;// MainWindow.lines[lid].rozklad[rid].text;
                    m.kierunki = l;
                    m.line_id = lid;

                    h.rozklady.Add(m);

                    h.nazwa_lini = name;
                    h.line_id = lid;
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
                (f.Result as List<PrzystanekListaPrzystanków2>).ForEach(p => lista3.Add(p));
                ProgressRing2.Visibility = Visibility.Collapsed;
            };

            worker.RunWorkerAsync();
        }
        //linia
        private void ListView_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            reset(sender);

            var linia = e.ClickedItem as PrzystanekListaPrzystanków2;

            changePage(linia.line_id);
        }
        private void changePage(int lineid, int rozkladid = -1, Przystanek przid = null)
        {
            if(MainWindowLinesList.selectedLine == null || (MainWindowLinesList.selectedLine != null && MainWindow.lines.IndexOf(MainWindowLinesList.selectedLine) != lineid))
            {
                MainWindowLinesList.selectedLine = new Linia();
                MainWindowLinesList.selectedLine = MainWindow.lines[lineid];
            }

            MainWindowLinesList.selectedRozklad = new int();
            MainWindowLinesList.selectedRozklad = rozkladid;

            navigated_from = true;

            MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;

            if (przid != null)
            {
                MainWindowLinesInfo.selectedPrzystanek = new Przystanek();
                MainWindowLinesInfo.selectedPrzystanek = przid;

                MainPage.gui.setViewPage = typeof(MainWindowLinesInfoHours);
                return;
            }
            else if(rozkladid == -1)
            {
                if (SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", lineid).Count() > 1)
                {
                    MainPage.gui.setViewPage = typeof(MainWindowLinesRozkladDzien);
                    return;
                }
            }

            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
        }

        //rozklad
        private void ListView_ItemClick_2(object sender, ItemClickEventArgs e)
        {
            reset(sender);

            var rozklady = e.ClickedItem as PrzystanekListaPrzystanków3;

            changePage(rozklady.line_id, rozklady.roz_id);
        }
        //kierunek
        private void ListView_ItemClick_3(object sender, ItemClickEventArgs e)
        {
            reset(sender);

            var kierunki = e.ClickedItem as PrzystanekListaPrzystanków4;

            var linia = kierunki.line_id;
            var rozklad = kierunki.rozk_id;
            var trasa = kierunki.track_id;

            var listview = ListView1.Visibility == Visibility.Collapsed ? ListViewSearchable : ListView1;
            var temp = (listview.SelectedItem as NazwaPrzystanku);

            if (temp == null)
                throw new Exception();

            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (s, f) =>
            {
                var a = MainWindow.lines[linia];
                var b = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", linia);//a.rozklad[rozklad];

                MainWindowLinesList.selectedLine = new Linia();
                MainWindowLinesList.selectedLine = MainWindow.lines[linia];

                MainWindowLinesList.selectedLine.rozklad = new List<Rozklad>();
                MainWindowLinesList.selectedLine.rozklad = b;

                var c = SQLServices.getData<Trasa>(0, "SELECT * FROM Trasa WHERE (id_linia = ? AND id_rozklad = ?) LIMIT 2", linia, rozklad);//b.track[trasa];
                MainWindowLinesList.selectedLine.rozklad[rozklad].track = new List<Trasa>();
                MainWindowLinesList.selectedLine.rozklad[rozklad].track = c;

                var d = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE id_trasa = ? AND id_rozklad = ?", c[trasa].id, b[rozklad].id, temp.id);//c.stops.Where(p => p.nid == temp.id).ToList();
                MainWindowLinesList.selectedLine.rozklad[rozklad].track[trasa].stops = new List<Przystanek>();
                MainWindowLinesList.selectedLine.rozklad[rozklad].track[trasa].stops = d;

                f.Result = d.Where(p => p.nid == temp.id).First() as Przystanek;
            };
            worker.RunWorkerCompleted += (s, f) => changePage(linia, rozklad, f.Result as Przystanek);
            worker.RunWorkerAsync();
            
        }

        private void reset(object sender)
        {
            navigated_from = false;
            foreach (ListView gr in MainWindowLinesInfoHours.FindVisualChildren<ListView>(this))
            {
                if (gr == sender || gr == ListView1 || gr == ListViewSearchable)
                    continue;

                gr.SelectedIndex = -1; 
            }
        }

        private void ListViewSearchable_ItemClick(object sender, ItemClickEventArgs e)
        {
            ResultListView.Visibility = Visibility.Visible;
            Grid.SetColumnSpan(ListViewSearchable, 1);
            changesize(null);

            showListView2(e.ClickedItem as NazwaPrzystanku);
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var txt = SearchBox.Text.Trim();

            if (txt == "")
            {
                ResultText.Text = "Przystanków: " + lista.Count().ToString();
                lista3.Clear();
                lista_searchable.Clear();
                ResultListView.Visibility = Visibility.Collapsed;

                Grid.SetColumnSpan(ListViewSearchable, 2);
                Grid.SetColumnSpan(ListView1, 2);

                changesize(null, 0.00, true);
                ListView1.Visibility = Visibility.Visible;
                ListViewSearchable.Visibility = Visibility.Collapsed;
                BrakWynikowTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            lista3.Clear();
            lista_searchable.Clear();
            ResultListView.Visibility = Visibility.Collapsed;
            Grid.SetColumnSpan(ListViewSearchable, 2);

            changesize(null, 0.00, true);

            ListView1.Visibility = Visibility.Collapsed;
            ListViewSearchable.Visibility = Visibility.Visible;
            BrakWynikowTextBlock.Visibility = Visibility.Collapsed;

            BackgroundWorker worker = new BackgroundWorker();

            var list = HTMLServices.przystankinames;

            worker.DoWork += (s, f) => f.Result = list.Where(p => normalize(p.name.ToLower()).Contains(normalize(txt.ToLower()))).OrderBy(p => p.name).ToList();
            worker.RunWorkerCompleted += (s, f) =>
            {
                list = null;
                foreach (var a in f.Result as List<NazwaPrzystanku>)
                    lista_searchable.Add(new NazwaPrzystanku() { id = a.id, name = a.name });

                ResultText.Text = "Przystanków: " + lista_searchable.Count().ToString();

                if(lista_searchable.Count() == 0 )
                    BrakWynikowTextBlock.Visibility = Visibility.Visible;

                ProgressRing1.Visibility = Visibility.Collapsed;
            };

            ProgressRing1.Visibility = Visibility.Visible;
            worker.RunWorkerAsync();

        }

        private string normalize(string input) => input.Replace('ą', 'a').Replace('ę', 'e').Replace('ó', 'o').Replace('ś', 's').Replace
                ('ł', 'l').Replace('ż', 'z').Replace('ź', 'z').Replace('ć', 'c').Replace('ń', 'n');

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(selectedPrzystanek != null && selectedPrzystanek.name != null)
                MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            if (MainPage.isFavourite(selectedPrzystanek))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            MainPage.gui.setPageTitle = "Rozkład jazdy -> Lista przystanków";

            reset(new object());
            this.SizeChanged += MainWindowStopList_SizeChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            tempPrzystankiNames = new NazwaPrzystanku();
            this.SizeChanged -= MainWindowStopList_SizeChanged;
        }
    }
}
