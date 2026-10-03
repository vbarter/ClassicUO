using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.UnitTests.Input
{
    public class MouseTests
    {
        [Fact]
        public void WindowToGame_Unscaled_Returns_Same_Position()
        {
            Point p = Mouse.WindowToGame(120, 80, 800, 600, 800, 600, 1f);

            Assert.Equal(new Point(120, 80), p);
        }

        [Fact]
        public void WindowToGame_Divides_By_Dpi_Scale()
        {
            Point p = Mouse.WindowToGame(300, 150, 800, 600, 800, 600, 1.5f);

            Assert.Equal(new Point(200, 100), p);
        }

        [Fact]
        public void WindowToGame_Applies_BackBuffer_Ratio()
        {
            Point p = Mouse.WindowToGame(100, 50, 1600, 1200, 800, 600, 1f);

            Assert.Equal(new Point(200, 100), p);
        }
    }
}
