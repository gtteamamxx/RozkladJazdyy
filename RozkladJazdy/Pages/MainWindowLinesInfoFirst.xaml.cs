using RozkladJazdy.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
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
    public sealed partial class MainWindowLinesInfoFirst : UserControl
    {
        public Przystanek getStop { get { return _stop; } }
        private Przystanek _stop = new Przystanek();
        public MainWindowLinesInfoFirst()
        {
            this.InitializeComponent();

            this.DataContextChanged += (sender, e) =>
            {
                if (MainWindowLinesInfo.usercontrol_mainwindowlinesinfofirst_list.IndexOf(this) == -1)
                    MainWindowLinesInfo.usercontrol_mainwindowlinesinfofirst_list.Add(this);

                var a = e.NewValue as Przystanek;
                if (a != null) ChangeText(a);
            };
        }

        public void setWidth(int val = 0, double width = 0.0)
        {
            MainWindowLinesInfoListView1Grid.Width = width;
            MainWindowLinesInfoListView1StopName.Width = (width / 2);
            var var = (width / 2) - (width / 2 / 2);
            asd.Margin = new Thickness(val == 1 ? var < 150 ? 0.0 : var : 0.0 ,0.0,0.0,0.0);     
        }

        private void ChangeText(Przystanek przystanek)
        {
            _stop = przystanek;

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

            if (przystanek.getName() == MainWindowLinesInfo.selected_schedule.track[przystanek.track_id].name)
            {
                color = Colors.Green;
                bold = FontWeights.Bold;
            }

            MainWindowLinesInfoListView1StopName.Text = name + przystanek.getName();
            MainWindowLinesInfoListView1StopName.Margin = margin;

            MainWindowLinesInfoListView1StopName.Foreground = new SolidColorBrush(color);
            MainWindowLinesInfoListView1Grid.Background = new SolidColorBrush(color2);
            MainWindowLinesInfoListView1StopName.FontWeight = bold;

            setWidth(MainWindowLinesInfo.getSecondDestVisibility == Visibility.Collapsed ? 1 : 0, MainWindowLinesInfo.getPageWidth);
        }
    }
}
