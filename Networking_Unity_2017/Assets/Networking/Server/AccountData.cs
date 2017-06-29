using System;
using System.Collections.Generic;

namespace Server.Accounts
{
    [Serializable]
    public class AccountData : IEquatable<AccountData>
    {
        public string Username { get; set; }
        public string Password { get; set; }
        [NonSerialized]
        public bool LoggedIn;      // Is logged in and connected?
        [NonSerialized]
        public ClientConnection Connection;  // Connection info

        public AccountData(string user, string pass)
        {
            Username = user;
            Password = pass;
            LoggedIn = false;
        }
        public AccountData(string user, string pass, ClientConnection conn)
        {
            Username = user;
            Password = pass;
            LoggedIn = true;
            Connection = conn;
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