using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace AntiDdos
{
    class AbuseIP
    {
        public class IPClient
        {
            public bool Block = false;
            public DateTime BlockTime;
            public DateTime LastTime = DateTime.Now;
            public uint BlockTimes = 0;
            public Dictionary<string, object> param = new Dictionary<string, object>();
            public DateTime FirstTime = DateTime.Now;
            public IPAddress ip;

            public static List<IPClient> lastconnects = new List<IPClient>();
            public static decimal MidPerMin = 0;
            public IPClient(IPAddress ip)
            {
                this.ip = ip;
            }
            public static void CheckOld()
            {
                lock (lastconnects)
                {
                    int blocked = 0;
                    for (int i = 0; i < lastconnects.Count; i++)
                    {
                        var length = DateTime.Now.Subtract(lastconnects[i].LastTime).TotalMinutes;
                        if (lastconnects[i].Block)
                        {
                            blocked++;
                            double totalMinutes = DateTime.Now.Subtract(lastconnects[i].BlockTime).TotalMinutes;
                            double v = Math.Pow(2, lastconnects[i].BlockTimes);
                            if (totalMinutes > v)
                            {
                                lastconnects[i].Block = false;
                            }
                        }
                        else if(length > 60 * 24 * 7)
                        {
                            lastconnects.RemoveAt(i);
                            i--;
                        } 
                    }
                }
            }
            public static IPClient Add(IPAddress ip, DateTime date)
            {
                lock (lastconnects)
                {
                    if (CheckNew(ip))//Есть ли ip в нашей базе
                    {
                        //Если нет, делай запрос к агрегатору базы данных ip адресов
                        IPClient newip = new IPClient(ip);//
                        lastconnects.Add(newip);          //Добавляем его в нашу БД
                        var outer = Task.Factory.StartNew(() =>
                        {
                            double ot = AbuseIP.CheckIP(newip.ip.ToString());//Получаем коэффициент от агрегатора
                            if (ot >= 0.5)
                            {
                                //Временная блокировка
                                newip.Block = true;
                                newip.BlockTime = DateTime.Now;
                                newip.BlockTimes = Math.Max((uint)Math.Ceiling(Math.Max(ot, 19)), newip.BlockTimes);//max rat - 19
                                Program.COut(newip.ip.ToString() + " - added to Blacklist with IP rating - " + Math.Round(ot, 1), ConsoleColor.Red);
                            }
                            else
                            {
                                //Всё нормально
                                Program.COut(newip.ip.ToString() + " IP rating - " + Math.Round(ot, 1), ConsoleColor.Green);
                            }
                        });
                        return newip;
                    }
                    else
                    {
                        //Если ip есть в БД, выдаем информацию по нему
                        var cl = GetByIP(ip);
                        cl.LastTime = DateTime.Now;
                        if (!Server.BlockWork)
                            cl.Block = false;
                        return cl;
                    }
                }
            }
            /// <summary>
            /// Поикс по IP в нашей БД
            /// </summary>
            /// <param name="ip"></param>
            /// <returns>true - если неизвестный, иначе false</returns>
            static bool CheckNew(IPAddress ip)
            {
                lock (lastconnects)
                {
                    for (int i = 0; i < lastconnects.Count; i++)
                    {
                        if (ip.ToString() == lastconnects[i].ip.ToString())
                            return false;
                    }
                }
                return true;
            }
            /// <summary>
            /// Сохранение БД
            /// </summary>
            public static void SaveAll()
            {
                try
                {
                    StreamWriter wr = new StreamWriter(Environment.CurrentDirectory + "\\BlackList.txt");
                    for (int i = 0; i < lastconnects.Count; i++)
                    {
                        try
                        {
                            wr.WriteLine(lastconnects[i].ip.ToString());
                            wr.WriteLine(lastconnects[i].Block.ToString());
                            wr.WriteLine(lastconnects[i].BlockTime.ToString());
                            wr.WriteLine(lastconnects[i].BlockTimes.ToString());
                            wr.WriteLine(lastconnects[i].FirstTime.ToString());
                            wr.WriteLine(lastconnects[i].LastTime.ToString());
                            wr.WriteLine("<>");
                        }
                        catch (Exception ex)
                        {
                            Program.COut("Error!!!\r\n" + ex.ToString(), ConsoleColor.Red);
                        }
                    }
                    wr.Close();
                }
                catch (Exception e)
                {
                    Program.COut("Error2!!!\r\n" + e.ToString(), ConsoleColor.Red);
                }
            }
            /// <summary>
            /// Чтение БД
            /// </summary>
            public static void ReadAll()
            {
                try
                {
                    if (!File.Exists(Environment.CurrentDirectory + "\\BlackList.txt"))
                        return;
                    StreamReader wr = new StreamReader(Environment.CurrentDirectory + "\\BlackList.txt");
                    string[] param = wr.ReadToEnd().Split(new String[] { "<>" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < param.Length; i++)
                    {
                        string[] args = param[i].Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        if (args.Length >= 1)
                        {
                            try
                            {
                                IPAddress add = IPAddress.Parse(args[0]);
                                IPClient ncl = new IPClient(add);

                                bool block = Convert.ToBoolean(args[1]);
                                DateTime.TryParse(args[2], out ncl.BlockTime);
                                uint.TryParse(args[3], out ncl.BlockTimes);
                                if(ncl.BlockTimes>0)
                                {
                                    
                                }
                                DateTime.TryParse(args[4], out ncl.FirstTime);
                                DateTime.TryParse(args[5], out ncl.LastTime);
                                lastconnects.Add(ncl);
                            }
                            catch (Exception ex)
                            {
                                Program.COut("Error!!!\r\n" + ex.ToString(), ConsoleColor.Red);
                            }
                        }
                    }
                    wr.Close();
                }
                catch (Exception e)
                {
                    Program.COut("Error2!!!\r\n" + e.ToString(), ConsoleColor.Red);
                }
            }
            /// <summary>
            /// Поикс по IP в нашей БД
            /// </summary>
            /// <param name="ip"></param>
            /// <returns>Возвращает информацию из нашей БД</returns>
            public static IPClient GetByIP(IPAddress ip)
            {
                lock (lastconnects)
                {
                    for (int i = 0; i < lastconnects.Count; i++)
                    {
                        if (ip.ToString() == lastconnects[i].ip.ToString())
                            return lastconnects[i];
                    }
                }
                return null;
            }
        }

        //https://www.abuseipdb.com/check/[IP]/json?key=[API_KEY]&days=[DAYS]//
        public static string APIkey = "x249dieAwummv9bgpN7P7fzp7R2Ai4NG65hHGTih";
        public static int days = 30;//Период поиска
        //Список интересующих нас категорий
        static string[] categories = new string[] {
        "Fraud Orders",
        "DDoS Attack",
        "FTP Brute-Force",
        "Ping of Death",
        "Phishing",
        "Fraud VoIP",
        "Open Proxy",
        "Web Spam",
        "Email Spam",
        "Blog Spam",
        "VPN IP",
        "Port Scan",
        "Hacking",
        "SQL Injection",
        "Spoofing",
        "Brute-Force",
        "Bad Web Bot",
        "Exploited Host",
        "Web App Attack",
        "SSH",
        "IoT Targeted"
        };
        static string[] critical = new string[] { "4", "6", "10", "11", "12", "14", "15", "16", "17", "18", "21" };
        /// <summary>
        /// Выдает коэффициент по безопасности IP адреса
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static double CheckIP(string ip)
        {
            return GetRating(GetBads(ip));
        }
        /// <summary>
        /// Выдает "бэды" по ip адресу, делая запрос к агрегатору
        /// </summary>
        /// <param name="ip"></param>
        /// <returns>Словарь бэдов-количество репортов за определенный период</returns>
        public static Dictionary<string, uint> GetBads(string ip)
        {
            string ot = Program.Get_Def("https://www.abuseipdb.com/check/" + ip + "/json?key=" + APIkey + "&days=" + days);//Сам запрос
            Dictionary<string, uint> bads = new Dictionary<string, uint>();
            //Дальше идет подсчет репортов, в интересующих нас, категориях
            string[] param = ot.Split(new string[] { "},{", "[{", "}]" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < param.Length; i++)
            {
                string[] args = param[i].Split(new string[] { "\",\"", ",\"" }, StringSplitOptions.RemoveEmptyEntries);
                for (int g = 0; g < args.Length; g++)
                {
                    args[g] = args[g].Replace("\":", "=").Replace("\"", "");
                }
                for (int g = 0; g < args.Length; g++)
                {
                    string[] spl = args[g].Split('=');
                    if (spl.Length == 2)
                    {
                        if (spl[0] == "category")
                        {
                            string[] cats = spl[1].Split(new string[] { "[", ",", "]" }, StringSplitOptions.RemoveEmptyEntries);
                            for (int h = 0; h < cats.Length; h++)
                            {
                                if (bads.ContainsKey(cats[h])) bads[cats[h]]++; else bads.Add(cats[h], 1);
                            }
                        }
                    }
                    else
                    {

                    }
                }
            }
            return bads;
        }
        /// <summary>
        /// По словарю бэдов вычисляется общий коэффициент
        /// </summary>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static double GetRating(Dictionary<string, uint> dic)
        {
            double war = 0;
            foreach (string key in dic.Keys)
            {
                if (critical.ToList().IndexOf(key) >= 0)
                    war += dic[key];
            }
            return war / 5;
        }
    }
}