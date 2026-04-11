namespace Property_and_Management.src.Interface
{
    public interface IMapper<TEntity, TDataTransferObject>
        where TEntity : IEntity
        where TDataTransferObject : IDataTransferObject<TEntity>
    {
        /// <summary>
        /// Method for creating a Data Transfer Object from a model
        /// </summary>
        /// <param name="model"></param>
        /// <returns>The newly created Data Transfer Object</returns>
        TDataTransferObject ToDataTransferObject(TEntity entity);

        /// <summary>
        /// Converts the current instance to a model of type <typeparamref name="TEntity"/>.
        /// </summary>
        /// <returns>An object of type <typeparamref name="TEntity"/> representing the model equivalent of the current instance.</returns>
        TEntity ToModel(TDataTransferObject dataTransferObject);
    }
}
