using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    // Tests the PagedViewModel<T> base class that Agent 2 extracts.
    // A local test double exposes the abstract hook so tests can drive it.
    [TestFixture]
    public sealed class PagedViewModelTests
    {
        private const int DefaultPageSize = 3;

        [Test]
        public void NextPage_AtLastPage_DoesNothing()
        {
            // arrange
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 3));

            // act
            pagedViewModel.NextPage();

            // assert
            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PrevPage_AtFirstPage_DoesNothing()
        {
            // arrange
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9));

            // act
            pagedViewModel.PrevPage();

            // assert
            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PrevPage_AtMiddlePage_DecrementsByOne()
        {
            // arrange
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9))
            {
                CurrentPage = 2,
            };

            // act
            pagedViewModel.PrevPage();

            // assert
            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PageCount_EmptyList_IsOne()
        {
            // arrange
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 0));

            // act
            var pageCount = pagedViewModel.PageCount;

            // assert
            pageCount.Should().Be(1);
        }

        [Test]
        public void PageCount_FullPages_IsCorrect()
        {
            // arrange — 9 items at page size 3 → 3 pages
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9));

            // act
            var pageCount = pagedViewModel.PageCount;

            // assert
            pageCount.Should().Be(3);
        }

        [Test]
        public void PageCount_PartialLastPage_RoundsUp()
        {
            // arrange — 10 items at page size 3 → 4 pages
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 10));

            // act
            var pageCount = pagedViewModel.PageCount;

            // assert
            pageCount.Should().Be(4);
        }

        [Test]
        public void Reload_RepopulatesPagedItemsForCurrentPage()
        {
            // arrange
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9))
            {
                CurrentPage = 1,
            };

            // act
            pagedViewModel.InvokeReload();

            // assert
            pagedViewModel.PagedItems.Should().HaveCount(DefaultPageSize);
        }

        private static ImmutableList<string> BuildItems(int count)
        {
            var builder = ImmutableList.CreateBuilder<string>();
            for (var index = 0; index < count; index++)
            {
                builder.Add($"item-{index}");
            }

            return builder.ToImmutable();
        }

        // Minimal test double over PagedViewModel<T>. The base class's
        // abstract hook is Reload(); the concrete implementation publishes
        // the underlying data via SetAllItems(ImmutableList<T>).
        private sealed class FakePagedViewModel : PagedViewModel<string>
        {
            private readonly ImmutableList<string> sourceItems;

            public FakePagedViewModel(ImmutableList<string> sourceItems)
            {
                this.sourceItems = sourceItems;
                Reload();
            }

            public void InvokeReload() => Reload();

            protected override void Reload() => SetAllItems(sourceItems);
        }
    }
}
