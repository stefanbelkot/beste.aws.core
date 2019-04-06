using System;
using System.Text;
using System.Collections.Generic;


namespace Beste.Rights {
    
    public class BesteRightsToken {
        public virtual BesteRightsNamespace BesteRightsNamespace { get; set; }
        public virtual int Id { get; set; }
        public virtual string Token { get; set; }
        public virtual int LegitimationId { get; set; }
        public virtual DateTime Ends { get; set; }
    }
}
