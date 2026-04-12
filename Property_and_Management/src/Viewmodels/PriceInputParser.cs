using System.Globalization;

namespace Property_and_Management.Src.Viewmodels
{
    /// <summary>
    /// Small helper used by CreateGameViewModel and EditGameViewModel to turn
    /// the raw text from a <c>NumberBox</c> into a double. Accepts both current
    /// culture and invariant-culture number formats so decimal separators are
    /// not locale-fragile.
    ///
    /// Lives in the Viewmodels namespace so the ViewModel layer does not
    /// depend on Views.
    /// </summary>
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
