using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Property_and_Management.src.Interface
{
    public interface IDTO<T> where T : IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        int Id { get; set; }
    }
}
