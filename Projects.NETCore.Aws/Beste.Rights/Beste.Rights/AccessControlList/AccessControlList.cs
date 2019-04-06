namespace Beste.Rights
{
/// <summary>
/// This class provides a simple and basic implementation for the required ACL interface.
/// </summary>
public class AccessControlList : IAccessControlList
{
    // We can use the same base control class to store and query for permissions granted and denied.
    protected readonly BaseControlList _granted;
    protected readonly BaseControlList _denied;

    /// <summary>
    /// Disallow null ACLs.
    /// </summary>
    public AccessControlList()
    {
        _granted = new BaseControlList();
        _denied = new BaseControlList();
    }

    /// <summary>
    /// Explain why the operation is granted or denied on the resource, given a collection of principals.
    /// </summary>
    public string Explain(string[] principals, string operation, string resource)
    {
        // Check for an overriding denial.
        if (_denied.IsIncluded(principals, operation, resource))
        {
            var included = _denied.FindIncludedPrincipals(principals, operation, resource);
            return string.Format("Access to operation {1} is explicitly denied to {0} on resource {2}.", included, operation, resource);
        }

        // Grant access only if there is a matching access control entry.
        if (_granted.IsIncluded(principals, operation, resource))
        {
            var included = _granted.FindIncludedPrincipals(principals, operation, resource);
            return string.Format("Access to operation {1} is granted to {0} on resource {2}.", included, operation, resource);
        } ;

        // Assume every permission is denied by default.
        return "Permission is not granted to any of the user roles specified.";
    }

    /// <summary>
    /// Returns true if any of the principals is granted the operation on the resource.
    /// </summary>
    public bool IsGranted(string[] principals, string operation, string resource)
    {
        // Assume every permission is denied by default.
        bool result = false;

        // Check for an overriding denial.
        if (!_denied.IsIncluded(principals, operation, resource))
        {
            // Grant access only if there is an explicit access control rule.
            if (_granted.IsIncluded(principals, operation, resource))
                result = true;
        }

        OnGrantChecked(operation, resource, result);

        return result;
    }

    /// <summary>
    /// Adds a permission to the ACL.
    /// </summary>
    public void Grant(string principal, string operation, string resource)
    {
        _granted.Include(principal,operation,resource);
    }

    /// <summary>
    /// Removes a permission from the ACL.
    /// </summary>
    public void Revoke(string principal, string operation, string resource)
    {
        _granted.Exclude(principal, operation, resource);
    }

    /// <summary>
    /// Returns true if any of the principals is explicitly denied the operation on the resource.
    /// </summary>
    public bool IsDenied(string[] principals, string operation, string resource)
    {
        return _denied.IsIncluded(principals, operation, resource);
    }

    /// <summary>
    /// Adds an overriding permission denial to the ACL.
    /// </summary>
    public void Deny(string principal, string operation, string resource)
    {
        _denied.Include(principal, operation, resource);
    }

    #region Events and Delegates

    /// <summary>
    /// An observer might want to observer ACL lookups and track their results to maintain a security audit trail.
    /// </summary>
    public delegate void GrantCheckHandler(string operation, string resource, bool result);
    public event GrantCheckHandler GrantCheck;
    protected virtual void OnGrantChecked(string operation, string resource, bool result)
    {
        GrantCheck?.Invoke(operation, resource, result);
    }

    #endregion
}
}