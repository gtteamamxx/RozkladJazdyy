using System;
using System.Collections.Generic;
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
using RozkladJazdy.Model;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Comunicates : Page
    {
        private ObservableCollection<Komunikat> komunikaty;
        public Comunicates()
        {
            this.InitializeComponent();

            komunikaty = new ObservableCollection<Komunikat>();

            this.Loaded += async (s, e) =>
            {
                if (!MainPage.IsInternetConnection())
                {
                    if (komunikaty.Count() == 0)
                    {
                        RefreshButton.Visibility = Visibility.Visible;
                        ListView.Visibility = Visibility.Collapsed;
                        RefreshButton.Click += async (se, fe) =>
                        {
                            if (MainPage.IsInternetConnection())
                                await DownloadRss();
                        };
                        MainPage.showInfo("Wystąpił problem z połączeniem. Sprawdź, czy masz dostęp do sieci i odśwież stronę.");
                        return;
                    }
                }
                else
                    await DownloadRss();
            };
        }
        private async Task DownloadRss()
        {
            await RSS_Services.getRSS(komunikaty);
            StatusProgressRing.Visibility = Visibility.Collapsed;

            RefreshButton.Visibility = Visibility.Collapsed;
            ListView.Visibility = Visibility.Visible;
        }
        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var komunikat = ((e.OriginalSource as HyperlinkButton).Content as TextBlock).DataContext as Komunikat;

            var stackpanel = (e.OriginalSource as HyperlinkButton).Parent as StackPanel;
            var textblock = (stackpanel.Children[1] as TextBlock);

            komunikat.state = komunikat.state == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            textblock.Text = komunikat.actual_text;

            var txt = ((sender as HyperlinkButton).Content as TextBlock).Text;

            ((sender as HyperlinkButton).Content as TextBlock).Text = txt == "Czytaj więcej..." ? "Czytaj mniej..." : "Czytaj więcej...";
        }

    }
}
