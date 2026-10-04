using ClassicUO.Resources;
using Xunit;

namespace ClassicUO.UnitTests.Resources
{
    public class ResourceLanguageTests
    {
        [Fact]
        public void Chinese_Language_Code_Uses_The_Chinese_Translation()
        {
            try
            {
                ResourceLanguage.Apply("CHS");

                Assert.Equal("取消", ResGumps.Cancel);
                Assert.Equal("你死了。", ResGeneral.YouAreDead);
                Assert.Equal("密码错误", ResErrorMessages.IncorrectPassword);
            }
            finally
            {
                ResourceLanguage.Apply("ENU");
            }
        }

        [Fact]
        public void Language_Without_Translation_Keeps_English()
        {
            ResourceLanguage.Apply("ENU");

            Assert.Equal("Cancel", ResGumps.Cancel);
        }

        [Fact]
        public void Language_Code_Is_Case_Insensitive()
        {
            try
            {
                Assert.NotNull(ResourceLanguage.Apply("chs"));
            }
            finally
            {
                ResourceLanguage.Apply("ENU");
            }
        }
    }
}
