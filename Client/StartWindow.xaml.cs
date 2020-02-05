using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client
{
    public partial class StartWindow : Window
    {
        Coordinate prev = new Coordinate { Col = -1, Row = -1 };
        List<Ship> ships = new List<Ship>();

        public StartWindow()
        {
            MessageBox.Show("Note that you can move your ships clicking on them and empty space!", "Note", MessageBoxButton.OK, MessageBoxImage.Information);

            InitializeComponent();
            DrawSea();
            DrawBorders();
            GetShips();
        }

        private void GetShips()
        {
            ships = new List<Ship>();
            foreach (var ship in Map.Children.Cast<UIElement>().Where(s => s is Image && (s as Image).Tag.ToString().Length == 1))
            {
                ships.Add(new Ship
                {
                    ShipImage = ship as Image,
                    Col = Grid.GetColumn(ship),
                    Row = Grid.GetRow(ship)
                });
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(UserName.Text))
            {
                MessageBox.Show("Please, enter username!");
            }
            else
            {
                GameClient gameClient = new GameClient(UserName.Text, Thread.CurrentThread);
                if (gameClient.StartClient())
                {
                    MainWindow mainWindow = new MainWindow(gameClient, UserName.Text, ships);
                    mainWindow.Show();
                    Hide();
                }
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

        private void GetClickCoordinates(out int row, out int col)
        {
            col = row = 0;
            var point = Mouse.GetPosition(Map);
            double accumulatedHeight = 0.0;
            double accumulatedWidth = 0.0;
            foreach (var rowDefinition in Map.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                row++;
            }
            foreach (var columnDefinition in Map.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                col++;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int row = 0;
            int col = 0;
            if (prev.Col == -1 && prev.Row == -1)
            {
                GetClickCoordinates(out row, out col);
                prev = new Coordinate { Col = col, Row = row };
            }
            else
            {
                GetClickCoordinates(out row, out col);
                int Xdiff = col - prev.Col;
                int Ydiff = row - prev.Row;
                var shipPart = ships.FirstOrDefault(s => s.Col == prev.Col && s.Row == prev.Row);
                if (shipPart == null)
                {
                    prev = new Coordinate { Col = col, Row = row };
                    return;
                }
                string tag = (shipPart.ShipImage as Image).Tag.ToString();
                var ship = ships.Where(s => (s.ShipImage as Image).Tag.ToString() == tag);
                try
                {
                    foreach (var part in ship)
                    {
                        int newCol = Grid.GetColumn(part.ShipImage) + Xdiff;
                        int newRow = Grid.GetRow(part.ShipImage) + Ydiff;
                        if (newCol < 0 || newCol > 9 || newRow < 0 || newRow > 9) throw new Exception();

                        var neighbours = GetNeighbours(newCol, newRow);
                        var shipParts = neighbours.Where(n => (n as Image).Tag.ToString().Length == 1);
                        var notTheSame = shipParts.Where(n => (n as Image).Tag.ToString() != part.ShipImage.Tag.ToString());
                        if (shipParts.Count() != 0 && notTheSame.Count() != 0) throw new Exception();
                    }
                    foreach (var part in ship)
                    {
                        int newCol = Grid.GetColumn(part.ShipImage) + Xdiff;
                        int newRow = Grid.GetRow(part.ShipImage) + Ydiff;
                        Grid.SetColumn(part.ShipImage, newCol);
                        Grid.SetRow(part.ShipImage, newRow);
                    }
                    GetShips();
                    DrawBorders();
                }
                catch { }
                prev = new Coordinate { Col = -1, Row = -1 };
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int row = 0;
            int col = 0;
            GetClickCoordinates(out row, out col);
            string shipTag = ships.FirstOrDefault(s => s.Col == col && s.Row == row).ShipImage.Tag.ToString();
            var ship = ships.Where(s => (s.ShipImage as Image).Tag.ToString() == shipTag).ToList();
            if (ship.Count == 1) return;
            
            if (Horizontal(ship) && CanFlipToVertical(ship, row, col))
            {
                for (int i = 0; i < ship.Count; i++)
                {
                    Grid.SetColumn(ship[i].ShipImage, col);
                    Grid.SetRow(ship[i].ShipImage, row + i);
                    GetShips();
                    DrawBorders();
                }
            }
            else if (!Horizontal(ship) && CanFlipToHorizontal(ship, row, col))
            {
                for (int i = 0; i < ship.Count; i++)
                {
                    Grid.SetColumn(ship[i].ShipImage, col + i);
                    Grid.SetRow(ship[i].ShipImage, row);
                    GetShips();
                    DrawBorders();
                }
            }
        }

        private bool CanFlipToHorizontal(List<Ship> ship, int row, int col)
        {
            for (int i = 0; i < ship.Count; i++)
            {
                if (col + i > 9) return false;
                var parts = ships.Where(s => s.Row == row && s.Col == col + 1 && s.ShipImage.Tag.ToString() != ship[i].ShipImage.Tag.ToString()).ToList();
                if (parts.Count > 0) return false;

                var neighbours = GetNeighbours(col + 1, row);
                var borders = neighbours.Where(s => (s as Image).Tag.ToString().Length == 1 && (s as Image).Tag.ToString() != ship[i].ShipImage.Tag.ToString()).ToList();
                if (borders.Count > 0) return false;
            }
            return true;
        }

        private bool CanFlipToVertical(List<Ship> ship, int row, int col)
        {
            for (int i = 0; i < ship.Count; i++)
            {
                if (row + i > 9) return false;
                var parts = ships.Where(s => s.Row == row + i && s.Col == col && s.ShipImage.Tag.ToString() != ship[i].ShipImage.Tag.ToString()).ToList();
                if (parts.Count > 0) return false;

                var neighbours = GetNeighbours(col, row + i);
                var borders = neighbours.Where(s => (s as Image).Tag.ToString().Length == 1 && (s as Image).Tag.ToString() != ship[i].ShipImage.Tag.ToString()).ToList();
                if (borders.Count > 0) return false;
            }
            return true;
        }

        private bool Horizontal(List<Ship> ship)
        {
            if (ship[0].Row == ship[1].Row) return true;
            return false;
        }

        private void UserName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Connect_Click(null, null);
            }
        }
    }
}
