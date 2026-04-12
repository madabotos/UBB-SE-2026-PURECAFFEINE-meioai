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
            // arrange — null input

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(null!, out var parsedPrice);

            // assert
            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_WhitespaceInput_ReturnsFalse()
        {
            // arrange
            var whitespaceInput = "   ";

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(whitespaceInput, out var parsedPrice);

            // assert
            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_InvariantCultureDecimalSeparator_Parses()
        {
            // arrange — invariant culture uses '.' as decimal separator
            var invariantInput = "12.50";

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(invariantInput, out var parsedPrice);

            // assert
            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(12.5);
        }

        [Test]
        public void TryParsePriceInput_PlainInteger_Parses()
        {
            // arrange
            var integerInput = "42";

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(integerInput, out var parsedPrice);

            // assert
            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(42);
        }

        [Test]
        public void TryParsePriceInput_NonNumericText_ReturnsFalse()
        {
            // arrange
            var nonNumericInput = "banana";

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(nonNumericInput, out var parsedPrice);

            // assert
            wasParsed.Should().BeFalse();
            parsedPrice.Should().Be(0);
        }

        [Test]
        public void TryParsePriceInput_LeadingAndTrailingWhitespace_IsTrimmedAndParsed()
        {
            // arrange
            var paddedInput = "   7.25   ";

            // act
            var wasParsed = PriceInputParser.TryParsePriceInput(paddedInput, out var parsedPrice);

            // assert
            wasParsed.Should().BeTrue();
            parsedPrice.Should().Be(7.25);
        }
    }
}
