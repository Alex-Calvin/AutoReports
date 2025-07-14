using System.Collections.Generic;
using System.Dynamic;

namespace AutoReportGenerator
{
    public class DynamicDataObject
    {
        public dynamic Instance { get; } = new ExpandoObject();

        public void AddProperty(string name, object value)
        {
            if (string.IsNullOrEmpty(name) || name.ToUpperInvariant().Contains("HIDDEN"))
                return;

            ((IDictionary<string, object>)Instance)[name.ToUpperInvariant()] = value ?? System.DBNull.Value;
        }

        public dynamic GetProperty(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var key = name.ToUpperInvariant();
            return ((IDictionary<string, object>)Instance).ContainsKey(key) 
                ? ((IDictionary<string, object>)Instance)[key] 
                : null;
        }
    }
} 