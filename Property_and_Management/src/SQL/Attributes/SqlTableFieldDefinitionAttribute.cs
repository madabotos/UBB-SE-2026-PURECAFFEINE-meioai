using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Property_and_Management.src.Attributes
{
    /// <summary>
    /// An attribute for specifying the name of an SQL table field
    /// </summary>
    /// <param name="fieldName">The name of the field</param>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlTableFieldDefinitionAttribute(string fieldName) : Attribute
    {
        public string FieldName { get; } = fieldName;
        public bool IsPrimaryKey { get; set; } = false;

        private string _variableName = fieldName;
        public string VariableName
        {
            get { return "@" + _variableName; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _variableName = value;
            }
        }

    }
}
