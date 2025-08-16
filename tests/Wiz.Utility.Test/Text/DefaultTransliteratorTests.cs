using Shouldly;
using Wiz.Utility.Text;
using Xunit;

namespace Wiz.Utility.Test.Text
{
    public class DefaultTransliteratorTests
    {
        [Fact]
        public void Transliterate_Returns_Empty_When_Null()
        {
            var t = new DefaultTransliterator();
            t.Transliterate(null!).ShouldBe(string.Empty);
        }

        [Fact]
        public void Transliterate_Returns_Same_String_For_Ascii()
        {
            var t = new DefaultTransliterator();
            var s = "Hello World 123";
            t.Transliterate(s).ShouldBe(s);
        }

        [Fact]
        public void Transliterate_Does_Not_Remove_Diacritics()
        {
            var t = new DefaultTransliterator();
            var s = "café naïve"; // diacritics preserved (no-op)
            t.Transliterate(s).ShouldBe(s);
        }

        [Fact]
        public void Transliterate_Preserves_Thai_Text()
        {
            var t = new DefaultTransliterator();
            var s = "ทดสอบภาษาไทย ๑๒๓";
            t.Transliterate(s).ShouldBe(s);
        }
    }
}
