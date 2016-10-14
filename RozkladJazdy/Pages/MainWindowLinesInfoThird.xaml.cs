using RozkladJazdy.Model;
using RozkladJazdy.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowLinesInfoThird : Page
    {
        private Przystanek temp_stop = new Przystanek();
        public MainWindowLinesInfoThird()
        {
            this.InitializeComponent();

            this.DataContextChanged += (sender, e) =>
            {
                var stop = e.NewValue as Przystanek;
                if (stop != null)
                {
                    // if (a.getName().Contains("ienkiewic"))
                    //    ; #debug

                    ChangeText(stop);
                }
            };

        }

        private void ChangeText(Przystanek stop)
        {
            FontWeight bold = FontWeights.Normal;
            Color color = Colors.Navy;
            Thickness margin = new Thickness(0.0, 0.0, 0.0, 0.0);
            string name = "";

            if (stop.wariant)
            {
                color = Colors.Red;
                name = "-- ";
                margin = new Thickness(10.0, 0.0, 0.0, 0.0);
            }

            if (stop.strefowy)
                color = Colors.Yellow;

            if (stop.na_zadanie())
                bold = FontWeights.Bold;

            var track = MainWindowLinesList.selectedLine.rozklad[stop.rozkladzien_id].track;
            if (track == null)
            {
                track = SQLServices.getData<Trasa>(0, "SELECT * FROM Trasa WHERE (id_linia = ? AND id_rozklad = ?) LIMIT 2", MainWindowLinesList.selectedLine.id, stop.rozkladzien_id);

                MainWindowLinesList.selectedLine.rozklad[stop.rozkladzien_id].track = new List<Trasa>();
                MainWindowLinesList.selectedLine.rozklad[stop.rozkladzien_id].track = track;

                for(int i = 0; i < track.Count(); i++)
                {
                    track[i].stops = new List<Przystanek>();
                    MainWindowLinesList.selectedLine.rozklad[stop.rozkladzien_id].track[i].stops = new List<Model.Przystanek>();

                    track[i].stops = MainWindowLinesList.selectedLine.rozklad[stop.rozkladzien_id].track[i].stops = SQLServices.getData<Przystanek>(0, "SELECT * FROM Przystanek WHERE id_trasa = ?", track[i].id);
                }

            }
            if (stop.getName() == track[stop.track_id].name)
            {
                color = Colors.Green;
                bold = FontWeights.Bold;
            }

            MainWindowLinesInfoListView1StopName.Text = name + stop.getName();
            MainWindowLinesInfoListView1StopName.Margin = margin;

            MainWindowLinesInfoListView1StopName.Foreground = new SolidColorBrush(color);

            MainWindowLinesInfoListView1StopName.FontWeight = bold;

            if (stop == MainWindowLinesInfo.selected_stop)
                MainPage.gui.setStopListActualIndex = MainPage.gui.getStopListActualIndex(stop);

            temp_stop = stop;

            /*if (MainWindowLinesInfoHours.lista_test2.IndexOf(przystanek) == -1)
            {
                MainWindowLinesInfoHours.lista_test.Add(this);
                MainWindowLinesInfoHours.lista_test2.Add(przystanek);
            }*/
        }

        /*public void test()
        {
            hours.Text = "";
            var przystanek = tempprzystanek == this.DataContext as Przystanek ? tempprzystanek : this.DataContext as Przystanek;

            if (przystanek == null || (przystanek != null && przystanek.wariant) ||
                (MainWindowLinesInfoHours.temphour[0] == 0 && MainWindowLinesInfoHours.temphour[1] == 0))
            {
                return;
            }

            var n1 = MainWindowLinesInfoHours.temphour[0];
            var n2 = MainWindowLinesInfoHours.temphour[1];

            var id_godziny = MainWindowLinesInfoHours.temphour3[0];
            var id_klikniętej_godziny = MainWindowLinesInfoHours.temphour3[1];

            var godziny = przystanek.godziny != null ? przystanek.godziny.Where(p => p.nid == id_godziny).ToList()
            : SQLServices.getData<Godzina>(0, "SELECT godziny_full FROM Godzina WHERE id_przystanek = ? AND nid = ?", przystanek.id, id_godziny);

            if (godziny.Count() == 0)
                return;

            var lista_godziny = godziny.First().godziny_full.Split('#').ToList();

            foreach (string godzina in lista_godziny)
            {
                if (godzina == "") continue;

                var temp = godzina.Split(':');
                var n11 = int.Parse(temp[0]);

                int n = -1, n22 = -1;

                if (!int.TryParse(temp[1], out n))
                {
                    temp[1] = temp[1].Replace(temp[1].Last(), ' ').ToString().Trim();
                    n22 = int.Parse(temp[1]);
                }

                n22 = n22 == -1 ? n : n22;

                var temp2 = MainWindowLinesInfoHours.temphour2;

                if (n11 < temp2[0] || (n11 == temp2[0] && n22 < temp2[1])
                    || (temp2[0] == -1 && temp2[1] == -1 && (n11 < n1 || (n11 == n1 && n22 < n2))))
                    continue;


                MainWindowLinesInfoHours.temphour2 = new int[] { n11, n22 };

                hours.Text = godzina;
                break;
            }
        }*/
    }
}
