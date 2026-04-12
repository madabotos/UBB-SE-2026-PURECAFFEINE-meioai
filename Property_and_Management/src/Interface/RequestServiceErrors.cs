namespace Property_and_Management.Src.Interface
{
    /// <summary>
    /// Failure modes returned by <see cref="IRequestService.CreateRequest"/>.
    /// </summary>
    public enum CreateRequestError
    {
        OwnerCannotRent,
        DatesUnavailable,
        GameDoesNotExist
    }

    /// <summary>
    /// Failure modes returned by <see cref="IRequestService.ApproveRequest"/>.
    /// </summary>
    public enum ApproveRequestError
    {
        Unauthorized,
        NotFound,
        TransactionFailed
    }

    /// <summary>
    /// Failure modes returned by <see cref="IRequestService.DenyRequest"/>.
    /// </summary>
    public enum DenyRequestError
    {
        Unauthorized,
        NotFound
    }

    /// <summary>
    /// Failure modes returned by <see cref="IRequestService.OfferGame"/>.
    /// </summary>
    public enum OfferError
    {
        NotFound,
        NotOwner,
        RequestNotOpen,
        TransactionFailed
    }

    /// <summary>
    /// Failure modes returned by <see cref="IRequestService.CancelRequest"/>.
    /// Consolidated here with the other request-service error enums even though
    /// CancelRequest keeps its int return shape for this iteration. Explicit
    /// negative values preserve the existing sentinel-code contract.
    /// </summary>
    public enum CancelRequestError
    {
        Unauthorized = -1,
        NotFound = -2
    }
}
