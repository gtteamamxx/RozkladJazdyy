using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using RozkladJazdy.Model.RozkladJazy.Modelnet;

namespace RozkladJazdy.Model
{
    class HTMLServices
    {
        public static List<Linia> lines_list;
        public static List<NazwaPrzystanku> stops_name = new List<NazwaPrzystanku>();
        public static List<NazwaGodziny> hours_name = new List<NazwaGodziny>();
        public static List<Literka> letters_info = new List<Literka>();

        public delegate void eventGetLinesBackgroundFinish(List<Linia> a, bool update = false);
        public delegate void eventGetLinesDetailProgressChange(int percent, object state, bool update = false);

        public static event eventGetLinesBackgroundFinish OnGetLinesBackgroundFinish;
        public static event eventGetLinesDetailProgressChange OnGetLinesDetailProgressChange;

        public HTMLServices()
        {
            lines_list = new List<Linia>();
            MainPage.OnTimeTableRefesh += () => lines_list.Clear();
        }

        public static void getLinesInfo(bool update = false)
        {
            BackgroundWorker worker = new BackgroundWorker();

            Literka.aid = 0;
            Godzina.aid = 0;
            Trasa.aid = 0;
            Rozklad.aid = 0;
            NazwaGodziny.aid = 0;
            NazwaPrzystanku.aid = 0;
            
            worker.DoWork += (senders, es) =>
                es.Result = ParserMain(AsyncHelpers.RunSync(() =>
                    GetHTML("http://rozklady.kzkgop.pl/index.php?co=rozklady")));

            worker.RunWorkerCompleted += (senders, es) =>
            {
                lines_list = update == false ? (es.Result as List<Linia>) : (es.Result as List<Linia>).Where(p => (p.pfm & 32) == 32).ToList();
                getLinesDetail(lines_list, update);
            };

            stops_name = new List<Model.NazwaPrzystanku>();
            hours_name = new List<Model.NazwaGodziny>();
            letters_info = new List<Model.Literka>();

            worker.RunWorkerAsync();
        }
        public static void getLinesDetail(List<Linia> buslist, bool update = false)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            worker.DoWork += (sender, e) =>
            {
                for (int i = 0; i < buslist.Count; i++)
                {
                    int nums = (int)(0.5f + ((100f * i) / buslist.Count));
                    var state = buslist[i].name;

                    string url = string.Format("{0}{1}", "http://rozklady.kzkgop.pl/", buslist[i].url);

                    lines_list[i].rozklad = ParserLine(url, AsyncHelpers.RunSync(() => GetHTML(url)), i).ToList();

                    worker.ReportProgress(nums, state);
                }
            };

            worker.ProgressChanged += (sender, e) => OnGetLinesDetailProgressChange(e.ProgressPercentage, e.UserState, update);

            worker.RunWorkerCompleted += (sender, e) => OnGetLinesBackgroundFinish?.Invoke(lines_list, update);
            worker.RunWorkerAsync();
        }
        private static int num = 0;
        public static List<Rozklad> ParserLine(string url, string source, int line_id)
        {
            try
            {
                var val = new List<Rozklad>();

                var parser = new HtmlParser();
                var document = parser.Parse(source);


                var list = document.QuerySelectorAll("td").Where(p => p.GetAttribute("class") != null && p.GetAttribute("class").Contains("kier")).ToList();

                // jesli nie ma kierunkow, chodiaz jednego, to sprawdz, czy nie div_tabelki tras
                // jest to lista rozkladow na specjlanie dnie.

                if (list == null || list.Count() == 0)
                {
                    list = document.QuerySelectorAll("div").Where(p => p.GetAttribute("id") != null && p.GetAttribute("id").Contains("div_tabelki_tra")).ToList();

                    if (list.Count() == 0)
                    {
                        list = document.QuerySelectorAll("div").Where(p => p.GetAttribute("id") != null && p.GetAttribute("id").Contains("content") && p.FirstElementChild.LocalName == "h3" && p.FirstElementChild.TextContent.Contains("zawie")).ToList();

                        val.Add(new Rozklad() { text = "linia zawieszona" });
                        return val;
                    }

                    var l2 = list[0].Children[1].QuerySelectorAll("li a").ToList();

                    for (int i = 0; i < l2.Count(); i++)
                        val.Add(new Rozklad() { text = l2[i].TextContent.Trim(), actual = l2[i].TextContent.Trim().Contains("obecnie"), url = string.Format("{0}{1}", "http://rozklady.kzkgop.pl/", @l2[i].GetAttribute("href")) });
                }

                if (val.Count() == 0)
                    val.Add(new Rozklad() { text = "obecnie obowiązujący", url = url, actual = true });

                for (int i = 0; i < val.Count(); i++)
                {
                    val[i].track = new List<Trasa>();

                    source = AsyncHelpers.RunSync(() => GetHTML(val[i].url));

                    document = parser.Parse(source);

                    var l2 = document.QuerySelectorAll("div").Where(p => p.GetAttribute("id") != null && (
                        p.Attributes["id"].Value.Contains("lewo") ||
                        p.Attributes["id"].Value.Contains("srodek") ||
                        p.Attributes["id"].Value.Contains("prawo"))).
                        ToList();

                    for (int s = 0; s < l2.Count(); s++)
                    {
                        string firstlink = l2[s].QuerySelectorAll("tr td a").ToList()[0].GetAttribute("href");

                        source = AsyncHelpers.RunSync(() => GetHTML(string.Format("http://rozklady.kzkgop.pl/{0}", firstlink)));

                        document = parser.Parse(source);

                        var l5 = document.QuerySelectorAll("table").Where(
                                p => p.GetAttribute("id") != null &&
                                    p.GetAttribute("id").Contains("div_trasy_table")).
                                        ToList();

                        var l3 = l5[0].QuerySelectorAll("tr").ToList();

                        Trasa track = new Trasa();
                        track.stops = new List<Przystanek>();

                        track.id_linia = line_id;
                        track.id_rozklad = i;

                        track.name = l3.Where(p => p.GetAttribute("class") == "tr_kierunek").ToList()[0].FirstElementChild.FirstElementChild.TextContent;

                        l3 = l3.Where(p => (p.GetAttribute("class").Contains("zwyk") || p.GetAttribute("class").Contains("stre") || p.GetAttribute("class").Contains("wyj"))).ToList();

                        for (int k = 0; k < l3.Count(); k++)
                        {
                            var d = l3[k].LastElementChild;

                            Przystanek stop = new Przystanek();

                            stop.nid = -1;

                            for (int v = 0; v < stops_name.Count(); v++)
                            {
                                if (stops_name[v].name == d.LastElementChild.TextContent)
                                {
                                    stop.nid = v;
                                    break;
                                }
                            }

                            if (stop.nid == -1)
                            {
                                stops_name.Add(new NazwaPrzystanku() { name = d.LastElementChild.TextContent });
                                stop.nid = stops_name.IndexOf(stops_name.Last());
                            }

                            stop.track_id = s;
                            stop.rozkladzien_id = i;

                            stop.id_trasa = track.id;
                            stop.id_rozklad = val[i].id;

                            stop.godziny = new List<Godzina>();

                            stop.url = @d.LastElementChild.GetAttribute("href");
                            stop.id = num++;

                            if (track.name != d.LastElementChild.TextContent)
                            {
                                source = AsyncHelpers.RunSync(() => GetHTML(string.Format("http://rozklady.kzkgop.pl/{0}", stop.url)));
                                document = parser.Parse(source);
                            }

                            var l4 = document.QuerySelectorAll("table").Where(
                                p => p.GetAttribute("id") != null && p.GetAttribute("id") == "tabliczka_przystankowo").ToList();

                            bool state = false;

                            if (l4.Count() > 0)
                            {
                                var l6 = l4[0].QuerySelectorAll("tr");

                                var hour = new Godzina();
                                hour.nid = -1;
                                hour.godziny_full = null;
                                int j = 0;

                                for (int b = 0; b < l6.Count(); b++)
                                {
                                    if (b % 2 == 0)
                                    {
                                        if (hour.godziny_full != null && b != 0 && hour.nid != -1)
                                        {
                                            j++;
                                            hour.id_przystanek = stop.id;
                                            stop.godziny.Add(hour);
                                            hour = new Godzina();
                                        }
                                        hour.godziny_full = "";

                                        bool set = false;

                                        for (int v = 0; v < hours_name.Count(); v++)
                                        {
                                            if (hours_name[v].name == l6[b].FirstElementChild.TextContent.Trim())
                                            {
                                                hour.nid = v;
                                                set = true;
                                                break;
                                            }
                                        }

                                        if (!set)
                                        {
                                            hours_name.Add(new NazwaGodziny() { name = l6[b].FirstElementChild.TextContent.Trim() });
                                            hour.nid = hours_name.IndexOf(hours_name.Last());
                                        }
                                        hour.id_przystanek = stop.id;

                                        continue;
                                    }

                                    var l7 = l6[b].QuerySelectorAll("span").Where(p => p.GetAttribute("id") != null && p.GetAttribute("id").Contains("blok_godzina")).ToList();

                                    for (int g = 0; g < l7.Count(); g++)
                                    {
                                        string n1, n2, letter = "";
                                        var l = l7[g];

                                        n1 = l.FirstElementChild.TextContent;

                                        if (int.Parse(n1) < 10)
                                        {
                                            var temp = n1;
                                            n1 = "0" + temp;
                                        }

                                        var l8 = l.QuerySelectorAll("a").ToList();

                                        for (int m = 0; m < l8.Count(); m++)
                                        {
                                            n2 = l8[m].FirstElementChild.TextContent;

                                            //jesli jest literka
                                            if (l8[m].FirstElementChild.FirstElementChild != null)
                                            {
                                                letter = l8[m].FirstElementChild.FirstElementChild.TextContent;

                                                n2 = n2.Replace(letter[0], ' ').Trim();
                                                state = true;
                                            }

                                            hour.godziny_full += string.Format("{0}:{1}{2}#", n1, n2, letter.ToLower());
                                        }
                                    }
                                }

                                stop.godziny.Add(hour);

                                // jeśli jest literka, dodaj do bazy danych
                                if (state)
                                {
                                    l4 = document.QuerySelectorAll("table").Where(p => p.ClassName != null && p.ClassName.Contains("legenda_literki")).ToList();

                                    if (l4.Count() > 0)
                                    {
                                        var l7 = l4[0].QuerySelectorAll("tr");

                                        for (int m = 0; m < l7.Count(); m++)
                                        {
                                            Char @c = @l7[m].TextContent[0];
                                            var @a = l7[m].TextContent.Remove(0, 1).Insert(0, string.Format("{0} - ", c));

                                            Literka letter = new Literka();

                                            letter.id_przystanku = stop.id;
                                            letter.info = @a;
                                            letters_info.Add(letter);
                                        }
                                    }
                                }

                            }

                            // stefa + wartiant
                            if (l3[k].GetAttribute("class").Contains("stref"))
                                stop.strefowy = true;

                            if (d.GetAttribute("class").Contains("wariant"))
                                stop.wariant = true;

                            track.stops.Add(stop);
                        }

                        track.stops = track.stops.OrderBy(p => p.id).ToList();
                        val[i].track.Add(track);
                    }
                    val[i].id_linia = line_id;
                }

                return val;
            }
            catch { return null; }

        }

        public static async Task<string> GetHTML(string url)
        {
            HttpClient http = new HttpClient();
            HttpResponseMessage response = await http.GetAsync(url);
            byte[] data = await response.Content.ReadAsByteArrayAsync();

            var result = @Encoding.UTF8.GetString(data);

            return result;
        }

        public static List<Linia> ParserMain(string source)
        {
            try
            {
                var parser = new HtmlParser();
                var document = parser.Parse(source);

                var list = document.QuerySelectorAll("div").Where(p => p.GetAttribute("class") != null && p.GetAttribute("class").Contains("zbior_linii") && p.FirstChild != null).ToList();

                var list2 = new List<AngleSharp.Dom.IElement>();

                foreach (var item in list)
                    foreach (var item2 in item.Children)
                        if (item2.LocalName == "a" && item2.FirstChild != null && item2.FirstElementChild.LocalName == "span")
                            list2.Add(item2);

                // 0 - url
                // 1 - bus name
                // 2 - pfm

                List<Linia> val = new List<Linia>();

                for (int i = 0; i < list2.Count; i++)
                {
                    var linia = new Linia();

                    string url = @list2[i].GetAttribute("href");

                    if (url.Contains("koleje"))
                        continue;
                    linia.url = url;
                    linia.name = list2[i].FirstChild.TextContent.Trim();

                    var fv = list2[i].FirstElementChild.GetAttribute("class").Trim();

                    uint pfm = new uint();

                    /* zwykly = 1
                     * przyspieszony = 2
                     * tramwaj = 4
                     * mini = 8 
                     * lotnisko = 16
                     * nowy/swiezy/zaaktualizowny = 32
                     * zastępczy = 64
                     * wiekszy = 128
                     * nocna = 256
                     * bezplatny = 512
                    */

                    if (fv.Contains("zwykly"))
                        pfm += 1;
                    if (fv.Contains("przysp"))
                        pfm += 2;
                    if (fv.Contains("tram"))
                        pfm += 4;
                    if (fv.Contains("mini"))
                        pfm += 8;
                    if (fv.Contains("lot"))
                        pfm += 16;
                    if (fv.Contains("swiezy"))
                        pfm += 32;
                    if (fv.Contains("zast"))
                        pfm += 64;
                    if (fv.Contains("wiekszy"))
                        pfm += 128;
                    if (fv.Contains("nocna"))
                        pfm += 256;
                    if (fv.Contains("bezp"))
                        pfm += 512;

                    linia.pfm = pfm;

                    linia.id = i;
                    val.Add(linia);
                }
                val = val.OrderBy(p => p.id).ToList();

                return val;
            }
            catch { return null; }
        }
    }

    /* from StackOverflow, doing async a sync */
    namespace RozkladJazy.Modelnet
    {
        public static class AsyncHelpers
        {
            /// <summary>
            /// Execute's an async Task<T> method which has a void return value synchronously
            /// </summary>
            /// <param name="task">Task<T> method to execute</param>
            public static void RunSync(Func<Task> task)
            {
                var oldContext = SynchronizationContext.Current;
                var synch = new ExclusiveSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(synch);
                synch.Post(async _ =>
                {
                    try { await task(); }
                    catch (Exception e)
                    {
                        synch.InnerException = e;
                        throw;
                    }
                    finally { synch.EndMessageLoop(); }
                }, null);
                synch.BeginMessageLoop();

                SynchronizationContext.SetSynchronizationContext(oldContext);
            }

            /// <summary>
            /// Execute's an async Task<T> method which has a T return type synchronously
            /// </summary>
            /// <typeparam name="T">Return Type</typeparam>
            /// <param name="task">Task<T> method to execute</param>
            /// <returns></returns>
            public static T RunSync<T>(Func<Task<T>> task)
            {
                var oldContext = SynchronizationContext.Current;
                var synch = new ExclusiveSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(synch);
                T ret = default(T);
                synch.Post(async _ =>
                {
                    try
                    {
                        ret = await task();
                    }
                    catch (Exception e)
                    {
                        synch.InnerException = e;
                        throw;
                    }
                    finally
                    {
                        synch.EndMessageLoop();
                    }
                }, null);
                synch.BeginMessageLoop();
                SynchronizationContext.SetSynchronizationContext(oldContext);
                return ret;
            }

            private class ExclusiveSynchronizationContext : SynchronizationContext
            {
                private bool done;
                public Exception InnerException { get; set; }
                readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
                readonly Queue<Tuple<SendOrPostCallback, object>> items =
                    new Queue<Tuple<SendOrPostCallback, object>>();

                public override void Send(SendOrPostCallback d, object state)
                {
                    throw new NotSupportedException("We cannot send to our same thread");
                }

                public override void Post(SendOrPostCallback d, object state)
                {
                    lock (items)
                    {
                        items.Enqueue(Tuple.Create(d, state));
                    }
                    workItemsWaiting.Set();
                }

                public void EndMessageLoop()
                {
                    Post(_ => done = true, null);
                }

                public void BeginMessageLoop()
                {
                    while (!done)
                    {
                        Tuple<SendOrPostCallback, object> task = null;
                        lock (items)
                        {
                            if (items.Count > 0)
                            {
                                task = items.Dequeue();
                            }
                        }
                        if (task != null)
                        {
                            task.Item1(task.Item2);
                            if (InnerException != null) // the method threw an exeption
                            {
                                throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                            }
                        }
                        else
                        {
                            workItemsWaiting.WaitOne();
                        }
                    }
                }

                public override SynchronizationContext CreateCopy()
                {
                    return this;
                }
            }
        }
    }
}
