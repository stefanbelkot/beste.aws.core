using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;

namespace Beste.Rights
{
    public class BaseControlItem
    {
        /// <summary>
        /// Store a collection of principals for each operation, using the operation as a hash key.
        /// </summary>
        private readonly Dictionary<string, StringCollection> _principals;

        /// <summary>
        /// Disallow a null dictionary.
        /// </summary>
        public BaseControlItem() { _principals = new Dictionary<string, StringCollection>(); }

        /// <summary>
        /// Returns true if the dictionary contains a collection of principals for the operation; false otherwise.
        /// </summary>
        public bool Contains(string operation)
        {
            var key = StringHelper.Sanitize(operation);
            return _principals.ContainsKey(key);
        }

        /// <summary>
        /// Removes a principal from an operation.
        /// </summary>
        public void Exclude(string principal, string operation)
        {
            principal = StringHelper.Sanitize(principal);
            if (principal == null)
                throw new ArgumentNullException();

            var value = GetValue(operation);
            if (value.Contains(principal))
                value.Remove(principal);
        }

        /// <summary>
        /// Returns only those principals already added to an operation. Given a list of principals, you might want to
        /// know which one(s) have been included for a specific operation.
        /// </summary>
        public CommaDelimitedStringCollection FindIncludedPrincipals(string[] principals, string operation)
        {
            var includedPrincipals = new CommaDelimitedStringCollection();

            var key = StringHelper.Sanitize(operation);

            if (key == null || !_principals.ContainsKey(key))
                return includedPrincipals;

            var value = _principals[key];
            foreach (var principal in principals)
            {
                var p = StringHelper.Sanitize(principal);
                if (value.Contains(p))
                    includedPrincipals.Add(p);
            }

            return includedPrincipals;
        }

        /// <summary>
        /// Adds a principal to an operation.
        /// </summary>
        public void Include(string principal, string operation)
        {
            principal = StringHelper.Sanitize(principal);
            if (principal == null)
                throw new ArgumentNullException();

            var value = GetValue(operation);
            if (!value.Contains(principal))
                value.Add(principal);
        }

        /// <summary>
        /// Returns true if any one of the principals has been included for the operation.
        /// </summary>
        public bool IsIncluded(string[] principals, string operation)
        {
            var key = StringHelper.Sanitize(operation);

            if (key == null)
                return false;

            var value = !_principals.ContainsKey(key) ? null : _principals[key];
            return value != null && principals.Any(principal => value.Contains(StringHelper.Sanitize(principal)));
        }

        /// <summary>
        /// Returns the collection of principals corresponding to the operation.
        /// </summary>
        private StringCollection GetValue(string operation)
        {
            var key = StringHelper.Sanitize(operation);
            if (key == null)
                throw new ArgumentNullException();

            StringCollection value;
            if (!_principals.ContainsKey(key))
            {
                value = new StringCollection();
                _principals.Add(key, value);
            }
            else
            {
                value = _principals[key];
            }

            return value;
        }
    }
}