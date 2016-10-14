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
        private ObservableCollection<Komunikat> communicates;
        public Comunicates()
        {
            this.InitializeComponent();

            communicates = new ObservableCollection<Komunikat>();

            this.Loaded += async (s, e) =>
            {
                if (!MainPage.IsInternetConnection())
                {
                    if (communicates.Count() == 0)
                    {
                        CommunicatesRefreshButton.Visibility = Visibility.Visible;
                        CommunicatesListOfCommunicates.Visibility = Visibility.Collapsed;
                        CommunicatesRefreshButton.Click += async (se, fe) =>
                        {
                            if (MainPage.IsInternetConnection())
                                await downloadRSS();
                        };
                        MainPage.showInfo("Wystąpił problem z połączeniem. Sprawdź, czy masz dostęp do sieci i odśwież stronę.");
                        return;
                    }
                }
                else
                    await downloadRSS();
            };
        }
        private async Task downloadRSS()
        {
            await RSS_Services.getRSS(communicates);
            CommunicatesStatusProgressRing.Visibility = Visibility.Collapsed;

            CommunicatesRefreshButton.Visibility = Visibility.Collapsed;
            CommunicatesListOfCommunicates.Visibility = Visibility.Visible;
        }
        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var communicate = ((e.OriginalSource as HyperlinkButton).Content as TextBlock).DataContext as Komunikat;

            var communicate_stackpanel = (e.OriginalSource as HyperlinkButton).Parent as StackPanel;
            var communicate_textblock = (communicate_stackpanel.Children[1] as TextBlock);

            communicate.state = communicate.state == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            communicate_textblock.Text = communicate.actual_text;

            var communicate_text = ((sender as HyperlinkButton).Content as TextBlock).Text;

            ((sender as HyperlinkButton).Content as TextBlock).Text = (communicate_text == "Czytaj więcej...") ? "Czytaj mniej..." : "Czytaj więcej...";
        }

    }
}
