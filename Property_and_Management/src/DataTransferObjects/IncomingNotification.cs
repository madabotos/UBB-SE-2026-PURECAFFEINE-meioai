using System;

namespace Property_and_Management.src.DataTransferObjects
{
    public sealed class IncomingNotification
    {
        public int UserIdentifier { get; init; }
        public DateTime Timestamp { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
    }
}

