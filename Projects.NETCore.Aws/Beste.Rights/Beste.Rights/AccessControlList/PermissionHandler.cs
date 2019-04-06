using System;

namespace Beste.Rights
{
    /// <summary>
    /// This class can be used to implement an Observer pattern on ACL permission checks.
    /// </summary>
    public class AccessControlListEventArgs : EventArgs
    {
        public string Principal { get; set; }
        public string Operation { get; set; }
        public string Resource { get; set; }
    }
}