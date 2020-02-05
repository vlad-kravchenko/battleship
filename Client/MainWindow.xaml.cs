using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client
{
    public partial class MainWindow : Window
    {
        GameClient gameClient;
        string user;
        List<Ship> ships;
        int leftMy = 20;
        int leftEnemy = 20;
        bool canShot = true;

        public MainWindow(GameClient gameClient, string user, List<Ship> ships)
        {
            InitializeComponent();
            this.gameClient = gameClient;
            this.gameClient.ReceiveResult += ReceiveResult;
            this.gameClient.ReceiveShot += ReceiveShot;
            this.gameClient.WriteLog += WriteLog;
            this.user = user;
            this.ships = ships;
            About.Text = $"Hello, {user}! Let's the battle begins!";

            SetupGrids();
            DrawSea();
            DrawShips();
            DrawBorders();
        }

        private void DrawShips()
        {
            foreach(var ship in ships)
            {
                var image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = Resources["Ship"] as BitmapImage,
                    Tag = ship.ShipImage.Tag
                };
                Panel.SetZIndex(image, 10);
                Map.Children.Add(image);
                Grid.SetRow(image, ship.Row);
                Grid.SetColumn(image, ship.Col);
            }
        }

        private void SetupGrids()
        {
            Map.ShowGridLines = true;
            Enemy.ShowGridLines = true;
            for(int i = 0; i < 10; i++)
            {
                Map.RowDefinitions.Add(new RowDefinition());
                Enemy.RowDefinitions.Add(new RowDefinition());
                Map.ColumnDefinitions.Add(new ColumnDefinition());
                Enemy.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        private void DrawSea()
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    var image = new Image
                    {
                        Stretch = Stretch.Fill,
                        Source = Resources["Sea"] as BitmapImage,
                        Tag = "Sea"
                    };
                    Panel.SetZIndex(image, 1);
                    Map.Children.Add(image);
                    Grid.SetRow(image, y);
                    Grid.SetColumn(image, x);
                }
            }
        }

        private void DrawBorders()
        {
            var borders = Map.Children.Cast<UIElement>().Where(c => (c as Image).Tag.ToString() == "Border").ToList();
            for (int i = 0; i < borders.Count; i++)
            {
                Map.Children.Remove(borders[i]);
            }
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    List<UIElement> neighbours = GetNeighbours(x, y);
                    neighbours = neighbours.Where(n => (n as Image).Tag.ToString().Length == 1).ToList();
                    if (neighbours.Count > 0)
                    {
                        var image = new Image
                        {
                            Stretch = Stretch.Fill,
                            Source = Resources["Border"] as BitmapImage,
                            Tag = "Border"
                        };
                        Panel.SetZIndex(image, 2);
                        image.Opacity = 0.4;
                        Map.Children.Add(image);
                        Grid.SetRow(image, y);
                        Grid.SetColumn(image, x);
                    }
                }
            }
        }

        private List<UIElement> GetNeighbours(int x, int y)
        {
            List<UIElement> neighbours = new List<UIElement>();
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y - 1 && Grid.GetColumn(c) == x - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y - 1 && Grid.GetColumn(c) == x).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y - 1 && Grid.GetColumn(c) == x + 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y && Grid.GetColumn(c) == x - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y && Grid.GetColumn(c) == x + 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y + 1 && Grid.GetColumn(c) == x - 1).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y + 1 && Grid.GetColumn(c) == x).ToList());
            neighbours.AddRange(Map.Children.Cast<UIElement>().Where(c => Grid.GetRow(c) == y + 1 && Grid.GetColumn(c) == x + 1).ToList());
            return neighbours;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            gameClient.Disconnect();
            Application.Current.Shutdown();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!canShot)
            {
                MessageBox.Show("It's not your turn!");
                return;
            }
            int row = 0;
            int col = 0;
            GetClickCoordinates(out row, out col);
            gameClient.SendShot(row, col);
        }

        public void ReceiveShot(int row, int col)
        {
            var elements = Map.Children.Cast<UIElement>().Where(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col).ToList();
            var shipParts = elements.Where(e => (e as Image).Tag.ToString().Length == 1).ToList();
            if (shipParts.Count == 0)
            {
                gameClient.SendResult("Miss", row, col);
                for(int i = 0; i < elements.Count; i++)
                {
                    Map.Children.Remove(elements[i]);
                }
                var image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = Resources["Miss"] as BitmapImage,
                    Tag = "Miss"
                };
                Panel.SetZIndex(image, 2);
                Map.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
            }
            else
            {
                gameClient.SendResult("Hit", row, col);
                for (int i = 0; i < elements.Count; i++)
                {
                    Map.Children.Remove(elements[i]);
                }
                var image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = Resources["Hit"] as BitmapImage,
                    Tag = "Hit"
                };
                Panel.SetZIndex(image, 10);
                Map.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
                leftMy--;
                if (leftMy == 0)
                {
                    WriteLog(user + " lost!");
                    MessageBox.Show("You lost.");
                }
            }
            canShot = true;
        }

        public void ReceiveResult(string result, int row, int col)
        {
            Image image;
            if (result == "Hit")
            {
                image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = Resources["Hit"] as BitmapImage,
                    Tag = "Hit"
                };
                Panel.SetZIndex(image, 10);
                Enemy.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
                canShot = true;
                leftEnemy--;
                if (leftEnemy == 0)
                {
                    WriteLog(user + " won!");
                    MessageBox.Show("Victory!");
                }
            }
            else
            {
                image = new Image
                {
                    Stretch = Stretch.Fill,
                    Source = Resources["Miss"] as BitmapImage,
                    Tag = "Miss"
                };
                Panel.SetZIndex(image, 10);
                Enemy.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
                canShot = false;
            }
        }

        public void WriteLog(string message)
        {
            Log.Text += message + Environment.NewLine;
        }

        private void GetClickCoordinates(out int row, out int col)
        {
            col = row = 0;
            var point = Mouse.GetPosition(Enemy);
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;
            foreach (var rowDefinition in Enemy.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }
            foreach (var columnDefinition in Enemy.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }
        }
    }
}