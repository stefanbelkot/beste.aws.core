using Beste.Databases.Connector;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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

        readonly BesteRightsNamespace BesteRightsNamespace = null;
        readonly List<BesteRightsDefinition> BesteRightsDefinitions = new List<BesteRightsDefinition>();
        readonly Dictionary<string, BesteRightsToken> TokensForLegitimationIds = new Dictionary<string, BesteRightsToken>();
        readonly Settings settings = null;
        readonly string settingsPath = "Resources" + Path.DirectorySeparatorChar + "Beste.Rights.Settings.xml";
        public RightControl(string mainNamespace, string settingsPath = "") : base()
        {
            if (settingsPath != "")
                this.settingsPath = settingsPath;
            settings = SettingsManager.LoadSettings(this.settingsPath);

            BesteRightsNamespace = GetNameSpace(mainNamespace);
        }
        
        /// <summary>
        /// Register with rights from database and a list of additional rights
        /// </summary>
        /// <param name="legitimationId">associated legitimated id</param>
        /// <param name="additionalRights"></param>
        /// <param name="token">optional pregiven token</param>
        /// <returns>the registered token</returns>
        public string Register(int legitimationId, List<PureRight> additionalRights, string token = null)
        {
            List<PureRight> pureRights = GetPureRights(legitimationId);
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
        public string Register(int legitimationId, PureRight additionalRight, string token = null)
        {
            List<PureRight> pureRights = GetPureRights(legitimationId);
            pureRights.Add(additionalRight);
            return ApplyRights(legitimationId, pureRights, token);
        }

        /// <summary>
        /// Register with only rights from Database
        /// </summary>
        /// <param name="legitimationId">associated legitimated id</param>
        /// <param name="token">optional pregiven token</param>
        /// <returns>the registered token</returns>
        public string Register(int legitimationId, string token = null)
        {
            List<PureRight> pureRights = GetPureRights(legitimationId);
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
                    BesteRightsNamespace = BesteRightsNamespace,
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

        private List<PureRight> GetPureRights(int legitimationId)
        {
            List<BesteRightsAuthorization> besteRightsAuthorizations = new List<BesteRightsAuthorization>();
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {

                BesteRightsAuthorization besteRightsAuthorization = null;
                BesteRightsDefinition besteRightsDefinition = null;

                // projection list
                var columns = Projections.ProjectionList();
                // root properties
                columns.Add(Projections.Property(() => besteRightsAuthorization.Authorized).As("Authorized"));
                // reference properties
                columns.Add(Projections.Property(() => besteRightsDefinition.RecourceModule).As("RecourceModule"));
                columns.Add(Projections.Property(() => besteRightsDefinition.RecourceId).As("RecourceId"));
                columns.Add(Projections.Property(() => besteRightsDefinition.Operation).As("Operation"));

                var authorizations = session.QueryOver<BesteRightsAuthorization>(() => besteRightsAuthorization)
                    .JoinAlias(() => besteRightsAuthorization.BesteRightsDefinition, () => besteRightsDefinition)
                    .JoinAlias(() => besteRightsDefinition.BesteRightsNamespace, () => BesteRightsNamespace)
                    .Where(() => besteRightsDefinition.BesteRightsNamespace == BesteRightsNamespace)
                    .And(() => besteRightsAuthorization.LegitimationId == legitimationId)
                    .Select(columns);
                var list = authorizations
                    .TransformUsing(new DeepTransformer<PureRight>())
                    .List<PureRight>() as List<PureRight>;
                //.List<BesteRightsNamespace>() as List<BesteRightsAuthorization>;
                return list;
            }
        }

        private List<BesteRightsDefinition> GetDefinitions(BesteRightsNamespace besteRightsNamespace)
        {
            List<BesteRightsDefinition> besteRightsDefinitions = new List<BesteRightsDefinition>();
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                BesteRightsDefinition besteRightsDefinition = null;
                besteRightsDefinitions = session.QueryOver<BesteRightsDefinition>(() => besteRightsDefinition)
                    .JoinAlias(() => besteRightsDefinition.BesteRightsNamespace, () => BesteRightsNamespace)
                    .Where(() => besteRightsDefinition.BesteRightsNamespace == BesteRightsNamespace)
                    .List<BesteRightsNamespace>() as List<BesteRightsDefinition>;
                return besteRightsDefinitions;
            }
        }

        private BesteRightsNamespace GetNameSpace(string mainNamespace)
        {
            BesteRightsNamespace besteRightsNamespace = null;
            using (NHibernate.ISession session = SessionFactory.GetSession())
            using (ITransaction transaction = session.BeginTransaction())
            {
                besteRightsNamespace = (BesteRightsNamespace)session.QueryOver<BesteRightsNamespace>()
                    .Where(p => p.Name == mainNamespace)
                    .SingleOrDefault();
                if (besteRightsNamespace == null || besteRightsNamespace.Equals(new BesteRightsNamespace()))
                {
                    besteRightsNamespace = new BesteRightsNamespace();
                    besteRightsNamespace.Name = mainNamespace;
                    session.Save(besteRightsNamespace);
                    transaction.Commit();
                }
                return besteRightsNamespace;
            }
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
