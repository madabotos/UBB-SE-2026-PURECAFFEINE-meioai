// Expose internal types (e.g. GameInputHelper, DateRangeValidationHelper,
// PriceInputParser) to the test project so the helpers can keep their
// "internal static" accessibility in production code while still being
// exercised by Osherove-style unit tests.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Property_and_Management.Tests")]
