using System;
using System.Collections.Generic;

namespace Property_and_Management.Src.Viewmodels
{
    internal static class GameInputHelper
    {
        private const int EmptyImageLength = 0;

        public static List<string> BuildValidationErrors(
            string name,
            decimal price,
            int minPlayers,
            int maxPlayers,
            string description,
            int minimumNameLength,
            int maximumNameLength,
            decimal minimumAllowedPrice,
            int minimumPlayerCount,
            int minimumDescriptionLength,
            int maximumDescriptionLength)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(name) || name.Length < minimumNameLength || name.Length > maximumNameLength)
            {
                errors.Add(Constants.ValidationMessages.NameLengthRange(minimumNameLength, maximumNameLength));
            }

            if (price < minimumAllowedPrice)
            {
                errors.Add(Constants.ValidationMessages.PriceMinimum(minimumAllowedPrice));
            }

            if (minPlayers < minimumPlayerCount)
            {
                errors.Add(Constants.ValidationMessages.MinimumPlayerCount(minimumPlayerCount));
            }

            if (maxPlayers < minPlayers)
            {
                errors.Add(Constants.ValidationMessages.MaximumPlayerCountComparedToMinimum);
            }

            if (string.IsNullOrWhiteSpace(description) || description.Length < minimumDescriptionLength || description.Length > maximumDescriptionLength)
            {
                errors.Add(Constants.ValidationMessages.DescriptionLengthRange(minimumDescriptionLength, maximumDescriptionLength));
            }

            return errors;
        }

        public static byte[] EnsureImageOrDefault(byte[] image, string baseDirectory)
        {
            if (image != null && image.Length > EmptyImageLength)
            {
                return image;
            }

            try
            {
                string defaultImagePath = System.IO.Path.Combine(baseDirectory, "Assets", "default-game-placeholder.jpg");
                return System.IO.File.ReadAllBytes(defaultImagePath);
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}