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
                var a = e.NewValue as Przystanek;

                if (a != null)
                    ChangeText(a);
            };

        }

        private void ChangeText(Przystanek przystanek)
        {
            if(przystanek.track_id+1 > MainWindowLinesInfo.selectedRozklad.track.Count())
                return;

            FontWeight bold = FontWeights.Normal;
            Color color = Colors.Navy;
            Color color2 = Colors.Transparent;

            Thickness margin = new Thickness(0.0, 0.0, 0.0, 0.0);
            string name = "";

            if (przystanek.wariant)
            {
                color = Colors.Red;
                name = "-- ";
                margin = new Thickness(10.0, 0.0, 0.0, 0.0);
            }

            if (przystanek.strefowy)
            {
                color = Colors.Yellow;
                color2 = Colors.Gray;

                color2.A = 125;
            }
            if (przystanek.na_zadanie())
                bold = FontWeights.Bold;
            
            if (przystanek.getName() == MainWindowLinesInfo.selectedRozklad.track[przystanek.track_id].name)
            {
                color = Colors.Green;
                bold = FontWeights.Bold;
            }

            MainWindowLinesInfoListView2TextBlock.Text = name + przystanek.getName();
            MainWindowLinesInfoListView2TextBlock.Margin = margin;

            MainWindowLinesInfoListView2TextBlock.Foreground = new SolidColorBrush(color);
            MainWindowLinesInfoListView2Grid.Background = new SolidColorBrush(color2);
            MainWindowLinesInfoListView2TextBlock.FontWeight = bold;
        }
    }
}
