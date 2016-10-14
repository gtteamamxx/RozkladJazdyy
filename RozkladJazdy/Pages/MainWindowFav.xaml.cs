using RozkladJazdy.Model;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowFav : Page
    {
        private ObservableCollection<Ulubiony> favourite_lines;
        private ObservableCollection<Ulubiony> favourite_stops;

        public static bool isNavigatedFromThisPage = false;
        public static bool? isPageLoaded = null;

        public MainWindowFav()
        {
            this.InitializeComponent();

            isPageLoaded = false;

            favourite_lines = new ObservableCollection<Ulubiony>();
            favourite_stops = new ObservableCollection<Ulubiony>();

            MainWindowStopList.OnLoaded += () => RefreshFavouriteList();

            MainPage.OnAddedFavouriteStop += (type, _object, delete) =>
            {
                if (type == 0) // linia
                {
                    if (!delete)
                        favourite_lines.Add(_object as Ulubiony);
                    else
                        favourite_lines.Remove(favourite_lines.Where(p => p.name == (_object as string)).ToList().First());
                }
                else if (type == 1)
                {
                    if (!delete)
                        favourite_stops.Add(_object as Ulubiony);
                    else
                        favourite_stops.Remove(favourite_stops.Where(p => p.name == (_object as string)).ToList().First());
                }
            };

            RefreshFavouriteList();
        }

        public void RefreshFavouriteList()
        {
            favourite_lines.Clear();
            favourite_stops.Clear();

            foreach (Ulubiony fav in (MainPage.favourite_stops.OrderBy(p => p.name)))
                if (fav.type == 0) // linia
                    favourite_lines.Add(fav);
                else
                    favourite_stops.Add(fav);

            FavLinesNum.Text = "Ulubionych linii: " + favourite_lines.Count().ToString();
            FavStopsNum.Text = "Ulubionych przystanków: " + favourite_stops.Count().ToString();

            MainWindowFavNoFavLines.Visibility = favourite_lines.Count() == 0 ? Visibility.Visible : Visibility.Collapsed;
            MainWindowFavNoFavStops.Visibility = favourite_stops.Count() == 0 ? Visibility.Visible : Visibility.Collapsed;

            isPageLoaded = true;
            MainWindowFavStatusProgressRing.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Ulubione";
            MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
        }

        private void MainWindowFavFavouriteList_Click(object sender, ItemClickEventArgs e)
        {
            if (isPageLoaded == false)
                return;

            var clickedFavItem = e.ClickedItem as Ulubiony;
            isNavigatedFromThisPage = true;

            if (clickedFavItem.type == 0) // linia
            {
                MainWindowLinesList.selected_line = new Linia();
                MainWindowLinesList.selected_schedule = new int();

                MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;

                MainWindowLinesList.selected_line = MainWindow.lines[clickedFavItem.id];

                var liczba_rozkladow = SQLServices.getData<Rozklad>(0, "SELECT id FROM Rozklad where id_linia = ?", MainWindowLinesList.selected_line.id).Count();

                if (liczba_rozkladow > 1)
                {
                    MainPage.gui.setViewPage = typeof(MainWindowLinesSchedule);
                    return;
                }

                MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
            }
            else // przystanek
            {
                MainPage.gui.setViewPage = typeof(MainWindowStopList);
                MainWindowStopList.preparefromfav(HTMLServices.przystankinames.ElementAt(clickedFavItem.id));   
            }
        }
    }
}
