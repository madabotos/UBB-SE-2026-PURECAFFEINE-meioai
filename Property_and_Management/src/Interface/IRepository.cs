using System.Collections.Generic;
using System.Collections.Immutable;

namespace Property_and_Management.Src.Interface
{
    public interface IRepository<TEntity>
        where TEntity : notnull, IEntity
    {
        /// <summary>
        /// Retrieves all items in the collection as an immutable list.
        /// </summary>
        /// <returns>An <see cref="ImmutableList{TEntity}"/> of all items; empty if none exist.</returns>
        ImmutableList<TEntity> GetAll();

        /// <summary>
        /// Adds the specified entity to the collection.
        /// </summary>
        /// <param name="newEntity">The entity to add. Cannot be null.</param>
        void Add(TEntity newEntity);

        /// <summary>
        /// Removes and returns the entity with the specified identifier from the repository.
        /// </summary>
        /// <param name="removedEntityIdentifier">The identifier of the entity to remove.</param>
        /// <returns>The removed entity instance.</returns>
        TEntity Delete(int removedEntityIdentifier);

        /// <summary>
        /// Replaces the entity with the specified identifier with the provided new entity.
        /// </summary>
        /// <param name="updatedEntityIdentifier">The identifier of the entity to update.</param>
        /// <param name="newEntity">The new entity data that will replace the existing entity.</param>
        void Update(int updatedEntityIdentifier, TEntity newEntity);

        /// <summary>
        /// Retrieves the entity with the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the entity to retrieve.</param>
        /// <returns>The entity matching the specified <paramref name="identifier"/>.</returns>
        /// <remarks>
        /// If no entity with the given identifier exists, implementations may throw <see cref="KeyNotFoundException"/>.
        /// </remarks>
        TEntity Get(int identifier);
    }
}


