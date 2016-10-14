using SQLite.Net.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RozkladJazdy.Model
{
    public class GodzinaHours
    {
        public int nid { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public List<string> godziny { get; set; }
    }
    public class Literka
    {
        [AutoIncrement]
        [PrimaryKey]
        [Indexed]
        public int id { get; set; }

        public static int aid = 0;
        public Literka() { id = aid++; }
        public int id_przystanku { get; set; }
        public string info { get; set; }
    }
    public class Rozklad
    {
        [AutoIncrement]
        [PrimaryKey]
        [Indexed]
        public int id { get; set; }

        public static int aid = 0;
        public Rozklad() { id = aid++; }
        public int id_linia { get; set; }
        public string url { get; set; }
        public string text { get; set; }
        public bool actual { get; set; }
        [Ignore]
        public List<Trasa> track { get; set; }
    }
    public class Trasa
    {
        
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }

        public static int aid = 0;
        public int id_linia { get; set; }
        public int id_rozklad { get; set; }
        public string name { get; set; }
        public Trasa() { id = aid++; }
        [Ignore]
        public List<Przystanek> stops { get; set; }
    }
    public class Linia
    {
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }

        private uint _pfm;
        public uint pfm { get { return (_pfm == 2) ? 3 : _pfm; } set {_pfm = value; } }

        public string info { get; set; }

        public override string ToString() => name;

        public string getPfmText(uint pfm)
        {
            var returnString = string.Empty;

            if ((pfm & 1) == 0x1)
                returnString += "Autobus";
            if ((pfm & 4) == 0x4)
                returnString += "Tramwaj";
            if ((pfm & 8) == 0x8)
                returnString += "Minibus";
            if (returnString == string.Empty)
                returnString += "Autobus";
            if ((pfm & 2) == 0x2)
                returnString += " przyśpieszony";
            if ((pfm & 32) == 0x20)
                returnString += " zaaktualizowany";
            if ((pfm & 64) == 0x40)
                returnString += " zastępczy";
            if ((pfm & 128) == 0x80)
                returnString += " duży";
            if ((pfm & 256) == 0x100)
                returnString += " nocny";
            if ((pfm & 512) == 0x200)
                returnString += " bezpłatny";
            if ((pfm & 16) == 0x10)
                returnString += " na lotnisko";

            /* zwykly = 1
             * przyspieszony = 2
             * tramwaj = 4
             * mini = 8 
             * lotnisko = 16
             * nowy = 32
             * zastępczy = 64
             * wiekszy = 128
             * nocna = 256
             * bezplatny = 512
            */

            return returnString;
        }
        [Ignore]
        public List<Rozklad> rozklad { get; set; }
    }
    public class NazwaPrzystanku
    {
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }
        public static int aid = 0;
        public NazwaPrzystanku() { id = aid++; }
        public string name { get; set; }
    }
    public class Przystanek
    {
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }
        public int nid { get; set; }
        public string url { get; set; }
        public string getName() { return HTMLServices.przystankinames[nid].name; }
        public bool wariant { get; set; }
        public bool strefowy { get; set; }
        public bool na_zadanie() { return getName().Contains("n/ż"); }
        public int track_id { get; set; }
        public int rozkladzien_id { get; set; }
        public int id_trasa { get; set; }
        public int id_rozklad { get; set; }
        [Ignore]
        public List<Godzina> godziny { get; set; }
        [Ignore]
        public List<string> literki_info { get; set; }
    }
    public class NazwaGodziny
    {
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }
        public static int aid = 0;
        public NazwaGodziny() { id = aid++; }
        public string name { get; set; }
    }
    public class Godzina
    {
        [PrimaryKey]
        [AutoIncrement]
        [Indexed]
        public int id { get; set; }
        public int nid { get; set; }
        public string getName() { return HTMLServices.godzinynames[nid].name; }
        public string godziny_full { get; set; }
        public int id_przystanek { get; set; }
        public static int aid = 0;
        public Godzina() { id = aid++; }
        public override string ToString() => getName();
    }
    public class PrzystanekListaPrzystanków
    {
        public int przystanek_id { get; set; }
        public int przystanek_id2 { get; set; }
        public int linia_id { get; set; }
        public int rozklad_id { get; set; }
        public int trasa_id { get; set; }
    }

    public class PrzystanekListaPrzystanków2
    {
        public int line_id { get; set; }
        public string nazwa_lini { get; set; }
        public List<PrzystanekListaPrzystanków3> rozklady { get; set; }
    }

    public class PrzystanekListaPrzystanków3
    {
        public string name { get; set; }
        public List<PrzystanekListaPrzystanków4> kierunki { get; set; }
        public int roz_id { get; set; }
        public int line_id { get; set; }
    }
    public class PrzystanekListaPrzystanków4
    {
        public string name { get; set; }
        public int rozk_id { get; set; }
        public int track_id { get; set; }
        public int line_id { get; set; }
    }
    public class Ulubiony
    {
        public int type { get; set; } // 0 linie 1 przystanki
        public int id { get; set; }
        public string name { get; set; }

    }
}