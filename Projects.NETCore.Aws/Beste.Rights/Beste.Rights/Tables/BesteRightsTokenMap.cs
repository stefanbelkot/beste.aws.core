using System; 
using System.Collections.Generic; 
using System.Text; 
using FluentNHibernate.Mapping;
using Beste.Rights; 

namespace Beste.Rights {
    
    
    public class BesteRightsTokenMap : ClassMap<BesteRightsToken> {
        
        public BesteRightsTokenMap() {
			Table("beste_rights_token");
			LazyLoad();
            Id(x => x.Id).GeneratedBy.Identity().Column("id");
            References(x => x.BesteRightsNamespace).Column("namespace");
			Map(x => x.Token).Column("token").Not.Nullable();
            Map(x => x.LegitimationId).Column("legitimation_id").Not.Nullable();
            Map(x => x.Ends).Column("ends").Not.Nullable();
        }
    }
}
