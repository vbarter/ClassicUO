using ClassicUO.Assets;
using Xunit;

namespace ClassicUO.UnitTests.Assets
{
    public class NameTranslationTests
    {
        private static NameTranslation Table()
        {
            var table = new NameTranslation();
            table.Parse(new[]
            {
                "# comment",
                "Alita\t阿莉塔",
                "the noble\t贵族",
                "a death adder\t死亡蝰蛇",
                "a horse\t马",
                "no translation\t",
                "Uzeraan\t乌泽兰"
            });

            return table;
        }

        [Fact]
        public void Skips_Comments_And_Empty_Translations()
        {
            Assert.Equal(5, Table().Count);
        }

        [Fact]
        public void Translates_Whole_Names_Ignoring_Case()
        {
            Assert.Equal("死亡蝰蛇", Table().Translate("A death adder"));
        }

        [Fact]
        public void Combines_Name_And_Title()
        {
            Assert.Equal("阿莉塔 贵族", Table().Translate("Alita the noble"));
        }

        [Fact]
        public void Keeps_Unknown_Name_With_Known_Title()
        {
            Assert.Equal("cjj 贵族", Table().Translate("cjj the noble"));
        }

        [Fact]
        public void Translates_State_Suffix()
        {
            Assert.Equal("马 (已驯服)", Table().Translate("a horse (tame)"));
        }

        [Fact]
        public void Translates_Text_Between_Tags()
        {
            Assert.Equal("<basefont color=#FFFFFF>乌泽兰</basefont>", Table().Translate("<basefont color=#FFFFFF>Uzeraan</basefont>"));
        }

        [Fact]
        public void Keeps_Surrounding_Spaces()
        {
            Assert.Equal(" 阿莉塔 ", Table().Translate(" Alita "));
        }

        [Fact]
        public void Leaves_Unknown_Names_Alone()
        {
            Assert.Equal("Lord Unknown", Table().Translate("Lord Unknown"));
        }
    }
}
