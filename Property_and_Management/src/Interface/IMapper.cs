namespace Property_and_Management.src.Interface
{
    public interface IMapper<TEntity, TDataTransferObject>
        where TEntity : IEntity
        where TDataTransferObject : IDTO<TEntity>
    {
        /// <summary>
        /// Method for creating a DTO from a model
        /// </summary>
        /// <param name="model"></param>
        /// <returns>The newly created DTO</returns>
        TDataTransferObject ToDTO(TEntity entity);

        /// <summary>
        /// Converts the current instance to a model of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <returns>An object of type <typeparamref name="TEntity"/> representing the model equivalent of the current instance.</returns>
        TEntity ToModel(TDataTransferObject dataTransferObject);
    }
}
