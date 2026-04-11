using System.Collections.Generic;

namespace Property_and_Management.Src.Interface
{
    /// <summary>
    /// Per-user persistent store for locally dismissed notification identifiers.
    /// Dismissing is a UI concern (the row is hidden on the current machine) and
    /// must not touch the Notifications table — the DB keeps a full history.
    /// </summary>
    public interface IDismissedNotificationStore
    {
        /// <summary>
        /// Returns the set of notification identifiers the user has dismissed on
        /// this machine, or an empty set if none / the store is missing.
        /// </summary>
        HashSet<int> Load(int userIdentifier);

        /// <summary>
        /// Replaces the persisted dismissed set for <paramref name="userIdentifier"/>
        /// with <paramref name="dismissedIdentifiers"/>.
        /// </summary>
        void Save(int userIdentifier, IEnumerable<int> dismissedIdentifiers);
    }
}
