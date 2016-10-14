using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace RozkladJazdy.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindowLinesInfoHoursHours : Page
    {
        public MainWindowLinesInfoHoursHours()
        {
            this.InitializeComponent();

            this.DataContextChanged += (sender, e) => { if (e.NewValue != null) changeText(e.NewValue as string); };
        }

        public void changeText(string text)
        {
            MainWindowHoursText.Text = text;

            var txt = text.Split(':');

            var n1 = txt[0];
            var n2 = txt[1];
            var letter = n2.Last();

            var time = DateTime.Now;

            if (!char.IsDigit(letter))
            {
                MainWindowHoursText.Foreground = new SolidColorBrush(Colors.Brown);
                n2 = n2.Replace(n2.Last(), ' ').Trim();
            }

            if (int.Parse(n1) < MainWindowLinesInfoHours.hour || 
                (int.Parse(n1) == MainWindowLinesInfoHours.hour 
                && int.Parse(n2) < MainWindowLinesInfoHours.minute))
                MainWindowLinesInfoHours.was = null;

            if (MainWindowLinesInfoHours.was == null)
            {
                int num = -1;

                if (int.TryParse(n2, out num) == false)
                    return;

                if (isClosest(int.Parse(n1), num, time.Hour, time.Minute) == true)
                {
                    MainWindowLinesInfoHours.hour = int.Parse(n1);
                    MainWindowLinesInfoHours.minute = int.Parse(n2);
                    MainWindowLinesInfoHours.was = false;
                    MainWindowHoursStackPanel.Background = new SolidColorBrush(Colors.Red);
                    MainWindowHoursText.Foreground = new SolidColorBrush(Colors.Yellow);
                }
            }
            else if(MainWindowLinesInfoHours.was == false)
            {
                MainWindowHoursStackPanel.Background = new SolidColorBrush(Colors.Yellow);
                MainWindowLinesInfoHours.was = true;
            }
            
        }
        public bool isClosest(int hour, int minutes, int achour, int acminutes)
          =>  (hour == achour) ? (minutes < acminutes) ? false : true : (hour > achour) ? true : false;
    }
}
