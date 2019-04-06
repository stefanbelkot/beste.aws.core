using System; 
using System.Collections.Generic; 
using System.Text; 
using FluentNHibernate.Mapping;
using Beste.Rights; 

namespace Beste.Rights {
    
    
    public class BesteRightsDefinitionMap : ClassMap<BesteRightsDefinition> {
        
        public BesteRightsDefinitionMap() {
			Table("beste_rights_definition");
			LazyLoad();
			Id(x => x.Id).GeneratedBy.Identity().Column("id");
			References(x => x.BesteRightsNamespace).Column("namespace");
			Map(x => x.RecourceModule).Column("recource_module").Not.Nullable();
			Map(x => x.RecourceId).Column("recource_id");
			Map(x => x.Operation).Column("operation").Not.Nullable();
        }
    }
}
