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
        private ObservableCollection<Przystanek> pk1;
        private ObservableCollection<Przystanek> pk2;
        public static int selectedRozkladIndex;
        private double PageWidth;

        public static MainWindowLinesInfo gui;
        public static Linia line;
        public static List<MainWindowLinesInfoFirst> cl;
        public static Przystanek selectedPrzystanek;
        public static Rozklad selectedRozklad;
        public static double getWidth => gui.MainWindowLinesInfoListView2.Visibility == Visibility.Collapsed ? gui.PageWidth - 20.0 : gui.MainWindowLinesInfoListView2.Width;
        public static Visibility getVisibility => gui.MainWindowLinesInfoListView2.Visibility;

        public bool first_time = false;
        public static bool loaded_to_finish = false;


        public MainWindowLinesInfo()
        {
            this.InitializeComponent();

            MainPage.OnRefreshRozklady += Refresh;

            gui = this;

            line = new Linia();
            selectedRozklad = new Rozklad();

            MainPage.gui.setBackButtonVisibility = Visibility.Visible;

            pk1 = new ObservableCollection<Przystanek>();
            pk2 = new ObservableCollection<Przystanek>();

            cl = new List<MainWindowLinesInfoFirst>();
            Color color = new Color();

            color = Colors.LightSlateGray;
            color.A = 30;

            MainWindowLinesInfoListView1.Background = new SolidColorBrush(color);
            MainWindowLinesInfoListView2.Background = new SolidColorBrush(color);

            first_time = true;

        }


        public void Refresh()
        {
            cl.Clear();
            pk1.Clear();
            pk2.Clear();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            MainPage.gui.setFavSubText = "linia";
            //check if Line is in favourite then set color
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Visible;

            if (MainPage.isFavourite(MainWindowLinesList.selectedLine))
                MainPage.gui.setFavouriteButtonColor = Colors.Black;
            else
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

            if (!first_time && (line.id == MainWindowLinesList.selectedLine.id && loaded_to_finish) && pk1.Count() > 0)
            {
                bool returnn = false;
                int rozklad_new = selectedRozkladIndex = MainWindowLinesList.selectedRozklad == -1 ? 0 : MainWindowLinesList.selectedRozklad;
                int rozklad_old = -1;

                if (cl.Count() > 0)
                {
                    rozklad_old = cl[0].getPrzystanek.rozkladzien_id;

                    if ((rozklad_new == rozklad_old))
                        returnn = true;
                }

                if (!loaded_to_finish)
                    returnn = true;

                if (returnn)
                    return;
            }

            loaded_to_finish = false;
            first_time = false;

            Refresh();

            if (MainWindowLinesList.selectedLine.id != line.id && MainWindowLinesList.selectedLine.rozklad == null)
                MainWindowLinesList.selectedRozklad = -1;

            selectedRozkladIndex = MainWindowLinesList.selectedRozklad == -1 ? 0 : MainWindowLinesList.selectedRozklad;
            selectedRozklad = new Rozklad();

            var result = (MainWindowLinesList.selectedLine.getPfmText(MainWindowLinesList.selectedLine.pfm));

            ProgressRing1.Visibility = Visibility.Visible;
            ProgressRing2.Visibility = Visibility.Visible;

            MainWindowLinesInfoImage.Text =  (result.Contains("bus") || result.Contains("ini")|| result.Contains("otni")) 
                ? "\uEB47" : "\uEB4D";

            MainWindowLinesInfoTextBlock.Text = MainWindowLinesList.selectedLine.name;
            MainWindowLinesInfoTextBlock2.Text = result;

            if (MainWindowLinesList.selectedLine.rozklad == null || MainWindowLinesList.selectedLine.rozklad.Count() == 0)
            {
                MainWindowLinesList.selectedLine.rozklad = new List<Rozklad>();
                MainWindowLinesList.selectedLine.rozklad = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", MainWindowLinesList.selectedLine.id);
                selectedRozklad = MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex];
            }
            else
                selectedRozklad = MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex];

            selectedRozklad.track = new List<Trasa>();
            MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track = new List<Trasa>();

            BackgroundWorker worker1 = new BackgroundWorker();

            worker1.DoWork += (se, fe) => fe.Result = SQLServices.getData<Trasa>(0, "SELECT * FROM Trasa WHERE (id_rozklad = ? AND id_linia = ?) LIMIT 2",
                selectedRozkladIndex, MainWindowLinesList.selectedLine.id);

            worker1.RunWorkerCompleted += (se, fe) =>
            {
                MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track = selectedRozklad.track =
                    fe.Result as List<Trasa>;

                if (selectedRozklad.text.Contains("zawie"))
                {
                    MainWindowLinesInfoTextBlock3.Text = "Linia zawieszona, spróbuj zaaktualizować rozkład.";
                    return;
                }
                else
                {
                    MainWindowLinesInfoTextBlock3.Text = "Kierunek: " + selectedRozklad.track[0].name;
                    MainWindowLinesInfoTextBlock5.Text = "Rozkład: " + selectedRozklad.text;

                    selectedRozklad.track[0].stops = new List<Przystanek>();
                    MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track[0].stops = new List<Przystanek>();

                    BackgroundWorker worker2 = new BackgroundWorker();

                    worker2.DoWork += (see, fee) => fee.Result = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE (id_rozklad = ? AND id_trasa = ?);",
                        selectedRozklad.id, selectedRozklad.track[0].id);

                    worker2.RunWorkerCompleted += (see, fee) =>
                    {
                        if (MainWindowLinesList.selectedLine.rozklad == null || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() == 0)
                            || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() > 0 && MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track == null)
                            || (MainWindowLinesList.selectedLine.rozklad != null && MainWindowLinesList.selectedLine.rozklad.Count() > 0 && MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track.Count() == 0))
                            return;

                        MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track[0].stops = selectedRozklad.track[0].stops = fee.Result as List<Przystanek>;

                        foreach (Przystanek p in selectedRozklad.track[0].stops)
                            pk1.Add(p);

                        ProgressRing1.Visibility = Visibility.Collapsed;

                        if (selectedRozklad.track != null && selectedRozklad.track.Count >= 2)
                        {
                            if (MainWindowLinesInfoListView2.Visibility == Visibility.Collapsed)
                            {
                                MainWindowLinesInfoListView2.Visibility = Visibility.Visible;

                                Grid.SetColumnSpan(MainWindowLinesInfoListView1, 1);
                                Grid.SetColumnSpan(MainWindowLinesInfoStackPanel1, 1);
                                MainWindowLinesInfoStackPanel2.Visibility = Visibility.Visible; ;

                                foreach (var uc in cl)
                                    uc.setWidth(0, getWidth);

                            }

                            MainWindowLinesInfoTextBlock4.Text = "Kierunek: " + selectedRozklad.track[1].name;

                            selectedRozklad.track[1].stops = new List<Przystanek>();
                            MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track[1].stops = new List<Przystanek>();

                            BackgroundWorker worker3 = new BackgroundWorker();

                            worker3.DoWork += (sea, fea) =>
                                fea.Result = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE (id_rozklad = ? AND id_trasa = ?);",
                                    selectedRozklad.id, selectedRozklad.track[1].id);

                            worker3.RunWorkerCompleted += (sea, fea) =>
                            {
                                MainWindowLinesList.selectedLine.rozklad[selectedRozkladIndex].track[1].stops = selectedRozklad.track[1].stops = fea.Result as List<Przystanek>;

                                foreach (Przystanek p in selectedRozklad.track[1].stops)
                                    pk2.Add(p);

                                ProgressRing2.Visibility = Visibility.Collapsed;
                                line.id = MainWindowLinesList.selectedLine.id;
                                loaded_to_finish = true;
                            };

                            worker3.RunWorkerAsync();
                        }
                        else
                        {
                            MainWindowLinesInfoListView2.Visibility = Visibility.Collapsed;
                            Grid.SetColumnSpan(MainWindowLinesInfoListView1, 2);
                            Grid.SetColumnSpan(MainWindowLinesInfoStackPanel1, 2);

                            ProgressRing2.Visibility = Visibility.Collapsed;
                            MainWindowLinesInfoStackPanel2.Visibility = Visibility.Collapsed;

                            foreach (var uc in cl)
                                uc.setWidth(1, getWidth);
                            line.id = MainWindowLinesList.selectedLine.id;
                            loaded_to_finish = true;
                        }

                    };

                    worker2.RunWorkerAsync();
                }
            };

            worker1.RunWorkerAsync();
        }

        //kierunek 1
        private void MainWindowLinesInfoListView1_ItemClick(object sender, ItemClickEventArgs e)
        {
            var przystanek = e.ClickedItem as Przystanek;

            selectedPrzystanek = przystanek;

            MainWindowFav.navigated_from = false;

            if (MainPage.gui.PrzystankiTrasa == 2)
                MainPage.gui.clearPrzystanki();

            MainPage.gui.setPage = typeof(MainWindowLinesInfoHours);

            if (MainWindowLinesInfoListView2.Visibility != Visibility.Collapsed)
            {
                var items = MainWindowLinesInfoListView2.Items.Where(d => (d as Przystanek).getName() == przystanek.getName()).ToList();

                if (items != null && items.Count > 0)
                {
                    MainWindowLinesInfoListView2.SelectionChanged += (s, f) =>
                        MainWindowLinesInfoListView2.ScrollIntoView(MainWindowLinesInfoListView2.SelectedItem);

                    MainWindowLinesInfoListView2.SelectedIndex = MainWindowLinesInfoListView2.Items.IndexOf(items.First());
                }
            }
        }

        //kierunek 2
        private void MainWindowLinesInfoListView2_ItemClick(object sender, ItemClickEventArgs e)
        {
            var przystanek = e.ClickedItem as Przystanek;

            selectedPrzystanek = przystanek;

            MainWindowFav.navigated_from = false;

            if (MainPage.gui.PrzystankiTrasa == 1)
                MainPage.gui.clearPrzystanki();

            MainPage.gui.setPage = typeof(MainWindowLinesInfoHours);

            var items = MainWindowLinesInfoListView1.Items.Where(d => (d as Przystanek).getName() == przystanek.getName()).ToList();

            if (items != null && items.Count > 0)
            {
                MainWindowLinesInfoListView1.SelectionChanged += (s, f) =>
                    MainWindowLinesInfoListView1.ScrollIntoView(MainWindowLinesInfoListView1.SelectedItem);

                MainWindowLinesInfoListView1.SelectedIndex = MainWindowLinesInfoListView1.Items.IndexOf(items.First());
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PageWidth = e.NewSize.Width;
            foreach (var uc in cl)
                uc.setWidth(getVisibility == Visibility.Collapsed ? 1 : 0, getWidth);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setTitle = "Rozkład jazdy -> Linia: " + MainWindowLinesList.selectedLine.name;
            this.SizeChanged += Page_SizeChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        =>
            this.SizeChanged -= Page_SizeChanged;

    }
}
