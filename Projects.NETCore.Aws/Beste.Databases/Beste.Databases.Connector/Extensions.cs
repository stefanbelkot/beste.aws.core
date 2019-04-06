using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Beste.Databases.Connector
{
    internal static class Extensions
    {
        internal static void WriteAllToFile(this Stream stream, string destPath)
        {
            using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }

    }
}
