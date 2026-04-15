using System.Globalization;

namespace Property_and_Management.Src.Viewmodels
{
    internal static class PriceInputParser
    {
        private const double InvalidParsedPrice = 0;

        public static bool TryParsePriceInput(string input, out double parsedPrice)
        {
            parsedPrice = InvalidParsedPrice;
            var priceText = input?.Trim();

            if (string.IsNullOrWhiteSpace(priceText))
            {
                return false;
            }

            return double.TryParse(priceText, NumberStyles.Float, CultureInfo.CurrentCulture, out parsedPrice) ||
                   double.TryParse(priceText, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedPrice);
        }
    }
}