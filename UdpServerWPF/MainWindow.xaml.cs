using MyClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Data;

namespace UdpServerWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string ImagesPath = AppDomain.CurrentDomain.BaseDirectory.ToString();//путь к папке bin
        public BinaryFormatter formatter = new BinaryFormatter();
        public List<Recipe> recipes;
        public ObservableCollection<User> users = new ObservableCollection<User>();
        public ObservableCollection<Query> queries = new ObservableCollection<Query>();
        public MyUdpServer server;
        public Task task;

        //ограничение количества подключений и ограничение по отдельным адресам
        public List<IPEndPoint> badadresses = new List<IPEndPoint>();//адреса с ограничением
        public int maxcountusers = int.MaxValue;//лимит для количества подключенных клиентов
        public int maxcountqueries;//лимит для количества запросов клиента
        public int limintime;//время за которое должны быть высчитаны запросы
        public MainWindow()
        {
            recipes = new List<Recipe>//лист рецептов получаем из базы данных (переводим картинки в байты сразу) 
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

            InitializeComponent();
            GridUsers.ItemsSource = users;
            ListQuery.ItemsSource = queries;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)//запуск сервера
        {
            try
            {
                server = new MyUdpServer { udpServer = new UdpClient(5555), clientpoint = new IPEndPoint(IPAddress.Any, 0) };
                task = Task.Factory.StartNew(StartServer);
            }
            catch
            {
                MessageBox.Show("Сервер уже запущен!");
            }
        }

        private void FinishButtom_Click(object sender, RoutedEventArgs e)//остановить все подключения
        {
            Task.Delay(2000).Wait();
            if (server != null)
            {
                server.ServerClose();
            }
            server = null;
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)//вызываем панель с настройками
        {
            SettingsGrid.Visibility = Visibility.Visible;
        }

        private void CloseSetButton_Click(object sender, RoutedEventArgs e)//закрываем панель с настойками
        {
            SettingsGrid.Visibility = Visibility.Hidden;
        }

        private void SetButton_Click(object sender, RoutedEventArgs e)//установка всех ограничений в настройках
        {
            if (int.TryParse(CountUserText.Text, out maxcountusers)) maxcountusers = int.Parse(CountUserText.Text);//количество клиентов

            if (int.TryParse(TimeText.Text, out limintime)) limintime = int.Parse(TimeText.Text);//лимит времени клиента

            if (int.TryParse(CountQueryText.Text, out maxcountqueries)) maxcountqueries = int.Parse(CountQueryText.Text);//количество запросов

            try
            {
                int port;
                if (int.TryParse(PortText.Text, out port))//
                {
                    port = int.Parse(PortText.Text);
                }
                IPAddress address = IPAddress.Parse(IPText.Text);
                IPEndPoint badpoint = new IPEndPoint(address, port);//получаем адрес ограниченного клиента
                badadresses.Add(badpoint);
            }
            catch
            {
                Dispatcher.Invoke(new Action(() => MessageBox.Show("Ошибка адрес/порт!")));
            }
        }
        ///нужное!!!!
        public void AddUser(IPEndPoint point, DateTime dateTime)//добавляем новогоклиента, если его нет еще в списке
        {
            if (users.Count > 0)
            {
                if (!users.Any(u => u.endPoint.ToString() == point.ToString()))
                {
                    users.Add(new User { endPoint = point, firstquery = dateTime });
                }
            }
            else
            {
                users.Add(new User { endPoint = point, firstquery = dateTime });
            }
        }
        public void AddQuery(IPEndPoint point, DateTime dateTime, string code_)//добавляем запросы в лист
        {
            string code = "";
            if (code_ == "0") code = "Запрос на подключение [0]";
            if (code_ == "1") code = "Получение всех рецептов [1]";
            if (code_ == "2") code = "Получить рецепты по ингридиентам [2]";
            if (code_ == "3") code = "Получить рецепты по названию [3]";
            Query query = new Query { endPoint = point, timequery = dateTime, code = code };
            queries.Add(query);
        }
        public void SetTimeQuery(IPEndPoint point, DateTime dateTime)//устанавливаем время длительности сессии, количество запросов
        {
            foreach (var item in users)
            {
                if (item.endPoint.ToString() == point.ToString())
                {
                    item.Timesession = new DateTime(dateTime.Subtract(item.firstquery).Ticks).ToLongTimeString();
                    item.Countquery++;
                }
            }
        }
        public void StartServer()//логика работы сервера (вынести отдельно!!)
        {
            Dispatcher.Invoke(new Action(() => MessageBox.Show("Сервер поключен!")));
            try
            {
                while (server != null)
                {
                    List<string> list = new List<string>();
                    List<Recipe> temp = new List<Recipe>();
                    byte[] byffer = server.ReciveServer();
                    string ansver = Encoding.UTF8.GetString(byffer);
                    if (users.Count < maxcountusers)//ограничение на количество подключенных клиентов
                    {
                        Dispatcher.Invoke(new Action(() => AddUser(server.clientpoint, DateTime.Now)));//добавление юзера, если его еще нет в списке

                    }
                    //проверка ограничения клиента 1-есть ли он в списке клиентов и нет ли на нем ограничений по запросам
                    if (users.Any(u => u.endPoint.ToString() == server.clientpoint.ToString()) && !IsBadUser(users.First(u => u.endPoint.ToString() == server.clientpoint.ToString())))
                    {
                        Dispatcher.Invoke(new Action(() => AddQuery(server.clientpoint, DateTime.Now, ansver)));//добавление запроса 
                        Dispatcher.Invoke(new Action(() => SetTimeQuery(server.clientpoint, DateTime.Now)));//переустановка длительности сессии и количества запросов
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
                                if (name != string.Empty)
                                {
                                    temp = FindName(name);
                                    byffer = GetBytesFromList(temp);
                                    //отправляем размер нашего буфера
                                    server.SendServer(Encoding.UTF8.GetBytes(byffer.Length.ToString()));
                                    server.Send_Many_Bytes(byffer);
                                }
                                break;
                            default:
                                MessageBox.Show("НЕИЗВЕСТНЫЙ ЗАПРОС!");
                                break;
                        }
                    }
                    else
                    {
                        server.SendServer(Encoding.UTF8.GetBytes("0"));
                    }

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Сервер отключен!");
            }
        }
        public bool IsBadUser(User user)//проверяем нашего клиента, если есть в списке
        {
            int countquery = 0;
            if (badadresses.Contains(user.endPoint))
            {
                //находим отчетное время с которого нужно проверять колиество запросов ограниченного клиента
                DateTime beginperiod = DateTime.Now.Subtract(new TimeSpan(0, limintime, 0));
                foreach (var item in queries)
                {
                    if (item.endPoint.ToString() == user.endPoint.ToString() && item.timequery > beginperiod)
                    {
                        countquery++;
                    }
                }
                if (countquery >= maxcountqueries) return true;
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }
        public List<Recipe> Choise(List<string> ingredients)//возвращает лист найденных рецептов
        {
            List<Recipe> choise = new List<Recipe>();
            choise = recipes.Where(r => isContain(r, ingredients)).ToList();
            return choise;
        }
        public List<Recipe> FindName(string name)//ищем по названию
        {
            List<Recipe> choise = new List<Recipe>();
            choise = recipes.Where(r => r.Name.ToUpperInvariant().Contains(name.ToUpperInvariant())).ToList();
            return choise;
        }
        public bool isContain(Recipe recipe, List<string> ingred)//если все ингредиенты найдены в рецепте в любом порядке, возвращает true
        {
            int ind = 0;
            foreach (var item in ingred)
            {
                if (recipe.Ingredients.Contains(item)) { ind++; }
            }
            if (ind == ingred.Count()) return true;
            else return false;
        }
        public byte[] GetBytesFromList(List<Recipe> temp)
        {
            using (MemoryStream stream = new MemoryStream())//переводим все рецепты в байты
            {
                formatter = new BinaryFormatter();
                formatter.Serialize(stream, temp);
                byte[] byffer = stream.ToArray();
                return byffer;
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
                MessageBox.Show("Error Image Path!!!!");
                return null;
            }
        }
    }
}
