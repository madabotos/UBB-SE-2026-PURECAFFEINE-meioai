namespace Property_and_Management.Src.Interface
{
    public enum CreateRequestError
    {
        OwnerCannotRent,
        DatesUnavailable,
        GameDoesNotExist
    }

    public enum ApproveRequestError
    {
        Unauthorized,
        NotFound,
        TransactionFailed
    }

    public enum DenyRequestError
    {
        Unauthorized,
        NotFound
    }

    public enum OfferError
    {
        NotFound,
        NotOwner,
        RequestNotOpen,
        TransactionFailed
    }

    public enum CancelRequestError
    {
        Unauthorized = -1,
        NotFound = -2
    }
}