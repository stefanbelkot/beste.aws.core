using System;
using System.Text;
using System.Collections.Generic;


namespace Beste.Rights {
    
    public class BesteRightsAuthorization {
        public virtual BesteRightsDefinition BesteRightsDefinition { get; set; }
        public virtual int Id { get; set; }
        public virtual int LegitimationId { get; set; }
        public virtual bool Authorized { get; set; }
    }
}
