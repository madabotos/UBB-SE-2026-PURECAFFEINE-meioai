using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class PriceInputParserTests
    {
        [Test]
        public void TryParsePriceInput_NullInput_ReturnsFalse()
        {
            var wasParsed = PriceInputParser.TryParsePriceInput(null!, out var parsedPrice);

            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_WhitespaceInput_ReturnsFalse()
        {
            var whitespaceInput = "   ";

            var wasParsed = PriceInputParser.TryParsePriceInput(whitespaceInput, out var parsedPrice);

            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_InvariantCultureDecimalSeparator_Parses()
        {
            var invariantInput = "12.50";

            var wasParsed = PriceInputParser.TryParsePriceInput(invariantInput, out var parsedPrice);

            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(12.5);
        }

        [Test]
        public void TryParsePriceInput_PlainInteger_Parses()
        {
            var integerInput = "42";

            var wasParsed = PriceInputParser.TryParsePriceInput(integerInput, out var parsedPrice);

            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(42);
        }

        [Test]
        public void TryParsePriceInput_NonNumericText_ReturnsFalse()
        {
            var nonNumericInput = "banana";

            var wasParsed = PriceInputParser.TryParsePriceInput(nonNumericInput, out var parsedPrice);

            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_LeadingAndTrailingWhitespace_IsTrimmedAndParsed()
        {
            var paddedInput = "   7.25   ";

            var wasParsed = PriceInputParser.TryParsePriceInput(paddedInput, out var parsedPrice);

            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(7.25);
        }
    }
}