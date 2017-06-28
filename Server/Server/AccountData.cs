using System;
using System.Collections.Generic;

namespace Server.Accounts
{
    [Serializable]
    public sealed class AccountData : IEquatable<AccountData>
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public AccountData(string user, string pass)
        {
            UserName = user;
            Password = pass;
        }

        public bool Equals(AccountData other)
        {
            return UserName.Equals(other.UserName);
        }

        public override int GetHashCode()
        {
            return UserName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AccountData);
        }
    }
}