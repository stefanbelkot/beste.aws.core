using System; 
using System.Collections.Generic; 
using System.Text; 
using FluentNHibernate.Mapping;
using Beste.Rights; 

namespace Beste.Rights {
    
    
    public class BesteRightsNamespaceMap : ClassMap<BesteRightsNamespace> {
        
        public BesteRightsNamespaceMap() {
			Table("beste_rights_namespace");
			LazyLoad();
			Id(x => x.Id).GeneratedBy.Identity().Column("id");
			Map(x => x.Name).Column("name").Not.Nullable();
        }
    }
}
