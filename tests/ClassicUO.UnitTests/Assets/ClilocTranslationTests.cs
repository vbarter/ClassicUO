using System.Collections.Generic;
using ClassicUO.Assets;
using Xunit;

namespace ClassicUO.UnitTests.Assets
{
    public class ClilocTranslationTests
    {
        private static Dictionary<int, string> Parse(params string[] lines)
        {
            var entries = new Dictionary<int, string>();
            ClilocLoader.ParseTranslation(lines, entries);

            return entries;
        }

        [Fact]
        public void Reads_Number_Tab_Text()
        {
            Assert.Equal("背包", Parse("3000431\t背包")[3000431]);
        }

        [Fact]
        public void Unescapes_Newlines_Tabs_And_Backslashes()
        {
            Assert.Equal("第一行\n第二行\t后\\面", Parse("1\t第一行\\n第二行\\t后\\\\面")[1]);
        }

        [Fact]
        public void Keeps_Cliloc_Arguments()
        {
            Assert.Equal("你的 ~1_SKILL~ 技能提升了", Parse("2\t你的 ~1_SKILL~ 技能提升了")[2]);
        }

        [Fact]
        public void Skips_Comments_Blank_And_Malformed_Lines()
        {
            var entries = Parse("# comment", "", "no tab here", "abc\ttext", "\tmissing number", "5\tok");

            Assert.Single(entries);
            Assert.Equal("ok", entries[5]);
        }

        [Fact]
        public void Later_Lines_Override_Earlier_Ones()
        {
            var entries = new Dictionary<int, string> { [7] = "Backpack" };
            ClilocLoader.ParseTranslation(new[] { "7\t背包" }, entries);

            Assert.Equal("背包", entries[7]);
        }
    }
}
