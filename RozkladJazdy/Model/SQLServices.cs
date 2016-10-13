using SQLite.Net;
using SQLite.Net.Platform.WinRT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace RozkladJazdy.Model
{
    class SQLServices
    {
        private static SQLiteConnection con;
        private static SQLiteConnection con2;
        public static SQLiteConnection getConnection(int num = 0) => num == 0 ? con : con2;
        public SQLServices()
        {
            con = new SQLiteConnection(new SQLitePlatformWinRT(), Path.Combine(ApplicationData.Current.LocalFolder.Path, "RozkladJazdy.sqlite"));
            con2 = new SQLiteConnection(new SQLitePlatformWinRT(), Path.Combine(ApplicationData.Current.LocalFolder.Path, "Rozklad_fav.sqlite"));
        }
        public static void closeConnection()
        {
            con.Close();
            con2.Close();
        }
        public static void createDatabase<T>(int num = 0)
        {
            (num == 0 ? con : con2).CreateTable<T>();
        }
        public static void createDatabases()
        {
            con.CreateTable<Linia>();
            con.CreateTable<Rozklad>();
            con.CreateTable<Przystanek>();
            con.CreateTable<NazwaPrzystanku>();
            con.CreateTable<Godzina>();
            con.CreateTable<NazwaGodziny>();
            con.CreateTable<Trasa>();
            con.CreateTable<Literka>();
            con.CreateTable<PrzystanekListaPrzystanków>();
        }
        public static List<T> getData<T>(int num, string query, params object[] args) where T : class
        {
            return getConnection(num).Query<T>(query, args);
        }
        public static void addAllToDataBase<T>(IEnumerable data) where T : class
        {
            con.InsertOrReplaceAll(data, typeof(T));
        }
        public static void addToDataBase<T>(int num, T item) where T : class
        {
            getConnection(num).Insert(item, typeof(T));
        }
        public static List<T> getAllFromDataBase<T>() where T : class
        {
            return (con.Table<T>().ToList());
        }
    }
}
