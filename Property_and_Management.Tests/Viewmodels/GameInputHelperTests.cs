using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class GameInputHelperTests
    {
        [Test]
        public void BuildValidationErrors_ValidInput()
        {
            // set up some valid parameters
            var gameName = "Catan";
            var gamePrice = 19.99m;
            var minimumPlayerCount = 3;
            var maximumPlayerCount = 4;
            var gameDescription = "Colonize the island";
            var minimumNameLength = 3;
            var maximumNameLength = 50;
            var minimumAllowedPrice = 0.01m;
            var absoluteMinimumPlayerCount = 2;
            var minimumDescriptionLength = 10;
            var maximumDescriptionLength = 200;
            
            // run the method
            var validationErrors = GameInputHelper.BuildValidationErrors(
                gameName,
                gamePrice,
                minimumPlayerCount,
                maximumPlayerCount,
                gameDescription,
                minimumNameLength,
                maximumNameLength,
                minimumAllowedPrice,
                absoluteMinimumPlayerCount,
                minimumDescriptionLength,
                maximumDescriptionLength);

            // assert
            Assert.That(validationErrors, Is.Empty);
        }

        [Test]
        public void BuildValidationErrors_InvalidInput1()
        {
            // set up some parameters
            var gameName = "Saboteur";
            var gamePrice = 2.0m;
            var minimumPlayerCount = 2;
            var maximumPlayerCount = 12;
            var gameDescription = "Find the gold";
            var minimumNameLength = 3;
            var maximumNameLength = 50;
            var minimumAllowedPrice = 20.01m;
            var absoluteMinimumPlayerCount = 2;
            var minimumDescriptionLength = 100;
            var maximumDescriptionLength = 200;

            // run the method
            var validationErrors = GameInputHelper.BuildValidationErrors(
                gameName,
                gamePrice,
                minimumPlayerCount,
                maximumPlayerCount,
                gameDescription,
                minimumNameLength,
                maximumNameLength,
                minimumAllowedPrice,
                absoluteMinimumPlayerCount,
                minimumDescriptionLength,
                maximumDescriptionLength);

            // assert
            Assert.That(validationErrors, Does.Contain(Constants.ValidationMessages.PriceMinimum(minimumAllowedPrice)));
            Assert.That(validationErrors, Does.Contain(Constants.ValidationMessages.DescriptionLengthRange(minimumDescriptionLength, maximumDescriptionLength)));
        }

        [Test]
        public void BuildValidationErrors_InvalidInput2()
        {
            // set up some parameters
            var gameName = "";
            var gamePrice = 30.0m;
            var minimumPlayerCount = 11;
            var maximumPlayerCount = 10;
            var gameDescription = "Find the gold";
            var minimumNameLength = 3;
            var maximumNameLength = 50;
            var minimumAllowedPrice = 20.01m;
            var absoluteMinimumPlayerCount = 20;
            var minimumDescriptionLength = 1;
            var maximumDescriptionLength = 200;

            // run the method
            var validationErrors = GameInputHelper.BuildValidationErrors(
                gameName,
                gamePrice,
                minimumPlayerCount,
                maximumPlayerCount,
                gameDescription,
                minimumNameLength,
                maximumNameLength,
                minimumAllowedPrice,
                absoluteMinimumPlayerCount,
                minimumDescriptionLength,
                maximumDescriptionLength);

            // assert
            Assert.That(validationErrors, Does.Contain(Constants.ValidationMessages.NameLengthRange(minimumNameLength, maximumNameLength)));
            Assert.That(validationErrors, Does.Contain(Constants.ValidationMessages.MinimumPlayerCount(absoluteMinimumPlayerCount)));
            Assert.That(validationErrors, Does.Contain(Constants.ValidationMessages.MaximumPlayerCountComparedToMinimum));
        }

        [Test]
        public void EnsureImageOrDefault_WithValidImage_ReturnsSameImage()
        {
            var gameImage = new byte[] { 1, 2, 3 };
            var baseDir = "SomeDirectory";

            var result = GameInputHelper.EnsureImageOrDefault(gameImage, baseDir);

            Assert.That(result, Is.SameAs(gameImage));
        }

        [Test]
        public void EnsureImageOrDefault_WithEmptyImageAndInvalidPath_ReturnsEmptyArray()
        {
            var gameImage = Array.Empty<byte>();
            var baseDir = "InvalidDirectory123456789";

            var result = GameInputHelper.EnsureImageOrDefault(gameImage, baseDir);

            Assert.That(result, Is.Empty);
        }
    }
}
