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
        private int favouritecolor = 0;
        public ObservableCollection<Przystanek> przystanki = new ObservableCollection<Przystanek>();
        public Type setPage { get { return MainPageMyFrame.CurrentSourcePageType; } set { MainPageMyFrame.Navigate(value); } }
        public string setTitle { get { return MainPageRelativePanelTextBlock.Text; } set { MainPageRelativePanelTextBlock.Text = value; } }
        public string setFavSubText { get { return FavouriteText.Text; } set { FavouriteText.Text = value; } }

        public Visibility setBackButtonVisibility
        {
            get { return MainPageRelativePanelBackButton.Visibility; }
            set
            {
                if (DetectPlatform() == Platform.WindowsPhone)
                    return;
                MainPageRelativePanelBackButton.Visibility = value;
            }
        }
        public Visibility setRefreshButtonVisibility { get { return MainPageRelativePanelRefreshButton.Visibility; } set { MainPageRelativePanelRefreshButton.Visibility = value; } }
        public Visibility setFavouriteButtonVisibility { get { return FavouriteButton.Visibility; } set { FavouriteButton.Visibility = value; FavouriteText.Visibility = value; } }
        public Visibility setPrzystankiButtonVisibility { get { return Button.Visibility; } set { Button.Visibility = value; } }
        public SplitViewDisplayMode setSplitViewDisplayMode { get { return MainPageSplitView.DisplayMode; } set { MainPageSplitView.DisplayMode = value; } }
        public Color setFavouriteButtonColor
        {
            set
            {
                favouritecolor = value == Colors.LightGray ? 0 : 1;
                FavouriteButton.Foreground = new SolidColorBrush(value);
            }
        }
        public Color getFavouriteButtonColor => favouritecolor == 0 ? Colors.LightGray : Colors.Black;
        public int setIndex { get { return ListViews.SelectedIndex; } set { ListViews.SelectedIndex = value; } }
        public int getItemIndex(Przystanek pr) => ListViews.Items.IndexOf(pr);
        public string setKierunek(string value) => TextBlockKierunek.Text = value;
        public bool PaneOpen { get { return SplitView.IsPaneOpen; } set { SplitView.IsPaneOpen = value; } }
        public void setPrzystanki(List<Przystanek> pr)
        {
            przystanki.Clear();
            //MainWindowLinesInfoHours.temphour = new int[] { 0, 0 };
            //MainWindowLinesInfoHours.lista_test.Clear();
           // MainWindowLinesInfoHours.lista_test2.Clear();
            foreach (Przystanek p in pr)
                przystanki.Add(p);
        }
        public void clearPrzystanki() => przystanki.Clear();
        public bool isPrzystanki() { return przystanki.Count > 0 ? true : false; }
        public int PrzystankiTrasa { get; set; }
        private static DoubleAnimation animation;
        public static bool isAdmin = false;
        //public static List<Przystanek> getPrzystanki => gui.przystanki.ToList();
        public static ObservableCollection<Ulubiony> ulubione = new ObservableCollection<Ulubiony>();
        
        public static eventAddedfav OnAddedFav;
        public static eventRefresh OnRefreshRozklady;

        public delegate void eventRefresh();
        public delegate void eventAddedfav(int type = -1, object obiekt = null, bool delete = false);


        //from so
        public Platform DetectPlatform()
        {
            if (ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
                return Platform.WindowsPhone;
            else
                return Platform.Windows;
        }

        public static MainPage gui;
        public MainPage()
        {
            this.InitializeComponent();
            gui = this;

            MainPageListView.SelectedIndex = 0;

            MainPageMyFrame.Navigate(typeof(MainWindow));
            MainPageRelativePanelTextBlock.Text = "Rozkład jazdy";

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

            var view = SystemNavigationManager.GetForCurrentView();
            view.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            view.BackRequested += (s, e) =>
            {
                bool a = false;
                goBack(ref a);

                if (!a)
                    e.Handled = true;
            };

            MainPageMyFrame.Navigated += (sender, e) =>
            {
                if (e.SourcePageType == typeof(MainWindowLinesInfoHours))
                    Button.Visibility = Visibility.Visible;
            };

            ListViews.SelectionChanged += (s, f) =>
            {
                if (ListViews.SelectedItem != null)
                    ListViews.ScrollIntoView(ListViews.SelectedItem);
            };

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(360, 500));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
            => SplitView.IsPaneOpen = !SplitView.IsPaneOpen;

        private void MainPageRelativePanelButton_Click(object sender, RoutedEventArgs e)
            => MainPageSplitView.IsPaneOpen = !MainPageSplitView.IsPaneOpen;

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var textblock = ((e.ClickedItem as StackPanel).Children[1] as TextBlock);

            if (textblock.Text.Contains("Roz") && MainWindow.isLoaded == false && MainWindow.refresh == false) // rozklad jazdy
            {
                MainPageMyFrame.Navigate(typeof(MainWindow));
                MainPageRelativePanelTextBlock.Text = "Rozkład jazdy";
            }
            else if (textblock.Text.Contains("Roz") && MainWindow.isLoaded == true && MainWindow.refresh == false)
            {
                MainPageMyFrame.Navigate(typeof(MainWindowSelect));
                MainPageRelativePanelTextBlock.Text = "Rozkład jazdy";
            }
            else if (textblock.Text.Contains("unika") && MainWindow.isLoaded == true && MainWindow.refresh == false)
            {
                MainPageMyFrame.Navigate(typeof(Comunicates));
                MainPageRelativePanelTextBlock.Text = "Komunikaty BETA";
            }
        }

        private void MainPageRelativePanelBackButton_Click(object sender, RoutedEventArgs e)
        {
            bool a = new bool();
            goBack(ref a);

            // jesli exit, to spytaj czy na pewno
        }

        private void goBack(ref bool exit)
        {
            if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesInfoHours))
            {
                MainWindowLinesInfoHours.was = null;
                MainWindowLinesInfoHours.hour = 0;
                //MainWindowLinesInfoHours.selectedHour = "";

                Button.Visibility = Visibility.Collapsed;

                if (MainWindowStopList.navigated_from == true)
                {
                    if (!clicked)
                        MainPageMyFrame.GoBack();
                    else
                    {
                        MainPageMyFrame.Navigate(typeof(MainWindowStopList));
                        clicked = false;
                    }
                }
                else
                    MainPageMyFrame.Navigate(typeof(MainWindowLinesInfo));

            }
            else if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesInfo))
            {
                przystanki.Clear();
                gui.PrzystankiTrasa = -1;

                if (MainWindowLinesInfoHours.navigated_from == true)
                {
                    MainWindowLinesInfoHours.navigated_from = false;
                    MainPageMyFrame.GoBack();
                    return;
                }

                if (MainWindowFav.navigated_from == true)
                {
                    MainWindowFav.navigated_from = false;
                    MainPageMyFrame.GoBack();
                    return;
                }

                if (MainWindowStopList.navigated_from == true)
                {
                    MainPage.gui.setRefreshButtonVisibility = Visibility.Visible;
                    MainPageMyFrame.GoBack();
                    return;
                }

                if (MainWindowLinesList.selectedLine.rozklad.Count == 1)
                        MainPageMyFrame.Navigate(typeof(MainWindowLinesList));
                else
                {
                    MainPageRelativePanelRefreshButton.Visibility = Visibility.Collapsed;
                    MainPageMyFrame.Navigate(typeof(MainWindowLinesRozkladDzien));
                }
            }
            else if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesRozkladDzien))
            {
                if (MainWindowFav.navigated_from == true)
                {
                    MainWindowFav.navigated_from = false;
                    MainPageMyFrame.GoBack();
                    return;
                }

                MainPageRelativePanelRefreshButton.Visibility = Visibility.Visible;

                if (MainWindowStopList.navigated_from == true)
                    MainPageMyFrame.GoBack();
                else
                    MainPageMyFrame.Navigate(typeof(MainWindowLinesList));
            }
            else if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesList)
                || MainPageMyFrame.SourcePageType == typeof(MainWindowStopList)
                || MainPageMyFrame.SourcePageType == typeof(MainWindowFav))
            {

                if (MainWindowLinesInfoHours.navigated_from == true)
                {
                    MainWindowLinesInfoHours.navigated_from = false;
                    MainPageMyFrame.GoBack();
                    return;
                }

                if (MainWindowFav.navigated_from == true)
                {
                    MainWindowFav.navigated_from = false;
                    MainPageMyFrame.GoBack();
                    return;
                }

                MainPageRelativePanelBackButton.Visibility = Visibility.Collapsed;
                MainPageRelativePanelTextBlock.Text = "Rozkład jazdy";
                MainPageMyFrame.Navigate(typeof(MainWindowSelect));
            }
            else
                exit = true;
        }
        public static void showInfo(string message)
        {
            if (gui.InfoGrid.Visibility == Visibility.Visible)
            {
                animation.Duration = TimeSpan.FromSeconds(2);
                gui.InfoText.Text = message;
                return;
            }

            gui.InfoGrid.Visibility = Visibility.Visible;

            animation = new DoubleAnimation();
            animation.To = 1;
            animation.From = 0;
            animation.Duration = TimeSpan.FromSeconds(2);
            animation.EasingFunction = new QuadraticEase();
            animation.AutoReverse = true;

            Storyboard sb = new Storyboard();
            sb.Children.Add(animation);

            Storyboard.SetTarget(sb, gui.InfoGrid);
            Storyboard.SetTargetProperty(sb, "Opacity");

            gui.InfoText.Text = message;
            gui.InfoText.Foreground = new SolidColorBrush(Colors.White);
            sb.Begin();

            sb.Completed += (s, e) => gui.InfoGrid.Visibility = Visibility.Collapsed;

        }

        private void MainPageRelativePanelRefreshButton_Click(object sender, RoutedEventArgs e) => MainWindow.refreshList();

        private bool clicked = false;
        private void ListViews_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainWindowLinesInfo.selectedPrzystanek = e.ClickedItem as Przystanek;
            MainPageMyFrame.Navigate(typeof(MainWindowLinesInfoHours));
            clicked = true;
        }

        private void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            bool added = false;
            int type = 0;
            string name = "";

            if (MainPage.gui.getFavouriteButtonColor == Colors.LightGray)
                added = true;

            if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesInfo))
            {
                if (added)
                    ulubione.Add(new Ulubiony() { type = 0, id = MainWindow.Lines.IndexOf(MainWindowLinesList.selectedLine), name = MainWindowLinesList.selectedLine.name });
                else
                    ulubione.Remove(ulubione.Where(p => p.type == 0 && p.name == MainWindowLinesList.selectedLine.name).ToList().First());

                name = MainWindowLinesList.selectedLine.name;
            }
            else if (MainPageMyFrame.SourcePageType == typeof(MainWindowLinesInfoHours))
            {
                if (added)
                    ulubione.Add(new Ulubiony() { type = 1, id = MainWindowLinesInfo.selectedPrzystanek.nid, name = MainWindowLinesInfo.selectedPrzystanek.getName() });
                else
                    ulubione.Remove(ulubione.Where(p => p.type == 1 && p.name == MainWindowLinesInfo.selectedPrzystanek.getName()).ToList().First());

                type = 1;
                name = MainWindowLinesInfo.selectedPrzystanek.getName();
            }
            else if (MainPageMyFrame.SourcePageType == typeof(MainWindowStopList))
            {
                if (added)
                    ulubione.Add(new Ulubiony() { type = 1, id = MainWindowStopList.selectedPrzystanek.id, name = MainWindowStopList.selectedPrzystanek.name });
                else
                    ulubione.Remove(ulubione.Where(p => p.type == 1 && p.name == MainWindowStopList.selectedPrzystanek.name).ToList().First());

                type = 1;
                name = MainWindowStopList.selectedPrzystanek.name;
            }

            if (added)
            {
                MainPage.gui.setFavouriteButtonColor = Colors.Black;

                var txt = string.Format("{0} {1}\"{2}\" zosta{3} dodan{4} do ulubionych", type == 0 ? "Linia" : "Przystanek", type == 0 ? "nr " : "", name, type == 0 ? "ła" : "ł", type == 0 ? "a" : "y");
                showInfo(txt);
            }
            else
            {
                MainPage.gui.setFavouriteButtonColor = Colors.LightGray;

                var txt = string.Format("{0} {1}\"{2}\" zosta{3} usunięt{4} z ulubionych", type == 0 ? "Linia" : "Przystanek", type == 0 ? "nr " : "", name, type == 0 ? "ła" : "ł", type == 0 ? "a" : "y");
                showInfo(txt);
            }

            object b;

            if (added)
                b = ulubione.Last();
            else
                b = name;

            OnAddedFav?.Invoke(type, b, !added);

            if (added)
                SQLServices.addToDataBase<Ulubiony>(1, b as Ulubiony);
            else
            {
                var cmd = SQLServices.getConnection(1).CreateCommand("DELETE FROM Ulubiony WHERE name = ?", b as string);
                cmd.ExecuteNonQuery();
            }
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
                var przystanek = item as NazwaPrzystanku;

                foreach (var a in ulubione)
                    if (a.type == 1)
                        if (a.name == przystanek.name)
                            return true;
            }
            else if (item.GetType() == typeof(Linia))
            {
                var linia = item as Linia;

                foreach (var a in ulubione)
                    if (a.type == 0)
                        if (a.name == linia.name)
                            return true;
            }

            return false;
        }
    }
}
