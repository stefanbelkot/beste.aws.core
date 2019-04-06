using System;
using System.Text;
using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace Beste.Databases.User {
    
    public class User {
        public virtual int UserId { get; set; }
        public virtual string Firstname { get; set; }
        public virtual string Lastname { get; set; }
        public virtual string Email { get; set; }
        public virtual string Username { get; set; }
        public virtual string Password { get; set; }
        public virtual int? SaltValue { get; set; }
        public virtual bool? MustChangePassword { get; set; }
        public virtual int? WrongPasswordCounter { get; set; }

        public override bool Equals(object obj)
        {
            var user = obj as User;
            return user != null &&
                   UserId == user.UserId &&
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
            hash.Add(UserId);
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

    public class UserMap : ClassMap<User>
    {

        public UserMap()
        {
            Table("user");
            LazyLoad();
            Id(x => x.UserId).GeneratedBy.Identity().Column("user_id");
            Map(x => x.Firstname).Column("firstname");
            Map(x => x.Lastname).Column("lastname");
            Map(x => x.Email).Column("email");
            Map(x => x.Username).Column("username");
            Map(x => x.Password).Column("password");
            Map(x => x.SaltValue).Column("salt_value");
            Map(x => x.MustChangePassword).Column("must_change_password");
            Map(x => x.WrongPasswordCounter).Column("wrong_password_counter");
        }
    }
}
