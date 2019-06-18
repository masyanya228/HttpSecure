using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace AntiDdos
{
    class Server
    {
        public static int ddosMinTarget = 20;//Нижняя граница для блокировки/верняя для нормального поведения
        static public List<IPAddress> IPList = new List<IPAddress>();//Список ip адресов
        static public List<UInt16> RequestList = new List<UInt16>();//Смежный список их запросов за последнюю секунду
        public static TcpListener Listener; // Объект, принимающий TCP-клиентов http
        public static bool work = true;//Флаг работы сервера
        public static bool Closed = false;//Флаг подтверждающий закрытие сервера
        public static bool BlockWork = true;//Флаг работы защиты

        public Server(int Port)
        {
            int lt = Environment.TickCount;
            try
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start(10);
            }
            catch (SocketException ex)
            {
                Program.COut("Порт занят другим процессом!",ConsoleColor.Red);
                return;
            }
            catch (Exception ex)
            {
                Program.COut(ex.Data.ToString(), ConsoleColor.DarkRed);
            }
            Program.COut("HTTP server started on " + Port, ConsoleColor.Green);

            while (work)
            {
                TcpClient Cl = null;
                try
                {
                    Cl = Listener.AcceptTcpClient();//Подключение клиента, поступил запрос
                }
                catch(Exception ex)
                {
                    continue;
                }
                string ip = Cl.Client.RemoteEndPoint.ToString();                //
                IPAddress n = IPAddress.Parse(ip.Substring(0, ip.IndexOf(':')));//извлекаем ip

                #region
                //вывод информации о запросах каждую секунду
                if (Environment.TickCount - lt >= 1000)
                {
                    long allcol = 0;
                    for (int h = 0; h < IPList.Count; h++)
                    {
                        allcol += RequestList[h];
                    }
                    Program.COut("*****\r\n" + IPList.Count + " IP\r\n" + allcol + " Запросов\r\n*****");
                    RequestList.Clear();
                    IPList.Clear();
                    AbuseIP.IPClient.CheckOld();
                    lt = Environment.TickCount;
                }
                #endregion

                var ipinfo = AbuseIP.IPClient.Add(n, DateTime.Now);//Получаем информацию из БД - активная защита
                if (ipinfo.Block)
                {
                    Cl.Close();//Закрываем соединение, если IP в блокировке
                    continue;
                }
                else
                {
                    #region
                    //Пассивная защита
                    if (IPList.IndexOf(n) >= 0)
                    {
                        RequestList[IPList.IndexOf(n)]++;//Прибавляем к счетчику ip адреса 1
                        if (RequestList[IPList.IndexOf(n)] >= ddosMinTarget)//Проверяем на нормальное использование
                        {
                            ipinfo.BlockTime = DateTime.Now;
                            ipinfo.BlockTimes++;
                            ipinfo.Block = true;
                            Program.COut("Added to blacklist " + n, ConsoleColor.Red);
                            RequestList.RemoveAt(IPList.IndexOf(n));
                            IPList.Remove(n);
                        }
                    }
                    else
                    {
                        IPList.Add(n);
                        RequestList.Add(1);
                    }
                    #endregion
                }
                try
                {
                    //Если всё нормально, пропускаем запрос на обработку
                    Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                    Thread.Start(Cl);
                }
                catch (Exception ex)
                {

                }
            }
            Closed = true;
            Thread.Sleep(1000);
            Thread.CurrentThread.Abort();
        }
        
        static void ClientThread(Object StateInfo)
        {
            new Client((TcpClient)StateInfo);
            Thread.CurrentThread.Abort();
        }

        ~Server()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
    }
}