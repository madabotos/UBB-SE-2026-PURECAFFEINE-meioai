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
            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().BeEmpty();
        }

        [Test]
        public void BuildValidationErrors_NameTooShort_ReportsNameLengthError()
        {
            var shortName = "abc";

            var errors = GameInputHelper.BuildValidationErrors(
                shortName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_NameNullOrWhitespace_ReportsNameLengthError()
        {
            var whitespaceName = "   ";

            var errors = GameInputHelper.BuildValidationErrors(
                whitespaceName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_NameTooLong_ReportsNameLengthError()
        {
            var overLongName = new string('x', MaximumNameLength + 1);

            var errors = GameInputHelper.BuildValidationErrors(
                overLongName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("Name"));
        }

        [Test]
        public void BuildValidationErrors_PriceBelowMinimum_ReportsPriceError()
        {
            var belowMinimumPrice = 0m;

            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, belowMinimumPrice, ValidMinPlayers, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("Price"));
        }

        [Test]
        public void BuildValidationErrors_MinimumPlayersBelowMinimum_ReportsPlayerCountError()
        {
            var belowMinimumPlayerCount = 0;

            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, belowMinimumPlayerCount, ValidMaxPlayers, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("player", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void BuildValidationErrors_MaximumPlayersBelowMinimum_ReportsPlayerOrderError()
        {
            var outOfOrderMin = 4;
            var outOfOrderMax = 2;

            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, outOfOrderMin, outOfOrderMax, ValidDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("player", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public void BuildValidationErrors_DescriptionTooShort_ReportsDescriptionError()
        {
            var shortDescription = "short";

            var errors = GameInputHelper.BuildValidationErrors(
                ValidName, ValidPrice, ValidMinPlayers, ValidMaxPlayers, shortDescription,
                MinimumNameLength, MaximumNameLength, MinimumAllowedPrice, MinimumPlayerCount,
                MinimumDescriptionLength, MaximumDescriptionLength);

            errors.Should().Contain(message => message.Contains("Description"));
        }

        [Test]
        public void EnsureImageOrDefault_ImageProvided_ReturnsSameImage()
        {
            var providedImageBytes = new byte[] { 0x01, 0x02, 0x03 };

            var result = GameInputHelper.EnsureImageOrDefault(providedImageBytes, baseDirectory: ".");

            result.Should().BeSameAs(providedImageBytes);
        }

        [Test]
        public void EnsureImageOrDefault_NullImageMissingDefault_ReturnsEmptyArray()
        {
            var nonexistentDirectory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"meioai-test-{Guid.NewGuid():N}");

            var result = GameInputHelper.EnsureImageOrDefault(null!, nonexistentDirectory);

            result.Should().BeEmpty();
        }
    }
}