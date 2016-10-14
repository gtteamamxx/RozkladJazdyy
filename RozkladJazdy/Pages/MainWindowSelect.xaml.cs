using RozkladJazdy.Model;
using RozkladJazdy.Model.RozkladJazy.Modelnet;
using SQLite.Net;
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
    /// 
    public sealed partial class MainWindowSelect : Page
    {
        private static bool loaded = false;
        public static List<PrzystanekListaPrzystanków> temp_lista = new List<PrzystanekListaPrzystanków>();

        public MainWindowSelect()
        {
            this.InitializeComponent();

            MainPage.gui.setPageTitle = "Rozkład jazdy";
            MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;

            this.Loaded += (sender, e) =>
            {
                BackgroundWorker work = new BackgroundWorker();

                work.DoWork += (se, esf) =>
                {
                    HTMLServices.stops_name = new List<NazwaPrzystanku>();
                    HTMLServices.stops_name = SQLServices.getData<NazwaPrzystanku>(0, "SELECT * FROM NazwaPrzystanku");

                    HTMLServices.hours_name = new List<NazwaGodziny>();
                    HTMLServices.hours_name = SQLServices.getData<NazwaGodziny>(0, "SELECT * FROM NazwaGodziny");

                    //wczytaj ulubione
                    MainPage.favourite_stops = new ObservableCollection<Ulubiony>();

                    var num = SQLServices.getData<Ulubiony>(1, "SELECT * FROM sqlite_master WHERE name LIKE '%Ulubiony%'");

                    if (num.Count() == 0)
                        SQLServices.createDatabase<Ulubiony>(1);
                    else
                    {
                        num = SQLServices.getData<Ulubiony>(1, "SELECT * FROM Ulubiony");

                        foreach (var a in num)
                            MainPage.favourite_stops.Add(a);
                    }

                    temp_lista = SQLServices.getAllFromDataBase<PrzystanekListaPrzystanków>();
                };

                work.RunWorkerCompleted += (se, esf) =>
                {
                    MainWindowSelectResultStackPanel.Visibility = Visibility.Collapsed;
                    loaded = true;
                    MainWindow.isPageLoaded = true;
                };
                
                if(loaded == false)
                    work.RunWorkerAsync();
            };

        }

        //stops
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;

            if (MainWindowFav.isPageLoaded == null)
                MainPage.gui.setViewPage = typeof(MainWindowFav); // this same, cache if not exist;

            MainPage.gui.setViewPage = typeof(MainWindowStopList);

            MainPage.gui.setBackButtonVisibility = Visibility.Visible;
        }
        // favorite
        private void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;

            MainPage.gui.setViewPage = typeof(MainWindowFav);
            MainPage.gui.setBackButtonVisibility = Visibility.Visible;
        }
        // linie
        private void LinesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;

            MainPage.gui.setViewPage = typeof(MainWindowLinesList);
            MainPage.gui.setBackButtonVisibility = Visibility.Visible;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainWindowStopList.isNavigatedFromThisPage = false;

            MainPage.gui.setMenuSplitViewDisplayMode = SplitViewDisplayMode.Overlay;
            MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
            MainPage.gui.setStopButtonVisibility = Visibility.Collapsed;
            MainPage.gui.setBackButtonVisibility = Visibility.Collapsed;

            MainPage.gui.setPageTitle = "Rozkład jazdy";
            MainWindowLinesInfo.selectedScheduleIndex = -1;
            MainWindowLinesList.selected_schedule = new int();
        }
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) 
            =>    MainPage.gui.setMenuSplitViewDisplayMode = SplitViewDisplayMode.Inline;

        //Admin privvilages
        private int num = 0;

        private void Button2_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!MainPage.isAdmin)
            {
                num++;

                if (num == 15)
                {
                    MainPage.isAdmin = true;
                    MainPage.showInfo("Okeeej, już jesteś adminem :)");
                }
                return;
            }

            MainPage.showInfo("już jesteś przecież adminem :)");
        }
    }
}
