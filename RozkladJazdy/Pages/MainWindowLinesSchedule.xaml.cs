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
    public sealed partial class MainWindowLinesSchedule : Page
    {
        private List<Rozklad> list_of_schedules;

        public MainWindowLinesSchedule()
        {
            this.InitializeComponent();

            MainWindowLinesScheduleTitle.Text = MainWindowLinesScheduleTitle.Text + " " + MainWindowLinesList.selected_line.name;

            list_of_schedules = new List<Rozklad>();
            MainWindowLinesList.selected_line.rozklad = new List<Rozklad>();
            MainWindowLinesList.selected_line.rozklad = list_of_schedules = SQLServices.getData<Rozklad>(0, "SELECT * FROM Rozklad WHERE id_linia = ?", MainWindowLinesList.selected_line.id);
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainWindowLinesList.selected_schedule = (sender as ListView).Items.IndexOf(e.ClickedItem as Rozklad);
            MainPage.gui.setViewPage = typeof(MainWindowLinesInfo);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) =>
            MainPage.gui.setPageTitle = "Rozkład jazdy -> Rozklady dla linii ";

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.SourcePageType == typeof(MainWindowLinesInfo))
            {
                if(MainWindowLinesList.selected_line.rozklad.Count() > 1) return;

                MainWindowLinesInfo.selectedScheduleIndex = -1;
                MainWindowLinesList.selected_schedule = new int();
            }
        }
    }
}
