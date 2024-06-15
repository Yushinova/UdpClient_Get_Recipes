using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyClasses
{
    [Serializable]
    public class Recipe
    {
        public string Description { get; set; }
        public List<string> Ingredients { get; set; }
        public string Name { get; set; }
        public byte[] ByteImage_ { get; set; }

    }
    public class User : INotifyPropertyChanged
    {
        public IPEndPoint endPoint {  get; set; }
        public DateTime firstquery {  get; set; }
        public string sessionstring { get; set; }
        public bool isblock { get; set; } = false;
        public int countquery { get; set; } = 0;

        public string Sessionstring
        {
            get { return sessionstring; }
            set
            {
                sessionstring = value;
                OnPropertyChanged("Sessionstring");
            }
        }
        public int Countquery
        {
            get { return countquery; }
            set
            {
                countquery = value;
                OnPropertyChanged("Countquery");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
    public class Query
    {
        public IPEndPoint endPoint { get; set; }
        public string code { get; set; }
        public DateTime timequery { get; set; }
        public override string ToString()
        {
            return $"[{endPoint.Address.ToString()}/{endPoint.Port.ToString()}] время: {timequery.ToShortTimeString()} {code}";
        }
    }

    public class MyUdpServer
    {
        public UdpClient udpServer { get; set; }

        public IPEndPoint clientpoint;

        public List<User> users = new List<User>();

        public void ServerClose()
        {
            udpServer.Close();
        }
        public byte[] ReciveServer()
        {
            try
            {
                byte[] byffer;
                var res = udpServer.ReceiveAsync();//ПРИНИМАЕМ ЗАПРОС ОТ КЛИЕНТА
                clientpoint = res.Result.RemoteEndPoint;
                byffer = res.Result.Buffer;// в виде листа ингридиентов
                return byffer;
            }
            catch
            {
                Console.WriteLine("Неизвестный запрос от клиента");
                return null;
            }

        }

        public void SendServer(byte[] data)
        {
            try
            {
                udpServer.SendAsync(data, data.Length, clientpoint);
            }
            catch
            {
                Console.WriteLine("Ошибка при отправке данных (простая отправка)");
            }

        }

        public async void Send_Many_Bytes(byte[] bytes)//разбиваем на части и отправляем частями по 8192 б
        {

            int Max_buf = 8192;
            List<byte> temp = bytes.ToList();
            byte[] b;
            while (temp.Count > 0)
            {
                if (temp.Count < Max_buf)
                {
                    b = temp.GetRange(0, temp.Count).ToArray();
                    temp.RemoveRange(0, temp.Count);
                }
                else
                {
                    b = temp.GetRange(0, Max_buf).ToArray();
                    temp.RemoveRange(0, Max_buf);
                }

                await udpServer.SendAsync(b, b.Length, clientpoint);
                //  Console.WriteLine(b.Length);
                Task.Delay(2).Wait();//без задержки передаются не все байты или принимающая сторона не успевает...
            }

        }

    }
    public class MyUdpClient
    {
        public UdpClient udpClient { get; set; }

        public IPEndPoint serverEndpoint { get; set; }//join другой адрес! "224.0.0.0"

        public void ConnectServer()
        {
            udpClient.Connect(serverEndpoint);
        }
        public void DisconectServer()
        {
            udpClient.Close();
        }
        public void SendClient(byte[] bytes)
        {
            try
            {
                udpClient.Send(bytes, bytes.Length);
            }
            catch (Exception) { }

        }
        public byte[] ReciveClient()
        {
            try
            {
                byte[] buffer;
                var res = udpClient.ReceiveAsync();
                buffer = res.Result.Buffer;//получаем размер буфера, который нам передают
                return buffer;
            }
            catch { return null; }
        }
        public byte[] Recive_Many_Bytes()//получение большого количества байтов
        {
            try
            {
                byte[] buffer;
                var res = udpClient.ReceiveAsync();
                buffer = res.Result.Buffer;//получаем размер буфера, который нам передают
                int count = int.Parse(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
                byte[] all = new byte[0];
                while (all.Length < count)
                {
                    res = udpClient.ReceiveAsync();
                    all = all.Concat(res.Result.Buffer).ToArray();
                    res.Dispose();
                    // Console.WriteLine(all.Length);
                }
                return all;
            }
            catch { return null; }

        }
    }
}
