using System;
using System.Collections.Generic;
using System.Text;

namespace Beste.Rights
{
    public class PureRight
    {
        public virtual string RecourceModule { get; set; }
        public virtual int? RecourceId { get; set; }
        public virtual string Operation { get; set; }
        public virtual bool Authorized { get; set; }
    }
}
