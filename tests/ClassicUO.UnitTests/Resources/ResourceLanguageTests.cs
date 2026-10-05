using ClassicUO.Resources;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using System;
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
        public void Chinese_Translates_Captions_And_Macro_Names_Without_Touching_Custom_Text()
        {
            try
            {
                ResourceLanguage.Apply("CHS");
                Assert.Equal("冥河深渊", UiLocalization.MapPlaceName("Stygian Abyss"));
                Assert.Equal("不列颠", UiLocalization.MapPlaceName("Britain"));
                Assert.Equal("Britain", UiLocalization.MapPlaceName("Britain", userDefined: true));
                Assert.Equal("Alice's Home", UiLocalization.MapPlaceName("Alice's Home"));
                Assert.Equal("技能", UiLocalization.Translate("Skills"));
                Assert.Equal("杂项", UiLocalization.SkillGroupName("Miscellaneous"));
                Assert.Equal("技能", UiLocalization.GraphicText(0x0834));
                Assert.Equal("驯兽", UiLocalization.Translate("AnimalTaming"));
                Assert.Equal("以自己为目标", UiLocalization.Translate("TargetSelf"));
                Assert.Equal("玩家 Alice 的自定义宏", UiLocalization.Translate("玩家 Alice 的自定义宏"));
                Assert.Equal("<basefont color=#FFF>技能</basefont>", UiLocalization.Html("<basefont color=#FFF>Skills</basefont>"));
                Assert.Equal("<a href=Skills> Alice </a>", UiLocalization.Html("<a href=Skills> Alice </a>"));
                Assert.Equal("和平", UiLocalization.ArtCaption(0x07e5));
                Assert.Equal("Alice，初学者生物学家", UiLocalization.PaperdollTitle("Alice, Neophyte Biologist"));
                Assert.Equal("Alice，银行家", UiLocalization.PaperdollTitle("Alice the banker"));
                Assert.Equal("Alice，银行家", UiLocalization.PaperdollTitle("Alice, the banker"));
                Assert.Equal("Alice，驯兽师", UiLocalization.PaperdollTitle("Alice, The Animal Trainer"));
                Assert.Equal("the banker", UiLocalization.PaperdollTitle("the banker"));
                Assert.Equal("Alice, Custom Title", UiLocalization.PaperdollTitle("Alice, Custom Title"));
            }
            finally { ResourceLanguage.Apply("ENU"); }
        }

        [Fact]
        public void All_Macro_Captions_Are_Chinese_And_Spell_And_Skill_Identifiers_Are_Stable()
        {
            try
            {
                ResourceLanguage.Apply("CHS");
                foreach (string name in Enum.GetNames(typeof(MacroType)))
                    Assert.NotEqual(name, UiLocalization.Translate(name));
                foreach (string name in Enum.GetNames(typeof(MacroSubType)))
                    Assert.NotEqual(name, UiLocalization.Translate(name));
                var skill = new Skill("Animal Taming", 35, true);
                Assert.Equal("驯兽", skill.DisplayName);
                Assert.Equal("Animal Taming", skill.Name);
                var spell = SpellsMagery.GetSpell(1);
                string nameBeforeSwitch = spell.Name;
                int id = spell.ID;
                Assert.NotEqual(spell.Name, spell.DisplayName);
                ResourceLanguage.Apply("ENU");
                Assert.Equal(nameBeforeSwitch, spell.Name);
                Assert.Equal(nameBeforeSwitch, spell.DisplayName);
                Assert.Equal(id, spell.ID);
            }
            finally { ResourceLanguage.Apply("ENU"); }
        }

        [Fact]
        public void English_Keeps_Captions_And_Original_Button_Art()
        {
            ResourceLanguage.Apply("ENU");
            Assert.Equal("Alice the banker", UiLocalization.PaperdollTitle("Alice the banker"));
            Assert.Equal("Alice, the banker", UiLocalization.PaperdollTitle("Alice, the banker"));
            Assert.Equal("Britain", UiLocalization.MapPlaceName("Britain"));
            Assert.Equal("Skills", UiLocalization.Translate("Skills"));
            Assert.Equal("Miscellaneous", UiLocalization.SkillGroupName("杂项"));
            Assert.Equal("Custom Group", UiLocalization.SkillGroupName("Custom Group"));
            Assert.Null(UiLocalization.GraphicText(0x0834));
            Assert.Equal("AnimalTaming", UiLocalization.Translate("AnimalTaming"));
            Assert.Equal("<b>Skills</b>", UiLocalization.Html("<b>Skills</b>"));
            Assert.Null(UiLocalization.ArtCaption(0x07e5));
        }

        [Fact]
        public void Chat_Notifications_Resolve_The_Current_Language_After_First_Use()
        {
            try
            {
                ResourceLanguage.Apply("ENU");
                string english = ChatManager.GetMessage(0);
                Assert.DoesNotContain("屏蔽", english);
                ResourceLanguage.Apply("CHS");
                Assert.Equal("综合", UiLocalization.ChatChannelName("General"));
                Assert.Equal("My Custom Channel", UiLocalization.ChatChannelName("My Custom Channel"));
                Assert.Equal("General", UiLocalization.Translate("General"));
                Assert.Equal("你屏蔽的人数已达上限。", ChatManager.GetMessage(0));
                for (int i = 0; i < 41; i++) Assert.True(UiLocalization.RequiresUnicode(ChatManager.GetMessage(i)));
                Assert.Contains("%1", ChatManager.GetMessage(1));
                Assert.Equal(string.Empty, ChatManager.GetMessage(-1));
                Assert.Equal(string.Empty, ChatManager.GetMessage(41));
                ResourceLanguage.Apply("ENU");
                Assert.Equal("General", UiLocalization.ChatChannelName("General"));
                Assert.Equal(english, ChatManager.GetMessage(0));
            }
            finally { ResourceLanguage.Apply("ENU"); }
        }

        [Fact]
        public void Player_Speech_Is_Preserved_When_It_Matches_A_System_Template()
        {
            const string text = "You already have nightsight.";
            try
            {
                ResourceLanguage.Apply("CHS");
                Assert.Equal("你已经拥有夜视效果。", UiLocalization.SystemMessage(text, MessageType.System));
                Assert.Equal("你已经拥有夜视效果。", UiLocalization.SystemMessage(text, MessageType.Regular, serverSystem: true));
                foreach (var type in new[] { MessageType.Regular, MessageType.Party, MessageType.Guild,
                    MessageType.Alliance, MessageType.Whisper, MessageType.Yell, MessageType.Emote })
                    Assert.Equal(text, UiLocalization.SystemMessage(text, type));
                Assert.Equal("Unknown shard message", UiLocalization.SystemMessage("Unknown shard message", MessageType.System));
                Assert.True(UiLocalization.RequiresUnicode("生命值恢复 10 点"));
                Assert.False(UiLocalization.RequiresUnicode("Heal 10 HP"));
                ResourceLanguage.Apply("ENU");
                Assert.Equal(text, UiLocalization.SystemMessage(text, MessageType.System));
            }
            finally { ResourceLanguage.Apply("ENU"); }
        }

        [Fact]
        public void Spell_Messages_Translate_Names_And_Preserve_Incantations_And_Custom_Format()
        {
            try
            {
                var spell = SpellsMagery.GetSpell(1);
                var ability = SpellsBushido.GetSpell(1);
                ResourceLanguage.Apply("CHS");
                Assert.Equal("笨拙术（Uus Jux）", spell.GetDisplayMessage());
                Assert.Equal("[笨拙术] Uus Jux {custom}", spell.GetDisplayMessage(" [{spell}] {power} {custom} "));
                Assert.Equal(UiLocalization.Translate("Honorable Execution"), ability.GetDisplayMessage());
                Assert.Equal("Clumsy", spell.Name);
                Assert.Equal("Uus Jux", spell.PowerWords);
                ResourceLanguage.Apply("ENU");
                Assert.Equal("Uus Jux", spell.GetDisplayMessage());
                Assert.Equal("[Clumsy] Uus Jux {custom}", spell.GetDisplayMessage(" [{spell}] {power} {custom} "));
                Assert.Equal("Honorable Execution", ability.GetDisplayMessage());
            }
            finally { ResourceLanguage.Apply("ENU"); }
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
