namespace Property_and_Management.src.Interface
{
    public interface IDTO<TEntity> where TEntity : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        int Id { get; set; }
    }
}
