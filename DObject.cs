using System.Collections.Generic;
using System.Dynamic;

namespace LSUF.AutoReports
{
    public class DObject
    {
        public dynamic Instance = new ExpandoObject();

        public void AddProperty(string name, object value)
        {
            if (!name.ToUpper().Contains("HIDDEN"))
            {
                ((IDictionary<string, object>)Instance).Add(name.ToUpper(), value);
            }
        }

        public dynamic GetProperty(string name)
        {
            if (((IDictionary<string, object>)Instance).ContainsKey(name))
                return ((IDictionary<string, object>)Instance)[name];
            else
                return null;
        }
    }
}