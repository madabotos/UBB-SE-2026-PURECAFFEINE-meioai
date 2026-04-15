using System.Collections.Immutable;
using FluentAssertions;
using NUnit.Framework;
using Property_and_Management.Src.Viewmodels;

namespace Property_and_Management.Tests.Viewmodels
{
    [TestFixture]
    public sealed class PagedViewModelTests
    {
        private const int DefaultPageSize = 3;

        [Test]
        public void NextPage_AtLastPage_DoesNothing()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 3));

            pagedViewModel.NextPage();

            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PrevPage_AtFirstPage_DoesNothing()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9));

            pagedViewModel.PrevPage();

            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PrevPage_AtMiddlePage_DecrementsByOne()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9))
            {
                CurrentPage = 2,
            };

            pagedViewModel.PrevPage();

            pagedViewModel.CurrentPage.Should().Be(1);
        }

        [Test]
        public void PageCount_EmptyList_IsOne()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 0));

            var pageCount = pagedViewModel.PageCount;

            pageCount.Should().Be(1);
        }

        [Test]
        public void PageCount_FullPages_IsCorrect()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9));

            var pageCount = pagedViewModel.PageCount;

            pageCount.Should().Be(3);
        }

        [Test]
        public void PageCount_PartialLastPage_RoundsUp()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 10));

            var pageCount = pagedViewModel.PageCount;

            pageCount.Should().Be(4);
        }

        [Test]
        public void Reload_RepopulatesPagedItemsForCurrentPage()
        {
            var pagedViewModel = new FakePagedViewModel(BuildItems(count: 9))
            {
                CurrentPage = 1,
            };

            pagedViewModel.InvokeReload();

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