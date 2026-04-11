namespace Property_and_Management.Src.Constants
{
    /// <summary>
    /// Cross-layer domain constants. Values placed here are shared between the
    /// service and repository layers and must stay in a single source of truth.
    /// </summary>
    public static class DomainConstants
    {
        /// <summary>
        /// Mandatory buffer, in hours, between two rentals of the same game
        /// (applied symmetrically on both sides of the rental window).
        /// </summary>
        public const int RentalBufferHours = 48;
    }
}
