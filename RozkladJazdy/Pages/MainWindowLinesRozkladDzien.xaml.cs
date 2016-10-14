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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowLinesRozkladDzien : Page
    {
        private List<Rozklad> lista;

        public MainWindowLinesRozkladDzien()
        {
            this.InitializeComponent();

            MainWindowLinesRozkladDzienTextBlock1.Text = MainWindowLinesRozkladDzienTextBlock1.Text + " " + MainWindowLinesList.selectedLine.name;

            lista = new List<Rozklad>();
            MainWindowLinesList.selectedLine.rozklad = new List<Rozklad>();
            MainWindowLinesList.selectedLine.rozklad = lista = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", MainWindowLinesList.selectedLine.id);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainWindowLinesList.selectedRozklad = (sender as ListView).Items.IndexOf(e.ClickedItem as Rozklad);
            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) =>
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Rozklady dla linii ";

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {

            if (e.SourcePageType == typeof(MainWindowLinesInfo))
            {
                if(MainWindowLinesList.selectedLine.rozklad.Count() > 1)
                {
                    return;
                }
                MainWindowLinesInfo.selectedRozkladIndex = -1;
                MainWindowLinesList.selectedRozklad = new int();
            }
        }
    }
}
