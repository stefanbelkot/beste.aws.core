using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;

namespace Beste.Rights
{
    public class BaseControlList
    {
        /// <summary>
        /// Store a collection of base control items for each resource, using the resource as a hash key.
        /// </summary>
        private readonly Dictionary<string, BaseControlItem> _operations;

        /// <summary>
        /// Disallow a null dictionary.
        /// </summary>
        public BaseControlList() { _operations = new Dictionary<string, BaseControlItem>(); }

        /// <summary>
        /// Returns true if the dictionary contains a collection of operations for the resource; false otherwise.
        /// </summary>
        public bool Contains(string resource)
        {
            var key = StringHelper.Sanitize(resource);
            return _operations.ContainsKey(key);
        }

        /// <summary>
        /// Removes the principal from the operation on the resource.
        /// </summary>
        public void Exclude(string principal, string operation, string resource)
        {
            principal = StringHelper.Sanitize(principal);
            operation = StringHelper.Sanitize(operation);
            var value = GetValue(resource);
            value.Exclude(principal, operation);
        }

        /// <summary>
        /// Returns only those principals already added to the operation on the resource. Given a list of principals, 
        /// you might want to know which one(s) have been included for a specific operation on a specific resource.
        /// </summary>
        public CommaDelimitedStringCollection FindIncludedPrincipals(string[] principals, string operation, string resource)
        {
            var value = GetValue(resource);
            return value.FindIncludedPrincipals(principals, operation);
        }

        /// <summary>
        /// Adds the principal to the operation on the resource.
        /// </summary>
        public void Include(string principal, string operation, string resource)
        {
            principal = StringHelper.Sanitize(principal);
            operation = StringHelper.Sanitize(operation);
            var value = GetValue(resource);
            value.Include(principal, operation);
        }

        /// <summary>
        /// Returns true if any one of the principals has been included for the operation on the resource.
        /// </summary>
        public bool IsIncluded(string[] principals, string operation, string resource)
        {
            operation = StringHelper.Sanitize(operation);
            var key = StringHelper.Sanitize(resource);

            if (key == null)
                return false;

            var value = !_operations.ContainsKey(key) ? null : _operations[key];
            return value != null && value.IsIncluded(principals, operation);
        }

        /// <summary>
        /// Returns the collection of operations corresponding to the resource.
        /// </summary>
        private BaseControlItem GetValue(string resource)
        {
            var key = StringHelper.Sanitize(resource);

            BaseControlItem value;
            if (!_operations.ContainsKey(key))
            {
                value = new BaseControlItem();
                _operations.Add(key, value);
            }
            else
            {
                value = _operations[key];
            }

            return value;
        }
    }
}