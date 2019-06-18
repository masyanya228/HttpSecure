using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AntiDdos
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }

        public static string kost = "\r\n";
        public static Form1 form;

        public static void COut(string str, ConsoleColor color = ConsoleColor.Gray)
        {
            lock (kost)
            {
                //Console.ForegroundColor = color;
                Form1.Sync.Send(new SendOrPostCallback((f) =>
                {
                    Form1.ConsoleTB.AppendText(str + kost);
                }), null);
            }
        }
        public static string Get_Def(string url, string Refer = null)
        {
            int st = Environment.TickCount;
            url = url.Replace(" ", "_");
            Uri ur = new Uri(url);
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.ReadWriteTimeout = 1000;
                req.Timeout = 1000;
                req.Host = ur.Host;
                req.UserAgent = "Mozilla/5.0(Windows NT 10.0; WOW64) AppleWebKit/537.36(KHTML, like Gecko) Chrome/69.0.3497.100 YaBrowser/18.10.0.2724 Yowser/2.5 Safari/537.36";
                req.Accept = "image/webp,image/apng,image/*,*/*;q=0.8";
                //req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                req.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru,en;q=0.9,de;q=0.8,fi;q=0.7");
                //req.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
                //req.CookieContainer = cookies;
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader sr = new StreamReader(stream, Encoding.Default);
                string Out = sr.ReadToEnd();
                sr.Close();
                //Referer = ur.OriginalString;
                return Out;
            }
            catch (Exception ex)
            {
                return " " + ex.Data;
            }
        }
    }
}
