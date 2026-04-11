using System;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class DateRangeValidationHelperTests
    {
        [Test]
        public void HasValidFutureDateRange_NullStart_ReturnsFalse()
        {
            // arrange
            DateTimeOffset? startDate = null;
            DateTimeOffset? endDate = DateTimeOffset.Now.AddDays(2);

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(startDate, endDate);

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void HasValidFutureDateRange_NullEnd_ReturnsFalse()
        {
            // arrange
            DateTimeOffset? startDate = DateTimeOffset.Now.AddDays(1);
            DateTimeOffset? endDate = null;

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(startDate, endDate);

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void HasValidFutureDateRange_StartAfterEnd_ReturnsFalse()
        {
            // arrange
            DateTimeOffset? startDate = DateTimeOffset.Now.AddDays(5);
            DateTimeOffset? endDate = DateTimeOffset.Now.AddDays(2);

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(startDate, endDate);

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void HasValidFutureDateRange_StartEqualsEnd_ReturnsFalse()
        {
            // arrange — helper requires strictly increasing range
            var sameDate = DateTimeOffset.Now.AddDays(2);

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(sameDate, sameDate);

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void HasValidFutureDateRange_StartInThePast_ReturnsFalse()
        {
            // arrange
            DateTimeOffset? startDate = DateTimeOffset.Now.AddDays(-1);
            DateTimeOffset? endDate = DateTimeOffset.Now.AddDays(2);

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(startDate, endDate);

            // assert
            isValid.Should().BeFalse();
        }

        [Test]
        public void HasValidFutureDateRange_FutureRange_ReturnsTrue()
        {
            // arrange
            DateTimeOffset? startDate = DateTimeOffset.Now.AddDays(1);
            DateTimeOffset? endDate = DateTimeOffset.Now.AddDays(3);

            // act
            var isValid = DateRangeValidationHelper.HasValidFutureDateRange(startDate, endDate);

            // assert
            isValid.Should().BeTrue();
        }
    }
}
