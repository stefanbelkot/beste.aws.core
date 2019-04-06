namespace Beste.Rights
{
    public interface IAccessControlList
    {
        bool IsGranted(string[] principals, string operation, string resource);
        void Grant(string principal, string operation, string resource);
        void Revoke(string principal, string operation, string resource);

        bool IsDenied(string[] principals, string operation, string resource);
        void Deny(string principal, string operation, string resource);

        string Explain(string[] principals, string operation, string resource);
    }
}