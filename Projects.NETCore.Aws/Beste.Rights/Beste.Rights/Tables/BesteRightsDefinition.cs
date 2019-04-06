using System;
using System.Text;
using System.Collections.Generic;


namespace Beste.Rights {
    
    public class BesteRightsDefinition {
        public BesteRightsDefinition() { }
        public virtual int Id { get; set; }
        public virtual BesteRightsNamespace BesteRightsNamespace { get; set; }
        public virtual string RecourceModule { get; set; }
        public virtual int? RecourceId { get; set; }
        public virtual string Operation { get; set; }
    }
}
