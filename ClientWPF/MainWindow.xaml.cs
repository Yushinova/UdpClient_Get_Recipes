using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
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
using MyClasses;
using System.IO;
using System.Collections.ObjectModel;

namespace ClientWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> Ingredients = new List<string>
        {
            "картофель","морковь","огурец","помидор","лук","мясо","капуста","бекон","чеснок", "паста", "яйца"
        };
        public MyUdpClient client = new MyUdpClient { udpClient = new UdpClient(), serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555) };
        public ObservableCollection<Recipe> Resipes = new ObservableCollection<Recipe>();
        public List<string> changeingrid = new List<string>();
        public BinaryFormatter formatter = new BinaryFormatter();
       public List<Recipe> recipes = new List<Recipe>();
        public int findMode;
        public MainWindow()
        {

            InitializeComponent();
            SetIngrid.ItemsSource = Ingredients;
            ListRecipe.ItemsSource = Resipes;
            client.ConnectServer();
            // Task.Run(SetConnection);//можно ли так делать? нельзя все виснет

        }

        private void SetConnection()
        {

            try
            {
               
                client.SendClient(Encoding.UTF8.GetBytes("0"));
                byte[] ansver;

                ansver = client.ReciveClient();
                if (Encoding.UTF8.GetString(ansver) == "1")
                {
                    Dispatcher.Invoke(new Action(() => Online.Fill = new SolidColorBrush(Colors.Green)));
                    Dispatcher.Invoke(new Action(() => OnlineText.Text = "Сервер доступен!"));
                    //break;
                }
            }
            catch
            {
                Dispatcher.Invoke(new Action(() => Online.Fill = new SolidColorBrush(Colors.Red)));
                Dispatcher.Invoke(new Action(() => OnlineText.Text = "Сервер недоступен!"));
            }


        }

        private void AllRecipeButton_Click(object sender, RoutedEventArgs e)//просим у сервера все рецепты
        {
            Resipes.Clear();
            client.SendClient(Encoding.UTF8.GetBytes("1"));//код 1-получить все рецепты
            try
            {

                byte[] all = client.Recive_Many_Bytes();//получаем буфер

               
                using (MemoryStream stream = new MemoryStream(all))//десериализует все верно!
                {
                    formatter = new BinaryFormatter();
                    recipes = (List<Recipe>)formatter.Deserialize(stream);
                }
                foreach (var item in recipes)
                {
                    Resipes.Add(item);
                }
            }
            catch
            {
                MessageBox.Show("Связь с сервером нестабильна((");
            }
        }

        public static byte[] Recive_Many_Bytes(UdpClient server, int count)//получение большого количества байтов
        {

            byte[] all = new byte[0];
            while (all.Length < count)
            {
                var res = server.ReceiveAsync();
                all = all.Concat(res.Result.Buffer).ToArray();
                res.Dispose();
                // Console.WriteLine(all.Length);
            }
            return all;
        }

        private void FindIngridButton_Click(object sender, RoutedEventArgs e)
        {
            SetIngrid.Visibility = Visibility.Visible;
            FindText.Visibility = Visibility.Hidden;
            findMode = 1;

        }

        private void SetIngrid_SelectionChanged(object sender, SelectionChangedEventArgs e)//выбираем ингридиенты и добавляем их в лист
        {
            StackPanel panel = new StackPanel();
            panel.Background = new SolidColorBrush(Colors.LightGray);
            panel.Orientation = Orientation.Horizontal;
            panel.Width = 80;
            panel.Height = 20;
            panel.Margin = new Thickness(10, 0, 0, 0);

            TextBlock text = new TextBlock();
            text.Text = SetIngrid.SelectedItem.ToString();
            text.Margin = new Thickness(5, 0, 0, 0);
            Button button = new Button();
            button.Width = 12;
            button.Height = 12;
            button.Background = new SolidColorBrush(Colors.LightBlue);
            button.Padding = new Thickness(0, -5, 0, 0);
            button.Content = "x";

            button.Margin = new Thickness(5, 0, 0, 0);
            button.Click += new RoutedEventHandler(Delet_Ingredients_Click);//кнопка удаления любого ингридиента событие
            panel.Children.Add(text);
            panel.Children.Add(button);
            Grid_4.Children.Add(panel);
            changeingrid.Add(text.Text);

        }

        private void Delet_Ingredients_Click(object sender, RoutedEventArgs e)
        {
            string str = (((sender as Button).Parent as StackPanel).Children[0] as TextBlock).Text;
            changeingrid.Remove(str);
            Grid_4.Children.Remove((sender as Button).Parent as StackPanel);
        }

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] bytes;
            if (findMode == 1)
            {
                SetIngrid.Visibility = Visibility.Hidden;
                Grid_4.Children.Clear();
                if (changeingrid.Count > 0)
                {

                    using (MemoryStream stream = new MemoryStream())
                    {
                        formatter = new BinaryFormatter();
                        formatter.Serialize(stream, changeingrid);
                        bytes = stream.ToArray();
                    }
                    client.SendClient(Encoding.UTF8.GetBytes("2"));
                    client.SendClient(bytes);
                    changeingrid.Clear();
                    try
                    {

                        byte[] all = client.Recive_Many_Bytes();//получаем буфер

                        using (MemoryStream stream = new MemoryStream(all))//десериализует все верно!
                        {
                            formatter = new BinaryFormatter();
                            recipes = (List<Recipe>)formatter.Deserialize(stream);
                        }
                        if (recipes.Count == 0)
                        {
                            MessageBox.Show("Рецепты не найдены. Попробуйте изменить ингридиенты");
                        }
                        Resipes.Clear();
                        foreach (var item in recipes)
                        {

                            Resipes.Add(item);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Связь с сервером нестабильна((");
                    }
                }
            }
            else
            {
                if (FindText.Text != string.Empty)
                {
                    client.SendClient(Encoding.UTF8.GetBytes("3"));
                    client.SendClient(Encoding.UTF8.GetBytes(FindText.Text));
                    try
                    {

                        byte[] all = client.Recive_Many_Bytes();//получаем буфер

                        using (MemoryStream stream = new MemoryStream(all))//десериализует все верно!
                        {
                            formatter = new BinaryFormatter();
                            recipes = (List<Recipe>)formatter.Deserialize(stream);
                        }
                        if (recipes.Count == 0)
                        {
                            MessageBox.Show("Рецепты не найдены");
                        }
                        Resipes.Clear();
                        foreach (var item in recipes)
                        {

                            Resipes.Add(item);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Связь с сервером нестабильна((");
                    }
                }
            }

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            SetConnection();
        }

        private void FindNameButton_Click(object sender, RoutedEventArgs e)//искать по названию
        {
            findMode = 2;
            FindText.Visibility = Visibility.Visible;
        }
    }
}
