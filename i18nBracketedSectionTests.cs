using System;
using i18n.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace i18n.Helpers.Tests
{
    [TestClass]
    public class i18nBracketedSectionTests
    {
        [TestMethod]
        public void BracketedSectionIgnoresPlainText()
        {
            // Arrange
            string original = "The quick brown fox jumps over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s);
            // Assert
            Assert.AreEqual(original, actual, false, @"Translate is supposed to ignore plain text, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionFindsOneEmbeddedBracket()
        {
            // Arrange
            string original = "The quick brown fox [[[jumps]]] over the lazy dog.";
            string expected = "The quick brown fox JUMPS over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s.ToUpper());
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to find embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionIgnoresUnclosedBracket()
        {
            // Arrange
            string original = "The quick brown fox [[[jumps over the lazy dog."; //]]]
            string expected = "The quick brown fox [[[jumps over the lazy dog."; //]]]
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s.ToUpper());
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to ignore unclosed brackes, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionFindsMultipleEmbeddedBrackets()
        {
            // Arrange
            string original = "The [[[quick]]] [[[brown]]] fox [[[jumps]]] over the lazy dog.";
            string expected = "The QUICK BROWN fox JUMPS over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s.ToUpper());
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to find multiple embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionAppliesParameters()
        {
            // Arrange
            string original = "The [[[%0 %1 fox|||quick|||brown]]] jumps over the lazy dog.";
            string expected = "The quick brown fox jumps over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s);
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to include parameters from embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionTranslatesParameters()
        {
            // Arrange
            string original = "The [[[%0 %1 fox|||(((quick)))|||brown]]] jumps over the lazy dog.";
            string expected = "The SPEEDY brown fox jumps over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s == "quick" ? "SPEEDY" : s);
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to translate tagged parameters from embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionFindsSubEmbeddedBracket()
        {
            // Arrange
            string original = "The quick brown fox [[[jumps [[[and leaps]]]]]] over the lazy dog.";
            string expected = "The quick brown fox bounces over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original,
                (s, c) => s == "jumps and bounds" ? "bounces" :
                         (s == "and leaps" ? "and bounds" :
                          s));
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to find embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionIgnoresInvalidBracket()
        {
            // Arrange
            string original = "The [[[quick]]] brown fox [[[jumps [[[and leaps]]] over the lazy dog."; //]]]
            string expected = "The QUICK brown fox [[[jumps [[[and leaps]]] over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original, (s, c) => s.ToUpper());
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to ignore invalid embedded brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionPassesContextThrough()
        {
            // Arrange
            string original = "The quick brown fox [[[jumps///CONTEXT]]] over the lazy dog.";
            string expected = "The quick brown fox bounces over the lazy dog.";
            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(original,
                (s, c) => c == "CONTEXT" && s == "jumps" ? "bounces" : "--FAILED--");
            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to pass context through, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionNotFound()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "" };
            string expected = "<not found>";
            // Act
            string actual = original.ToString();
            // Assert
            Assert.AreEqual(expected, actual, false, @"BracketedSection is supposed to say when it can't find a set of brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionIncomplete()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "[[[quick" };
            string expected = "<incomplete>[[[quick";
            // Act
            string actual = original.ToString();
            // Assert
            Assert.AreEqual(expected, actual, false, @"BracketedSection is supposed to say when it can't find a set of brackets, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionToString()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "[[[quick brown]]]" };
            string expected = "[[[quick brown]]]";
            // Act
            string actual = original.ToString();
            // Assert
            Assert.AreEqual(expected, actual, false, @"BracketedSection is supposed to show it's contents when it has a match, but it didn't.");
        }

        [TestMethod]
        public void BracketedSectionWithGrandchildrenOnlyReportsOneChild()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "[[[quick [[[brown [[[fox]]]]]]]]]" };
            int expected = 1;
            // Act
            int actual = original.ChildList.Count;
            // Assert
            Assert.AreEqual(expected, actual, @"BracketedSection is supposed to only count direct children, but it didn't.");
        }

        [TestMethod]
        public void EmptyBracketedSectionReportsNoChildren()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "" };
            int expected = 0;
            // Act
            int actual = original.ChildList.Count;
            // Assert
            Assert.AreEqual(expected, actual, @"NotFound BracketedSection is supposed to have no children, but it didn't.");
        }

        [TestMethod]
        public void IncompleteBracketedSectionReportsNoChildren()
        {
            // Arrange
            var original = new i18nBracketedSection() { Text = "[[[fox" };
            int expected = 0;
            // Act
            int actual = original.ChildList.Count;
            // Assert
            Assert.AreEqual(expected, actual, @"NotFound BracketedSection is supposed to have no children, but it didn't.");
        }

        [TestMethod]
        public void TestEverythingAtOnce()
        {
            // Arrange
            Func<string, string, string> getTextWithContext = (s, ctx) => {
                if (s == "some translatable bits") return "Sum TranSLATEable bytes";
                if (s == "enuf") return "enough";
                if (ctx == "context") return "the context";
                if (ctx == "cousin") return "Cousin IT from the Adaams Family";
                return s;
            };

            var text = @"A big long piece of text, populated with [[[some translatable bits]]], and some other
[[[bits with %0 parameters|||3]]], as well as existing [[[%0 translatable elements|||(((enuf)))]]].
It's worth noting that [[[it///context]]] and the other [[[it///cousin]]] are also handled.";

            var expected = @"A big long piece of text, populated with Sum TranSLATEable bytes, and some other
bits with 3 parameters, as well as existing enough translatable elements.
It's worth noting that the context and the other Cousin IT from the Adaams Family are also handled.";

            // Act
            string actual = i18nBracketedSection.FindAndTranslateBrackets(text, getTextWithContext);

            // Assert
            Assert.AreEqual(expected, actual, false, @"Translate is supposed to work in complex cases, but it didn't.");
        }
    }
}
