using System.Windows.Controls;

namespace Client
{
    public class Coordinate
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class Ship : Coordinate
    {
        public Image ShipImage { get; set; }
    }
}