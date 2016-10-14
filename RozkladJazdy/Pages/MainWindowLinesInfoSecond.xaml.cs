using RozkladJazdy.Model;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RozkladJazdy.Pages
{
    public sealed partial class MainWindowLinesInfoSecond : UserControl
    {
        public MainWindowLinesInfoSecond()
        {
            this.InitializeComponent();
            this.DataContextChanged += (sender, e) =>
            {
                var stop = e.NewValue as Przystanek;

                if (stop != null) ChangeText(stop);
            };

        }

        private void ChangeText(Przystanek stop)
        {
            if(stop.track_id+1 > MainWindowLinesInfo.selected_schedule.track.Count())
                return;

            FontWeight bold = FontWeights.Normal;
            Color color = Colors.Navy;
            Color color2 = Colors.Transparent;

            Thickness margin = new Thickness(0.0, 0.0, 0.0, 0.0);
            string name = "";

            if (stop.wariant)
            {
                color = Colors.Red;
                name = "-- ";
                margin = new Thickness(10.0, 0.0, 0.0, 0.0);
            }

            if (stop.strefowy)
            {
                color = Colors.Yellow;
                color2 = Colors.Gray;

                color2.A = 125;
            }
            if (stop.na_zadanie())
                bold = FontWeights.Bold;
            
            if (stop.getName() == MainWindowLinesInfo.selected_schedule.track[stop.track_id].name)
            {
                color = Colors.Green;
                bold = FontWeights.Bold;
            }

            MainWindowLinesInfoListView2StopName.Text = name + stop.getName();
            MainWindowLinesInfoListView2StopName.Margin = margin;

            MainWindowLinesInfoListView2StopName.Foreground = new SolidColorBrush(color);
            MainWindowLinesInfoListView2Grid.Background = new SolidColorBrush(color2);
            MainWindowLinesInfoListView2StopName.FontWeight = bold;
        }
    }
}
