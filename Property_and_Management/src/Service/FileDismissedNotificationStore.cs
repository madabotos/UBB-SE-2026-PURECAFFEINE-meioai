using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Service
{
    /// <summary>
    /// File-backed <see cref="IDismissedNotificationStore"/>. Stores one file per user
    /// under <c>%LOCALAPPDATA%/BoardRent/dismissed-notifications-user-{id}.txt</c>
    /// as a comma-separated list of integers. Matches the format previously inlined
    /// in the notifications view model so existing files stay readable.
    /// </summary>
    public class FileDismissedNotificationStore : IDismissedNotificationStore
    {
        private const int InvalidNotificationIdentifier = -1;
        private const int MinimumValidNotificationIdentifier = 0;
        private const string ApplicationFolderName = "BoardRent";
        private const string DismissedFilePrefix = "dismissed-notifications-user-";
        private const string DismissedFileSuffix = ".txt";
        private const char TokenSeparator = ',';

        public HashSet<int> Load(int userIdentifier)
        {
            var path = GetStoragePath(userIdentifier);
            if (!File.Exists(path))
            {
                return new HashSet<int>();
            }

            var serialized = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(serialized))
            {
                return new HashSet<int>();
            }

            return serialized
                .Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => int.TryParse(token, out var parsedIdentifier) ? parsedIdentifier : InvalidNotificationIdentifier)
                .Where(identifier => identifier > MinimumValidNotificationIdentifier)
                .ToHashSet();
        }

        public void Save(int userIdentifier, IEnumerable<int> dismissedIdentifiers)
        {
            if (dismissedIdentifiers == null)
            {
                throw new ArgumentNullException(nameof(dismissedIdentifiers));
            }

            var path = GetStoragePath(userIdentifier);
            var serialized = string.Join(TokenSeparator, dismissedIdentifiers.OrderBy(identifier => identifier));
            File.WriteAllText(path, serialized);
        }

        private static string GetStoragePath(int userIdentifier)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationFolderName);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"{DismissedFilePrefix}{userIdentifier}{DismissedFileSuffix}");
        }
    }
}
