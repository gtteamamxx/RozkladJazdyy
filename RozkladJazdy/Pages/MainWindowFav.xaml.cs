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
        private ObservableCollection<Ulubiony> ulubionelinie;
        private ObservableCollection<Ulubiony> ulubioneprzystanki;

        public static bool navigated_from = false;

        public static bool? loaded = null;
        public MainWindowFav()
        {
            this.InitializeComponent();

            loaded = false;

            ulubionelinie = new ObservableCollection<Ulubiony>();
            ulubioneprzystanki = new ObservableCollection<Ulubiony>();

            MainWindowStopList.OnLoaded += () => refresh();

            MainPage.OnAddedFavouriteStop += (typ, obiekt, delete) =>
            {
                if (typ == 0) // linia
                {
                    if (!delete)
                        ulubionelinie.Add(obiekt as Ulubiony);
                    else
                        ulubionelinie.Remove(ulubionelinie.Where(p => p.name == (obiekt as string)).ToList().First());
                }
                else if (typ == 1)
                {
                    if (!delete)
                        ulubioneprzystanki.Add(obiekt as Ulubiony);
                    else
                        ulubioneprzystanki.Remove(ulubioneprzystanki.Where(p => p.name == (obiekt as string)).ToList().First());
                }
            };

            refresh();
        }

        public void refresh()
        {
            ulubionelinie.Clear();
            ulubioneprzystanki.Clear();

            foreach (var a in MainPage.favourite_stops)
                if (a.type == 0) // linia
                    ulubionelinie.Add(a);
                else
                    ulubioneprzystanki.Add(a);

            FavLinesNum.Text = "Ulubionych linii: " + ulubionelinie.Count().ToString();
            FavStopsNum.Text = "Ulubionych przystanków: " + ulubioneprzystanki.Count().ToString();

            NoFavLines.Visibility = ulubionelinie.Count() == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoFavStops.Visibility = ulubioneprzystanki.Count() == 0 ? Visibility.Visible : Visibility.Collapsed;

            loaded = true;
            ProgressRing.Visibility = Visibility.Collapsed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Ulubione";
            MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
            MainPage.gui.setFavouriteButtonVisibility = Visibility.Collapsed;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (loaded == false)
                return;

            var item = e.ClickedItem as Ulubiony;
            navigated_from = true;

            if (item.type == 0) // linia
            {
                MainWindowLinesList.selectedLine = new Linia();
                MainWindowLinesList.selectedRozklad = new int();

                MainPage.gui.setRefreshButtonVisibility = Visibility.Collapsed;

                MainWindowLinesList.selectedLine = MainWindow.Lines[item.id];

                var liczba_rozkladow = SQLServices.getData<Rozklad>(0, "SELECT id FROM Rozklad where id_linia = ?", MainWindowLinesList.selectedLine.id).Count();

                if (liczba_rozkladow > 1)
                {
                    MainPage.gui.setViewPage = typeof(MainWindowLinesRozkladDzien);
                    return;
                }

                MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
            }
            else // przystanek
            {
                MainPage.gui.setViewPage = typeof(MainWindowStopList);
                MainWindowStopList.preparefromfav(HTMLServices.przystankinames.ElementAt(item.id));   
            }
        }
    }
}
