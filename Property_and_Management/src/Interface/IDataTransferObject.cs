namespace Property_and_Management.Src.Interface
{
    public interface IDataTransferObject<TEntity>
        where TEntity : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        int Identifier { get; set; }
    }
}
