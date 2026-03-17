using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Property_and_Management.src.Interface
{
    public interface IEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Creates a new IEntity given a dictionary of parameters.
        /// </summary>
        /// <param name="parameters">A dictionary containing key-value pairs of the variable names (column names) and actual values</param>
        /// <returns>A new IEntity instance from the given parameters</returns>
        abstract static IEntity BuildFromParameters(Dictionary<string, object> parameters);
    }
}
