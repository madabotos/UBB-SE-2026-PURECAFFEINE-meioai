using System;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class GameInputHelperTests
    {
        private const int MinimumNameLength = 5;
        private const int MaximumNameLength = 30;
        private const decimal MinimumAllowedPrice = 1m;
        private const int MinimumPlayerCount = 1;
        private const int MinimumDescriptionLength = 10;
        private const int MaximumDescriptionLength = 500;

        private const string ValidName = "Valid Name";
        private const decimal ValidPrice = 5m;
        private const int ValidMinPlayers = 2;
        private const int ValidMaxPlayers = 4;
        private const string ValidDescription = "A description long enough.";

        [Test]
        public void BuildValidationErrors_AllInputsValid_ReturnsEmptyList()
        {
            // arrange — defaults above are valid

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().BeEmpty();
        }

        [Test]
        public void BuildValidationErrors_NameTooShort_ReportsNameLengthError()
        {
            // arrange
            var shortName = "abc";

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                shortName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_NameNullOrWhitespace_ReportsNameLengthError()
        {
            // arrange
            var whitespaceName = "   ";

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                whitespaceName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_NameTooLong_ReportsNameLengthError()
        {
            // arrange
            var overLongName = new string('x', MaximumNameLength + 1);

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                overLongName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_PriceBelowMinimum_ReportsPriceError()
        {
            // arrange
            var belowMinimumPrice = 0m;

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, belowMinimumPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("Price"));
        }

        [Test]
        public void BuildValidationErrors_MinimumPlayersBelowMinimum_ReportsPlayerCountError()
        {
            // arrange
            var belowMinimumPlayerCount = 0;

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, belowMinimumPlayerCount, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("player", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void BuildValidationErrors_MaximumPlayersBelowMinimum_ReportsPlayerOrderError()
        {
            // arrange — max < min
            var outOfOrderMin = 4;
            var outOfOrderMax = 2;

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, outOfOrderMin, outOfOrderMax, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("player", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void BuildValidationErrors_DescriptionTooShort_ReportsDescriptionError()
        {
            // arrange
            var shortDescription = "short";

            // act
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, shortDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            // assert
            errors.Should().Contain(message => message.Contains("Description"));
        }

        [Test]
        public void EnsureImageOrDefault_ImageProvided_ReturnsSameImage()
        {
            // arrange
            var providedImageBytes = new byte[] { 0x01, 0x02, 0x03 };

            // act
            var result = GameInputHelper.EnsureImageOrDefault(providedImageBytes, baseDirectory: ".");

            // assert
            result.Should().BeSameAs(providedImageBytes);
        }

        [Test]
        public void EnsureImageOrDefault_NullImageMissingDefault_ReturnsEmptyArray()
        {
            // arrange — baseDirectory points at a folder with no Assets/default-game-placeholder.jpg
            var nonexistentDirectory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"meioai-test-{Guid.NewGuid():N}");

            // act
            var result = GameInputHelper.EnsureImageOrDefault(null!, nonexistentDirectory);

            // assert
            result.Should().BeEmpty();
        }
    }
}
