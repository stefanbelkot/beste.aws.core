using System;
using System.Text;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Beste.Rights {

    [DynamoDBTable(TableName)]
    public class BesteRightsAuthorization
    {
        [DynamoDBIgnore]
        public const string TableName = "beste_rights_authorization";
        [DynamoDBProperty]
        public virtual int TableId { get; set; }
        [DynamoDBRangeKey]
        public virtual string Uuid { get; set; }
        public virtual string LegitimationUuid { get; set; }
        public virtual bool Authorized { get; set; }
        public virtual string RecourceModule { get; set; }
        public virtual int? RecourceId { get; set; }
        public virtual string Operation { get; set; }
        public virtual string Namespace { get; set; }
    }
}
