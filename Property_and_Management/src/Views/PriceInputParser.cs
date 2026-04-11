using System.Globalization;

namespace Property_and_Management.src.Views
{
    internal static class PriceInputParser
    {
        public static bool TryParsePriceInput(string input, out double parsedPrice)
        {
            parsedPrice = 0;
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
