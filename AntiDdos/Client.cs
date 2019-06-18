using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AntiDdos
{
    class Client
    {
        public static string UriToStr(string str)
        {
            Uri uriAddress = new Uri(str);
            return uriAddress.ToString();
        }
        public static string UriUnencode(string str)
        {
            return Uri.UnescapeDataString(str);
        }
        static string GetUnUri(string str)
        {
            while (true)
            {
                int pos = str.IndexOf('%');
                if (pos >= 0)
                {
                    if (str.Substring(pos + 1, 2) == "3A")
                    {
                        str = str.Replace("%3A", ":");
                    }
                }
                else
                    break;
            }
            return str;
        }
        // Отправка страницы с ошибкой
        private void SendError(TcpClient Client, int Code)
        {
            try
            {
                string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
                string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
                string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
                byte[] Buffer = Encoding.ASCII.GetBytes(Str);
                Client.GetStream().Write(Buffer, 0, Buffer.Length);
                Client.Close();
            }
            catch
            {

            }
        }

        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(TcpClient Client)
        {
            //Console.WriteLine(Test.tests.Count);
            int startt = Environment.TickCount;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            /*for (int i = Int32.MaxValue; i < Int32.MaxValue / 16; i++)
            {
                i += 1;
                i -= 1;
            }*/
            // Объявим строку, в которой будет хранится запрос клиента
            string Request = "";
            // Буфер для хранения принятых от клиента данных
            byte[] Buffer = new byte[1024];
            List<byte> req = new List<byte>();
            // Переменная для хранения количества байт, принятых от клиента
            int LostTicks = 0;
            List<string> map = new List<string>();
            int Count;
            int pausenum = 0;
            bool byteread = false;
            try
            {
                for (; Client.Connected;)
                {
                    if (Client.Available > 0)
                        Count = Client.GetStream().Read(Buffer, 0, Buffer.Length);
                    else
                        Count = 0;
                    Request += Encoding.UTF8.GetString(Buffer, 0, Count);
                    int l = Request.Length;
                    map.Add("add+" + Count);
                    if (Request.EndsWith("^"))
                    {
                        Request = Request.Remove(Request.Length - 1);
                        map.Add("bb^");
                        break;
                    }
                    else if (Request.StartsWith("GET") & Request.EndsWith("\r\n\r\n"))
                    {
                        map.Add("bb\\r\\r");
                        break;
                    }
                    else if (Request.StartsWith("POST") & Request.IndexOf("\r\n\r\n") >= 0 & pausenum >= 2)
                    {
                        map.Add("bb\\r\\r");
                        break;
                    }
                    if (Request.StartsWith("POST") & Request.IndexOf("\r\n\r\n") >= 0)
                    {
                        byteread = true;
                    }

                    if (Count == 0)
                    {
                        if (pausenum == 0)
                        {
                            Thread.Sleep(15);
                        }
                        else if (pausenum == 1)
                        {
                            Thread.Sleep(45);
                        }
                        else if (pausenum == 2)
                        {
                            Thread.Sleep(150);
                        }
                        else if (pausenum == 3)
                        {
                            Thread.Sleep(450);
                        }
                        else
                        {
                            break;
                        }
                        pausenum++;
                    }
                    else
                    {
                        pausenum = 0;
                    }

                    if (byteread)
                    {
                        req.AddRange(Buffer);
                        int last = req.Count - (Buffer.Length - Count);
                        for (; req.Count > last;)
                            req.RemoveAt(last);
                    }
                    if (Environment.TickCount - startt >= 5000 & false)
                    {
                        map.Add("bbt");
                        //Console.WriteLine("Time out");
                        Client.Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.COut("Http reading error " + Client.Client.RemoteEndPoint);
                if (Client.Connected)
                    Client.Close();
                return;
            }
            for (int i = 0; i < req.Count; i++)
            {
                if (req.IndexOf((byte)'\r', i) == -1)
                {
                    //cou
                }
            }

            Program.COut("*****\r\nConnected(HTTP) " + Client.Client.RemoteEndPoint + " " + Request.Length + "byte\r\n" + Request + "\r\n*****", ConsoleColor.Green);

            // Парсим строку запроса с использованием регулярных выражений
            // При этом отсекаем все переменные GET-запроса
            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");

            // Если запрос не удался
            if (ReqMatch == Match.Empty)
            {
                // Передаем клиенту ошибку 400 - неверный запрос
                SendError(Client, 400);
                return;
            }

            string RequestUri = ReqMatch.Groups[1].Value;
            if (RequestUri.IndexOf(":") >= 0)
            {
                RequestUri = RequestUri.Remove(0, RequestUri.IndexOf(":"));
            }
            if (RequestUri == "/")
            {
                RequestUri += "index.html";
            }
            /*if (RequestUri.IndexOf("/") >= 0)
            {
                RequestUri = RequestUri.Substring(RequestUri.LastIndexOf("/") + 1);
            }*/
            for (int i = 0; i < ReqMatch.Groups.Count; i++)
            {
                //Program.COut(ReqMatch.Groups[i].Value);
            }

            // Приводим ее к изначальному виду, преобразуя экранированные символы
            // Например, "%20" -> " "
            RequestUri = Uri.UnescapeDataString(RequestUri);

            // Если в строке содержится двоеточие, передадим ошибку 400
            // Это нужно для защиты от URL типа http://example.com/../../file.txt
            if (RequestUri.IndexOf("..") >= 0)
            {
                SendError(Client, 400);
                return;
            }
            string str = ReqMatch.Groups[0].Value;
            try
            {
                DoWork(Client, RequestUri, str, Request, req, sw);
            }
            catch (IOException ex)
            {
                Program.COut(ex.ToString());
            }
            finally
            {
            }
        }
        public string GetDir(string pa)
        {
            int p = pa.IndexOf('?');
            if (p > -1)
                return pa.Substring(0, p).Replace("/", "\\");
            else
                return pa.Replace("/", "\\");
        }
        string YM = "<!-- Yandex.Metrika counter --> <script type=\"text/javascript\" > (function (d, w, c) { (w[c] = w[c] || []).push(function() { try { w.yaCounter47916812 = new Ya.Metrika2({ id:47916812, clickmap:true, trackLinks:true, accurateTrackBounce:true }); } catch(e) { } }); var n = d.getElementsByTagName(\"script\")[0], s = d.createElement(\"script\"), f = function () { n.parentNode.insertBefore(s, n); }; s.type = \"text/javascript\"; s.async = true; s.src = \"https://mc.yandex.ru/metrika/tag.js\"; if (w.opera == \"[object Opera]\") { d.addEventListener(\"DOMContentLoaded\", f, false); } else { f(); } })(document, window, \"yandex_metrika_callbacks2\"); </script> <noscript><div><img src=\ttps://mc.yandex.ru/watch/47916812\" style=\"position:absolute; left:-9999px;\" alt=\"\" /></div></noscript> <!-- /Yandex.Metrika counter -->";
        public void DoWork(TcpClient Client, string RequestUri, string str, string Request, List<byte> req, Stopwatch startt)
        {
            if (RequestUri == "")
            {
                Client.Close();
                return;
            }
            int Count;
            /*if (RequestUri.IndexOf('.') == -1)
            {
                Server.col--;
                Client.Close();
                return;
            }*/
            int _toch = RequestUri.LastIndexOf('.');
            string Extension = "";
            if (_toch >= 0)
                Extension = RequestUri.Substring(RequestUri.LastIndexOf('.'));
            string ContentType = "";
            byte[] Buffer = new byte[1024];
            string FilePath = Environment.CurrentDirectory + "\\www\\" + GetDir(RequestUri);
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (str.IndexOf("?") > -1)
            {
                str = str.Substring(str.IndexOf("?") + 1, str.IndexOf(" ", str.IndexOf("?")) - str.IndexOf("?") - 1);
                string[] paramf = str.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                string sum = "";
                for (int i = 0; i < paramf.Length; i++)
                {
                    string[] fast = paramf[i].Split('=');
                    if (fast.Length == 2)
                    {
                        param.Add(fast[0], fast[1]);
                        sum += fast[0] + ":" + fast[1] + " ";
                    }
                }
                //Console.WriteLine(sum.Trim());
            }
            if (Request.StartsWith("POST"))
            {
                //param.Add("maindata", Request.Substring(Request.IndexOf("\r\n\r\n") + 4));
                string maindata = Request.Substring(Request.IndexOf("\r\n\r\n") + 4);

                //str = Uri.UnescapeDataString(str);
                string[] paramf = maindata.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                string sum = "";
                for (int i = 0; i < paramf.Length; i++)
                {
                    string[] fast = paramf[i].Split('=');
                    if (fast.Length == 2)
                    {
                        try
                        {
                            param.Add(fast[0], fast[1]);
                        }
                        catch (Exception ex)
                        {
                            param[fast[0]] += fast[1];
                        }
                        sum += fast[0] + ":" + fast[1] + " ";
                    }
                }
            }
            else if (Request.StartsWith("GET"))
            {

            }
            string FS = "";
            string ot = "";
            byte[] info;
            if (RequestUri.EndsWith("api"))
            {

            }

            FilePath = FilePath.Replace("\\\\", "\\");
            bool exist = false;
            try { exist = new FileInfo(FilePath).Exists; }
            catch (Exception ex)
            {
                SendError(Client, 500);
                return;
            }

            if (exist)
            {
                string CodeStr = "200 " + ((HttpStatusCode)200).ToString();
                string Headers = "HTTP/1.1 " + CodeStr + "\nContent-Type: " + ContentType + "\nContent-Length: ";
                if (Extension == ".html" | Extension == ".css")
                {
                    StreamReader f = new StreamReader(FilePath);
                    FS = f.ReadToEnd();
                    f.Close();
                    Buffer = Encoding.UTF8.GetBytes(FS);
                    Headers += Buffer.Length;

                    Headers += "\n\n";
                    byte[] HeadersBuffer = Encoding.UTF8.GetBytes(Headers);
                    try
                    {
                        Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
                        Client.GetStream().Write(Buffer, 0, Buffer.Length);
                    }
                    catch (Exception ex)
                    {
                        Program.COut("Closed by brouser " + ex.Message);
                    }
                    Client.Close();
                }
                else
                {
                    FileStream fstr = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    Headers += fstr.Length;
                    Headers += "\n\n";
                    byte[] HeadersBuffer = Encoding.UTF8.GetBytes(Headers);
                    try
                    {
                        Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);
                        while (fstr.Position < fstr.Length)
                        {
                            Buffer = new byte[Math.Min(fstr.Length, Int16.MaxValue)];
                            Count = fstr.Read(Buffer, 0, Buffer.Length);
                            Client.GetStream().Write(Buffer, 0, Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.COut("Closed " + ex.Message);
                    }
                    fstr.Close();
                    Client.Close();
                }
            }
            else
            {
                SendError(Client, 404);
                return;
            }
        }
        static bool SendHttp(string str, TcpClient Client)
        {
            try
            {
                byte[] info2 = Encoding.UTF8.GetBytes(str);
                string CodeStr2 = "200 " + ((HttpStatusCode)200).ToString();
                string Headers2 = "HTTP/1.1 " + CodeStr2 + "\r\nContent-Length: ";
                Headers2 = "HTTP/1.1 " + CodeStr2 + "\r\nContent-Type: text/html\r\nConnection: Closed\r\nContent-Length: ";
                Headers2 += info2.Length + "\r\n\r\n";
                Headers2 += str;
                byte[] HeadersBuffer2 = Encoding.UTF8.GetBytes(Headers2);
                Client.GetStream().Write(HeadersBuffer2, 0, HeadersBuffer2.Length);
                //Client.GetStream().Write(info2, 0, info2.Length);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}