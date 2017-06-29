using System;
using System.Collections.Generic;

namespace Server.Accounts
{
    [Serializable]
    public sealed class AccountData : IEquatable<AccountData>
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public AccountData(string user, string pass)
        {
            Username = user;
            Password = pass;
        }

        public bool Equals(AccountData other)
        {
            return Username.Equals(other.Username);
        }

        public override int GetHashCode()
        {
            return Username.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AccountData);
        }
    }
}