using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Property_and_Management.Src.Interface;

namespace Property_and_Management.Src.Service
{
    public class FileDismissedNotificationStore : IDismissedNotificationStore
    {
        private const int InvalidNotificationId = -1;
        private const int MinimumValidNotificationId = 0;
        private const string ApplicationFolderName = "BoardRent";
        private const string DismissedFilePrefix = "dismissed-notifications-user-";
        private const string DismissedFileSuffix = ".txt";
        private const char TokenSeparator = ',';

        public HashSet<int> Load(int userId)
        {
            var path = GetStoragePath(userId);
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
                .Select(token => int.TryParse(token, out var parsedId) ? parsedId : InvalidNotificationId)
                .Where(notificationId => notificationId > MinimumValidNotificationId)
                .ToHashSet();
        }

        public void Save(int currentUserId, IEnumerable<int> dismissedNotificationIdentifiers)
        {
            if (dismissedNotificationIdentifiers == null)
            {
                throw new ArgumentNullException(nameof(dismissedNotificationIdentifiers));
            }

            var path = GetStoragePath(currentUserId);
            var serialized = string.Join(TokenSeparator, dismissedNotificationIdentifiers.OrderBy(notificationId => notificationId));
            File.WriteAllText(path, serialized);
        }

        private static string GetStoragePath(int userId)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationFolderName);
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, $"{DismissedFilePrefix}{userId}{DismissedFileSuffix}");
        }
    }
}
