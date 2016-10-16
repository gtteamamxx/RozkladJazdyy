using RozkladJazdy.Model;
using RozkladJazdy.Model.RozkladJazy.Modelnet;
using RozkladJazdy.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RozkladJazdy
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int _favColor = 0;
        public ObservableCollection<Przystanek> stops = new ObservableCollection<Przystanek>();

        public Type setViewPage { get { return MainPageFrame.CurrentSourcePageType; } set { MainPageFrame.Navigate(value); } }
        public string setPageTitle { get { return MainPageTopPanelTitle.Text; } set { MainPageTopPanelTitle.Text = value; } }
        public string setFavouriteSubText { get { return MainPageTopPanelFavouriteButtonTypeText.Text; } set { MainPageTopPanelFavouriteButtonTypeText.Text = value; } }

        public Visibility setBackButtonVisibility { get { return MainPageTopPanelBackButton.Visibility; } set { if (DetectPlatform() == Platform.WindowsPhone) return; MainPageTopPanelBackButton.Visibility = value; }}
        public Visibility setRefreshButtonVisibility { get { return MainPageTopPanelRefreshButton.Visibility; } set { MainPageTopPanelRefreshButton.Visibility = value; } }
        public Visibility setFavouriteButtonVisibility { get { return MainPageTopPanelFavouriteButton.Visibility; } set { MainPageTopPanelFavouriteButton.Visibility = value; MainPageTopPanelFavouriteButtonTypeText.Visibility = value; } }
        public Visibility setStopButtonVisibility { get { return MainPageTopPanelStopsButton.Visibility; } set { MainPageTopPanelStopsButton.Visibility = value; } }
        public SplitViewDisplayMode setMenuSplitViewDisplayMode { get { return MainPageSplitView.DisplayMode; } set { MainPageSplitView.DisplayMode = value; } }

        public Color setFavouriteButtonColor { set { _favColor = value == Colors.LightGray ? 0 : 1; MainPageTopPanelFavouriteButton.Foreground = new SolidColorBrush(value); } }
        public Color getFavouriteButtonColor => _favColor == 0 ? Colors.LightGray : Colors.Black;

        public int setStopListActualIndex { get { return MainPageStopListStopsList.SelectedIndex; } set { MainPageStopListStopsList.SelectedIndex = value; } }
        public int getStopListActualIndex(Przystanek pr) => MainPageStopListStopsList.Items.IndexOf(pr);

        public string setStopListDestName(string value) => MainPageStopListDestText.Text = value;
        public bool isStopListPaneOpen { get { return MainPageStopsSplitView.IsPaneOpen; } set { MainPageStopsSplitView.IsPaneOpen = value; } }

        public void setStopListStops(List<Przystanek> st) { stops.Clear(); foreach (Przystanek p in st) stops.Add(p); }
        public void clearStopListStops() => stops.Clear();
        public bool isAnyStopInList() { return stops.Count > 0 ? true : false; }
        public int stops_track { get; set; }

        public static bool isAdmin = false;
        public static ObservableCollection<Ulubiony> favourite_stops = new ObservableCollection<Ulubiony>();
        
        public static eventAddedfav OnAddedFavouriteStop;
        public static eventRefresh OnTimeTableRefesh;

        public delegate void eventRefresh();
        public delegate void eventAddedfav(int _type = -1, object _object = null, bool _delete = false);

        public static MainPage gui;
        public MainPage()
        {
            this.InitializeComponent();
            gui = this;

            MainPageMenuList.SelectedIndex = 0;


            // Ustala, czy jestesmy na pc, czy mobile ; i daje kolor pasku titlebar i statusbar
            //mobile
            if (DetectPlatform() == Platform.WindowsPhone)
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.Blue;
                AsyncHelpers.RunSync(async () => await statusBar.ShowAsync());
                statusBar.BackgroundColor = Colors.White;
            }// pc
            else
            {
                ApplicationView.GetForCurrentView().TitleBar.BackgroundColor = Colors.White;
                ApplicationView.GetForCurrentView().TitleBar.ForegroundColor = Colors.Red;
            }

            // utawia widoczny guzik "cofnij" 
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            currentView.BackRequested += BackButtonPressed;

            MainPageStopListStopsList.SelectionChanged += (sender, e) =>
            {
                var list = (sender as ListView);
                if (list.SelectedItem != null)
                    list.ScrollIntoView(list.SelectedItem);
            };

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 500));

            MainPageFrame.Navigate(typeof(MainWindow));
            MainPageTopPanelTitle.Text = "Rozkład jazdy";
        }

        private void BackButtonPressed(object sender, BackRequestedEventArgs e)
        {
            bool isExit = false;
            goBack(ref isExit);

            if (!isExit && e != null)
                e.Handled = true;
        }

        //from so
        public Platform DetectPlatform() => (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons")) ? Platform.WindowsPhone : Platform.Windows;

        private void MainPageTopPanelStopsButton_Click(object sender, RoutedEventArgs e) => MainPageStopsSplitView.IsPaneOpen = !MainPageStopsSplitView.IsPaneOpen;

        private void MainPageTopPanelMenuButton_Click(object sender, RoutedEventArgs e) => MainPageSplitView.IsPaneOpen = !MainPageSplitView.IsPaneOpen;

        private void MainPageMenuList_Click(object sender, ItemClickEventArgs e)
        {
            var clickedItemName = ((e.ClickedItem as StackPanel).Children[1] as TextBlock);

            if (clickedItemName.Text.Contains("Roz") && MainWindow.isPageLoaded == false && MainWindow.isTimetableRefreshing == false) // rozklad jazdy
            {
                MainPageFrame.Navigate(typeof(MainWindow));
                MainPageTopPanelTitle.Text = "Rozkład jazdy";
            }
            else if (clickedItemName.Text.Contains("Roz") && MainWindow.isPageLoaded == true && MainWindow.isTimetableRefreshing == false)
            {
                MainPageFrame.Navigate(typeof(MainWindowSelect));
                MainPageTopPanelTitle.Text = "Rozkład jazdy";
            }
            else if (clickedItemName.Text.Contains("unika") && MainWindow.isPageLoaded == true && MainWindow.isTimetableRefreshing == false)
            {
                MainPageFrame.Navigate(typeof(Comunicates));
                MainPageTopPanelTitle.Text = "Komunikaty BETA";
            }
        }

        private void MainPageTopPanelBackButton_Click(object sender, RoutedEventArgs e) => BackButtonPressed(null, null);

        private void goBack(ref bool exit)
        {
            if (MainPageFrame.SourcePageType == typeof(MainWindowLinesInfoHours))
            {
                MainWindowLinesInfoHours.isClosestHour_temp = null;
                MainWindowLinesInfoHours.closest_hour = 0;
                //MainWindowLinesInfoHours.selectedHour = "";

                MainPageTopPanelStopsButton.Visibility = Visibility.Collapsed;

                if (MainWindowStopList.isNavigatedFromThisPage == true)
                {
                    if (!isStopClicked)
                        MainPageFrame.GoBack();
                    else
                    {
                        MainPageFrame.Navigate(typeof(MainWindowStopList));
                        isStopClicked = false;
                    }
                }
                else
                    MainPageFrame.Navigate(typeof(MainWindowLinesInfo));

            }
            else if (MainPageFrame.SourcePageType == typeof(MainWindowLinesInfo))
            {
                stops.Clear();
                gui.stops_track = -1;

                if (MainWindowLinesInfoHours.navigated_from == true)
                {
                    MainWindowLinesInfoHours.navigated_from = false;
                    MainPageFrame.GoBack();
                    return;
                }

                if (MainWindowFav.isNavigatedFromThisPage == true)
                {
                    MainWindowFav.isNavigatedFromThisPage = false;
                    MainPageFrame.GoBack();
                    return;
                }

                if (MainWindowStopList.isNavigatedFromThisPage == true)
                {
                    MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
                    MainPageFrame.GoBack();
                    return;
                }

                if (MainWindowLinesList.selected_line.rozklad.Count == 1)
                        MainPageFrame.Navigate(typeof(MainWindowLinesList));
                else
                {
                    MainPageTopPanelRefreshButton.Visibility = Visibility.Collapsed;
                    MainPageFrame.Navigate(typeof(MainWindowLinesSchedule));
                }
            }
            else if (MainPageFrame.SourcePageType == typeof(MainWindowLinesSchedule))
            {
                if (MainWindowFav.isNavigatedFromThisPage == true)
                {
                    MainWindowFav.isNavigatedFromThisPage = false;
                    MainPageFrame.GoBack();
                    return;
                }

                MainPageTopPanelRefreshButton.Visibility = Visibility.Visible;

                if (MainWindowStopList.isNavigatedFromThisPage == true)
                    MainPageFrame.GoBack();
                else
                    MainPageFrame.Navigate(typeof(MainWindowLinesList));
            }
            else if (MainPageFrame.SourcePageType == typeof(MainWindowLinesList)
                || MainPageFrame.SourcePageType == typeof(MainWindowStopList)
                || MainPageFrame.SourcePageType == typeof(MainWindowFav))
            {

                if (MainWindowLinesInfoHours.navigated_from == true)
                {
                    MainWindowLinesInfoHours.navigated_from = false;
                    MainPageFrame.GoBack();
                    return;
                }

                if (MainWindowFav.isNavigatedFromThisPage == true)
                {
                    MainWindowFav.isNavigatedFromThisPage = false;
                    MainPageFrame.GoBack();
                    return;
                }

                MainPageTopPanelBackButton.Visibility = Visibility.Collapsed;
                MainPageTopPanelTitle.Text = "Rozkład jazdy";
                MainPageFrame.Navigate(typeof(MainWindowSelect));
            }
            else
                exit = true;
        }

        private static DoubleAnimation animation;
        public static void showInfo(string message)
        {
            if (gui.MainPageInfoGrid.Visibility == Visibility.Visible)
            {
                animation.Duration = TimeSpan.FromSeconds(2);
                gui.MainPageInfoText.Text = message;
                return;
            }

            gui.MainPageInfoGrid.Visibility = Visibility.Visible;

            animation = new DoubleAnimation();
            animation.To = 1;
            animation.From = 0;
            animation.Duration = TimeSpan.FromSeconds(2);
            animation.EasingFunction = new QuadraticEase();
            animation.AutoReverse = true;

            Storyboard sb = new Storyboard();
            sb.Children.Add(animation);

            Storyboard.SetTarget(sb, gui.MainPageInfoGrid);
            Storyboard.SetTargetProperty(sb, "Opacity");

            gui.MainPageInfoText.Text = message;
            gui.MainPageInfoText.Foreground = new SolidColorBrush(Colors.White);
            sb.Begin();

            sb.Completed += (s, e) => gui.MainPageInfoGrid.Visibility = Visibility.Collapsed;
        }

        private void MainPageTopPanelRefreshButton_Click(object sender, RoutedEventArgs e) => MainWindow.refreshList();

        private bool isStopClicked = false;
        private void MainPageStopListStopsList_Click(object sender, ItemClickEventArgs e)
        {
            MainWindowLinesInfo.selected_stop = e.ClickedItem as Przystanek;
            MainPageFrame.Navigate(typeof(MainWindowLinesInfoHours));
            isStopClicked = true;
        }

        private void MainPageTopPanelFavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            bool addToFavourite = false;
            int type = 0;
            string name = "";

            if (MainPage.gui.getFavouriteButtonColor == Colors.LightGray)
                addToFavourite = true;

            if (MainPageFrame.SourcePageType == typeof(MainWindowLinesInfo))
            {
                if (addToFavourite)
                    favourite_stops.Add(new Ulubiony() { type = 0, id = MainWindow.lines.IndexOf(MainWindowLinesList.selected_line), name = MainWindowLinesList.selected_line.name });
                else
                    favourite_stops.Remove(favourite_stops.Where(p => p.type == 0 && p.name == MainWindowLinesList.selected_line.name).First());

                name = MainWindowLinesList.selected_line.name;
            }
            else if (MainPageFrame.SourcePageType == typeof(MainWindowLinesInfoHours))
            {
                if (addToFavourite)
                    favourite_stops.Add(new Ulubiony() { type = 1, id = MainWindowLinesInfo.selected_stop.nid, name = MainWindowLinesInfo.selected_stop.getName() });
                else
                    favourite_stops.Remove(favourite_stops.Where(p => p.type == 1 && p.name == MainWindowLinesInfo.selected_stop.getName()).First());

                type = 1;
                name = MainWindowLinesInfo.selected_stop.getName();
            }
            else if (MainPageFrame.SourcePageType == typeof(MainWindowStopList))
            {
                if (addToFavourite)
                    favourite_stops.Add(new Ulubiony() { type = 1, id = MainWindowStopList.selected_stop.id, name = MainWindowStopList.selected_stop.name });
                else
                    favourite_stops.Remove(favourite_stops.Where(p => p.type == 1 && p.name == MainWindowStopList.selected_stop.name).First());

                type = 1;
                name = MainWindowStopList.selected_stop.name;
            }

            if (addToFavourite)
            {
                MainPage.gui.setFavouriteButtonColor = Colors.Black;

                var infoMessage = string.Format("{0} {1}\"{2}\" zosta{3} dodan{4} do ulubionych", type == 0 ? "Linia" : "Przystanek", type == 0 ? "nr " : "", name, type == 0 ? "ła" : "ł", type == 0 ? "a" : "y");
                showInfo(infoMessage);
            }
            else
            {
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

                var infoMessage = string.Format("{0} {1}\"{2}\" zosta{3} usunięt{4} z ulubionych", type == 0 ? "Linia" : "Przystanek", type == 0 ? "nr " : "", name, type == 0 ? "ła" : "ł", type == 0 ? "a" : "y");
                showInfo(infoMessage);
            }

            object objectToAdd;

            if (addToFavourite)
                objectToAdd = favourite_stops.Last() as Ulubiony;
            else
                objectToAdd = name as string;

            OnAddedFavouriteStop?.Invoke(type, objectToAdd, !addToFavourite);

            if (addToFavourite)
                SQLServices.addToDataBase<Ulubiony>(1, objectToAdd as Ulubiony);
            else
                SQLServices.getConnection(1).CreateCommand("DELETE FROM Ulubiony WHERE name = ?", objectToAdd as string).ExecuteNonQuery();
        }
        public static bool IsInternetConnection()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            return (bool)(connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }

        public static bool isFavourite(object item)
        {
            if (item.GetType() == typeof(NazwaPrzystanku))
            {
                var stop = item as NazwaPrzystanku;

                foreach (var a in favourite_stops)
                    if (a.type == 1 && a.name == stop.name)
                            return true;
            }
            else if (item.GetType() == typeof(Linia))
            {
                var line = item as Linia;

                foreach (var a in favourite_stops) 
                    if (a.type == 0 && a.name == line.name)
                            return true;
            }

            return false;
        }

    }
}
