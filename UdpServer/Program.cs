using MyClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace UdpServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string ImagesPath = AppDomain.CurrentDomain.BaseDirectory.ToString();//путь к папке bin
            List<Recipe> recipes = new List<Recipe>//лист рецептов (переводим картинки в байты сразу)
            {
                new Recipe() {Name= "Борщ", Description="1-Сварите бульон, 2-Сделайте зажарку, 3-......",
                    Ingredients=new List<string> {"капуста", "картофель", "мясо", "лук"},
                    ByteImage_= Get_byte_from_path($"{ImagesPath}Images\\борщ.jpg")},
                new Recipe() {Name= "Жаренный картофель", Description="1-Почистить картофель и лук, 2-Пожарить на масле, 3-......",
                    Ingredients=new List<string> {"картофель","лук"},
                    ByteImage_ = Get_byte_from_path($"{ImagesPath}Images\\жареннаякарт.jpg")},
                new Recipe() {Name= "Карбонара",Description="1-Сварите пасту альденте, 2-Пожарьте бекон, 3-......",
                    Ingredients=new List<string> {"бекон", "паста", "чеснок", "лук"},
                    ByteImage_ = Get_byte_from_path($"{ImagesPath}Images\\карбонара.jpg")},
                new Recipe() {Name = "Окрошка", Description="1-Все сварить, 2-Все нарезать, 3-Добавить квас....",
                    Ingredients=new List<string> {"яйца", "картофель", "огурец", "лук"},
                    ByteImage_ = Get_byte_from_path($"{ImagesPath}Images\\окрошка.jpg")},
                new Recipe() {Name= "Салат", Description="1-Помыть овощи, 2-Нарезать кубиком, 3-Заправить маслом",
                    Ingredients=new List<string> {"помидор", "огурец", "лук"},
                    ByteImage_ = Get_byte_from_path($"{ImagesPath}Images\\салатовощи.jpg")},
            };
            BinaryFormatter formatter = new BinaryFormatter();
          
            MyUdpServer server = new MyUdpServer { udpServer = new UdpClient(5555), remoteEndpoint = new IPEndPoint(IPAddress.Any, 0) };

            Console.WriteLine("Waiting for the client.......");

            //udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.0"));//слушаем
            try
            {
                while (true)
                {
                    List<string> list = new List<string>();
                    List<Recipe> temp = new List<Recipe>();
                    byte[] byffer = server.ReciveServer();
                    string ansver = Encoding.UTF8.GetString(byffer);
                    Console.WriteLine(ansver);
                    switch (ansver)
                    {
                        case "0"://проверяем связь с сервером (типа приветствия)
                            server.SendServer(Encoding.UTF8.GetBytes("1"));
                            break;
                        case "1"://получить все рецепты
                            byffer = GetBytesFromList(recipes);
                            //отправляем размер нашего буфера
                            server.SendServer(Encoding.UTF8.GetBytes(byffer.Length.ToString()));
                            server.Send_Many_Bytes(byffer);
                            break;
                        case "2"://получить рецепты по ингридиентам
                            byffer = server.ReciveServer();                       
                            using (MemoryStream stream = new MemoryStream(byffer))
                            {
                                formatter = new BinaryFormatter();
                                list = (List<string>)formatter.Deserialize(stream);
                            }         
                            if (list.Count > 0)
                            {
                                temp = Choise(list);//плучаем все рецепты по запросу
                                byffer = GetBytesFromList(temp);
                                //отправляем размер нашего буфера
                                server.SendServer(Encoding.UTF8.GetBytes(byffer.Length.ToString()));
                                server.Send_Many_Bytes(byffer);
                            }
                                break;
                        case "3"://ищем по названию
                            byffer = server.ReciveServer();
                            string name = Encoding.UTF8.GetString(byffer);
                            if(name!=string.Empty)
                            {
                                temp = FindName(name);
                                byffer = GetBytesFromList(temp);
                                //отправляем размер нашего буфера
                                server.SendServer(Encoding.UTF8.GetBytes(byffer.Length.ToString()));
                                server.Send_Many_Bytes(byffer);
                            }
                            break;
                        default:
                            Console.WriteLine("НЕИЗВЕСТНЫЙ ЗАПРОС!");
                            break;

                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Connect Error");
            }
            finally
            {
                server.ServerClose();
            }

            //нужное
            List<Recipe> Choise(List<string> ingredients)//возвращает лист найденных рецептов
            {
                List<Recipe> choise = new List<Recipe>();
                choise = recipes.Where(r => isContain(r, ingredients)).ToList();
                return choise;
            }
            List<Recipe> FindName(string name)//ищем по названию
            {
                List<Recipe> choise = new List<Recipe>();
                choise = recipes.Where(r => r.Name.ToUpperInvariant().Contains(name.ToUpperInvariant())).ToList();
                return choise;
            }
            bool isContain(Recipe recipe, List<string> ingred)//если все ингредиенты найдены в рецепте в любом порядке, возвращает true
            {
                int ind = 0;
                foreach (var item in ingred)
                {
                    if (recipe.Ingredients.Contains(item)) { ind++; }
                }
                if (ind == ingred.Count()) return true;
                else return false;
            }
            byte[] GetBytesFromList(List<Recipe> temp)
            {

                using (MemoryStream stream = new MemoryStream())//переводим все рецепты в байты
                {
                    formatter = new BinaryFormatter();
                    formatter.Serialize(stream, temp);
                    byte[] byffer = stream.ToArray();
                    return byffer;
                }
               
            }
        }   
        public static byte[] Get_byte_from_path(string path)//переводим файл в байты (у нас картинка)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch
            {
                Console.WriteLine("Error Image Path!!!!");
                return null;
            }
        }
    }
}
