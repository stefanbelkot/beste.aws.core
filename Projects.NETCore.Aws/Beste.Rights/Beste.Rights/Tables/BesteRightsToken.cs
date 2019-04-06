using System;
using System.Text;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Beste.Rights {

    [DynamoDBTable(TableName)]
    public class BesteRightsToken
    {
        [DynamoDBIgnore]
        public const string TableName = "beste_rights_token";
        public virtual int TableId { get; set; }
        public virtual string Namespace { get; set; }
        public virtual string Token { get; set; }
        public virtual int LegitimationId { get; set; }
        public virtual DateTime Ends { get; set; }
    }
}
