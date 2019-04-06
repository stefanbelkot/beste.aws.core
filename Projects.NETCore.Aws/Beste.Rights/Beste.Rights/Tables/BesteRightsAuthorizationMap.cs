using System; 
using System.Collections.Generic; 
using System.Text; 
using FluentNHibernate.Mapping;
using Beste.Rights; 

namespace Beste.Rights {
    
    
    public class BesteRightsAuthorizationMap : ClassMap<BesteRightsAuthorization> {
        
        public BesteRightsAuthorizationMap() {
			Table("beste_rights_authorization");
			LazyLoad();
            Id(x => x.Id).GeneratedBy.Identity().Column("id");
            References(x => x.BesteRightsDefinition).Column("definition_id");
			Map(x => x.LegitimationId).Column("legitimation_id").Not.Nullable();
			Map(x => x.Authorized).Column("authorized").Not.Nullable();
        }
    }
}
