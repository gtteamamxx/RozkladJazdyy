using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Syndication;

namespace RozkladJazdy.Model
{
    class RSS_Services
    {
        private static string RSS_url = "http://rozklady.kzkgop.pl/RSS/komunikaty/";

        //from githubproject
        public static async Task getRSS(ObservableCollection<Komunikat> list)
        {
            SyndicationClient client = new SyndicationClient();
            SyndicationFeed feed = await client.RetrieveFeedAsync(new Uri(RSS_url, UriKind.Absolute));
            if (feed != null)
                foreach (SyndicationItem item in feed.Items)
                {
                    var @description = item.Summary.Text.Replace(@"•", string.Format("{0}•", Environment.NewLine)).Replace(".", ". ");

                    int lenght = description.Length;

                    int num = (int)(0.5f + (lenght / 3 + 1));
                    int num2 = lenght - num;

                    string @string1 = description.Substring(0, num) + "....";
                    string @string2 = description.Substring(num, num2);

                    list.Add(new Komunikat { title = item.Title.Text, date = item.LastUpdatedTime.ToString(), desc = string1, desc2 = string2, url = item.Id });
                }
        }
    }

    class Komunikat
    {
        public Visibility state = Visibility.Visible;
        public string url { get; set; }
        public string title { get; set; }
        public string @desc { get; set; }
        public string desc2 { get; set; }
        public string actual_text => state == Visibility.Visible? desc : (desc.Replace("....", "") + desc2);
        public string date { get; set; }
    }
}
