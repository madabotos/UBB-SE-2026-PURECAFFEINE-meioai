using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Property_and_Management.src.Attributes
{
    /// <summary>
    /// An attribute for specifying the name of an SQL table
    /// </summary>
    /// <param name="tableName">The name of the table</param>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    class SqlTableDefinitionAttribute(string tableName) : Attribute
    {
        public string TableName { get; } = tableName;
    }
}
