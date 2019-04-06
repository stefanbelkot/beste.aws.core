using System;
using System.Text;
using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace Beste.Databases.User {

    [DynamoDBTable(TableName)]
    public class User {

        [DynamoDBIgnore]
        public const string TableName = "user";

        [DynamoDBHashKey]
        [DynamoDBProperty("id")]
        public virtual int TableId { get; set; }
        [DynamoDBRangeKey]
        [DynamoDBProperty("username")]
        public virtual string Username { get; set; }
        public virtual string Firstname { get; set; }
        public virtual string Lastname { get; set; }
        public virtual string Email { get; set; }
        public virtual string Password { get; set; }
        public virtual int? SaltValue { get; set; }
        public virtual bool? MustChangePassword { get; set; }
        public virtual int? WrongPasswordCounter { get; set; }

        public override bool Equals(object obj)
        {
            var user = obj as User;
            return user != null &&
                   TableId == user.TableId &&
                   Firstname == user.Firstname &&
                   Lastname == user.Lastname &&
                   Email == user.Email &&
                   Username == user.Username &&
                   Password == user.Password &&
                   EqualityComparer<int?>.Default.Equals(SaltValue, user.SaltValue) &&
                   EqualityComparer<bool?>.Default.Equals(MustChangePassword, user.MustChangePassword) &&
                   EqualityComparer<int?>.Default.Equals(WrongPasswordCounter, user.WrongPasswordCounter);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(TableId);
            hash.Add(Firstname);
            hash.Add(Lastname);
            hash.Add(Email);
            hash.Add(Username);
            hash.Add(Password);
            hash.Add(SaltValue);
            hash.Add(MustChangePassword);
            hash.Add(WrongPasswordCounter);
            return hash.ToHashCode();
        }
    }

}
