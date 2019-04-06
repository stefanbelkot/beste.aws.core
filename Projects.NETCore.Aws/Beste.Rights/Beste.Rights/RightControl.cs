using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Beste.Aws.Databases.Connector;
using Beste.Databases.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Beste.Rights
{
    public class RightControl : AccessControlList
    {
        /// <summary>
        /// Concept
        /// Each Right will be generated with a token and added to the "AccessControlList"
        /// What in the "AccessControlList" is called "Principal" or "Principals" will
        /// in this class be the "Token".
        /// All rights are Granted or Denied for a specific Token
        /// Which and how rights will be granted or denied is specified by the logic in the user of
        /// the Library Beste.Rights
        /// example: On authorization in a User Module a specific user gets a token
        ///  By program logic in the User Module the User gets right to "Modify" (The Operation) himself
        ///  by give him Modify rights to the Ressource "User.Module.User_<UserId>"
        /// example 2: For an admin the rights are already predefined by the combination of:
        ///  "User.Module.User" and all his legitimated operations (Add, Edit, Delete) in the database
        ///  As soon the admin logs in, additionally to his personal rights (Modify User.Module.User_<AdminUserId>"
        ///  the admin gets rights to "User.Module.User"
        ///  The admin does not get the right by his Id but with a token. This token must be transmitted everytime
        ///  the admin wants to do a restricted operation in a module.
        /// In the database the rights are defined in the table "beste_rights_authorization"
        /// The table is kept generalized. The field "legitimation_id" represents for example the User Id of the Admin
        /// In another context this might represent other Ids, like "Service Ids", "ComputerIds" and so on.
        /// Important is, that always this rights will be mapped to tokens internally and the token will be used on
        /// any later check after the "registration" (for users e.g login, for "Computers" some network handshake for example)
        /// </summary>

        public static int TABLE_ID = 1;
        readonly string BesteRightsNamespace = null;
        readonly Dictionary<string, BesteRightsToken> TokensForLegitimationIds = new Dictionary<string, BesteRightsToken>();
        readonly Settings settings = null;
        readonly string settingsPath = "Resources" + Path.DirectorySeparatorChar + "Beste.Rights.Settings.xml";
        public RightControl(string mainNamespace, string settingsPath = "") : base()
        {
            if (settingsPath != "")
                this.settingsPath = settingsPath;
            settings = SettingsManager.LoadSettings(this.settingsPath);

            BesteRightsNamespace = mainNamespace;
        }
        
        /// <summary>
        /// Register with rights from database and a list of additional rights
        /// </summary>
        /// <param name="legitimationId">associated legitimated id</param>
        /// <param name="additionalRights"></param>
        /// <param name="token">optional pregiven token</param>
        /// <returns>the registered token</returns>
        public async Task<string> Register(int legitimationId, List<PureRight> additionalRights, string token = null)
        {
            List<PureRight> pureRights = await GetPureRights(legitimationId);
            pureRights.AddRange(additionalRights);
            return ApplyRights(legitimationId, pureRights, token);
        }
        
        /// <summary>
        /// Register with rights from database and one additional right
        /// </summary>
        /// <param name="legitimationId">associated legitimated id</param>
        /// <param name="additionalRight"></param>
        /// <param name="token">optional pregiven token</param>
        /// <returns>the registered token</returns>
        public async Task<string> Register(int legitimationId, PureRight additionalRight, string token = null)
        {
            List<PureRight> pureRights = await GetPureRights(legitimationId);
            pureRights.Add(additionalRight);
            return ApplyRights(legitimationId, pureRights, token);
        }

        /// <summary>
        /// Register with only rights from Database
        /// </summary>
        /// <param name="legitimationId">associated legitimated id</param>
        /// <param name="token">optional pregiven token</param>
        /// <returns>the registered token</returns>
        public async Task<string> Register(int legitimationId, string token = null)
        {
            List<PureRight> pureRights = await GetPureRights(legitimationId);
            return ApplyRights(legitimationId, pureRights, token);
        }

        /// <summary>
        /// Applies the rights and returns the generated token
        /// </summary>
        /// <param name="legitimationId"></param>
        /// <param name="pureRights"></param>
        /// <returns></returns>
        private string ApplyRights(int legitimationId, List<PureRight> pureRights, string token = null)
        {
            string authorizedToken = token ?? GenerateToken(legitimationId);
            RegisterToken(authorizedToken, legitimationId);
            foreach(PureRight pureRight in pureRights)
            {
                if (pureRight.Authorized == true)
                {
                    Grant(authorizedToken, pureRight.Operation, pureRight.RecourceModule, pureRight.RecourceId);
                }
                else
                {
                    Deny(authorizedToken, pureRight.Operation, pureRight.RecourceModule, pureRight.RecourceId);
                }
            }

            return authorizedToken;
        }

        private void RegisterToken(string authorizedToken, int legitimationId)
        {
            if(!TokensForLegitimationIds.ContainsKey(authorizedToken))
            {
                TokensForLegitimationIds.Add(authorizedToken, new BesteRightsToken
                {
                    Namespace = BesteRightsNamespace,
                    LegitimationId = legitimationId,
                    Token = authorizedToken,
                    Ends = DateTime.Now
                });
            }
        }

        private string GenerateToken(int legitimationId)
        {
            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return token;
        }

        private async Task<List<PureRight>> GetPureRights(int legitimationId)
        {
            return await AmazonDynamoDBFactory.ExecuteInTransactionContext(async (client, context) =>
            {
                List<PureRight> pureRights = new List<PureRight>();
                var request = new QueryRequest
                {
                    TableName = BesteRightsAuthorization.TableName,
                    //ScanIndexForward = false,
                    KeyConditions = new Dictionary<string, Condition>
                    {
                        { "TableId", new Condition()
                            {
                                ComparisonOperator = ComparisonOperator.EQ,
                                AttributeValueList = new List<AttributeValue>
                                {
                                  new AttributeValue { N = TABLE_ID.ToString() }
                                }

                            }
                        }
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        { "#namespace", "Namespace" },
                        { "#legitimationid", "LegitimationId" }
                    },
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":namespace", new AttributeValue { S = BesteRightsNamespace } },
                        { ":legitimationid", new AttributeValue { N = legitimationId.ToString() } }
                    },
                    FilterExpression = "#namespace = :namespace and #legitimationid = :legitimationid"
                };
                var response = await AmazonDynamoDBFactory.Client.QueryAsync(request);
                foreach(var item in response.Items)
                {
                    PureRight pureRight = new PureRight
                    {
                        RecourceModule = item["RecourceModule"].S,
                        Authorized = item["Authorized"].N == "1" ? true : false,
                        Operation = item["Operation"].S
                    };
                    if (item.ContainsKey("RecourceId"))
                        pureRight.RecourceId = Convert.ToInt32(item["RecourceId"].N);
                    pureRights.Add(pureRight);
                }
                return pureRights;
            });
        }
        
        private bool IsTokenRegistered(string token)
        {
            return TokensForLegitimationIds.ContainsKey(token);
        }
        
        /// <summary>
        /// Explain why the operation is granted or denied on the resource, given a collection of principals.
        /// </summary>
        public string Explain(string token, string operation, string resource, int? resourceId = null)
        {
            if (IsTokenRegistered(token))
            {
                BesteRightsToken besteRightsToken = TokensForLegitimationIds[token];
                string explaination = base.Explain(new string[] { token }, operation, resource + (resourceId == null ? "" : "_" + resourceId));
                return explaination.Replace(token, token + " for " + besteRightsToken.LegitimationId);
            }
            return "token='" + token + "' not registered";
        }
        public new string Explain(string[] token, string operation, string resourceWithId)
        {
            return base.Explain(token, operation, resourceWithId);
        }

        /// <summary>
        /// Returns true if any of the principals is granted the operation on the resource.
        /// </summary>
        public bool IsGranted(string token, string operation, string resource, int? resourceId = null)
        {
            if (!IsTokenRegistered(token))
            {
                return false;
            }
            return base.IsGranted(new string[] { token }, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }
        public new bool IsGranted(string[] token, string operation, string resourceWithId)
        {
            return base.IsGranted(token, operation, resourceWithId);
        }

        /// <summary>
        /// Returns true if any of the principals is explicitly denied the operation on the resource.
        /// </summary>
        public bool IsDenied(string token, string operation, string resource, int? resourceId = null)
        {
            if (!IsTokenRegistered(token))
            {
                return true;
            }
            return base.IsDenied(new string[] { token }, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }
        public new bool IsDenied(string[] token, string operation, string resourceWithId)
        {
            return base.IsDenied(token, operation, resourceWithId);
        }

        /// <summary>
        /// Adds a permission to the ACL.
        /// </summary>
        public void Grant(string token, string operation, string resource, int? resourceId = null)
        {
            base.Grant(token, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }
        public new void Grant(string token, string operation, string resourceWithId)
        {
            base.Grant(token, operation, resourceWithId);
        }

        /// <summary>
        /// Removes a permission from the ACL.
        /// </summary>
        public void Revoke(string token, string operation, string resource, int? resourceId = null)
        {
            base.Revoke(token, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }
        public new void Revoke(string token, string operation, string resourceWithId)
        {
            base.Revoke(token, operation, resourceWithId);
        }

        /// <summary>
        /// Adds an overriding permission denial to the ACL.
        /// </summary>
        public void Deny(string token, string operation, string resource, int? resourceId = null)
        {
            base.Deny(token, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }
        public new void Deny(string token, string operation, string resourceWithId)
        {
            base.Deny(token, operation, resourceWithId);
        }

        /// <summary>
        /// Removes a denial from the ACL.
        /// </summary>
        public void Allow(string token, string operation, string resource, int? resourceId = null)
        {
            _denied.Exclude(token, operation, resource + (resourceId == null ? "" : "_" + resourceId));
        }

    }
}
