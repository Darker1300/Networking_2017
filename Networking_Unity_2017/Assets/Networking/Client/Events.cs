using System;
using Networking;

namespace Client
{
    public enum ErrorID : byte
    {
        TooUserName = Protocol.IM_TooUsername,
        TooPassword = Protocol.IM_TooPassword,
        Exists = Protocol.IM_Exists,
        NoExists = Protocol.IM_NoExists,
        WrongPassword = Protocol.IM_WrongPass
    }

    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

    public delegate void AvailEventHandler(object sender, UserAvailEventArgs e);

    public delegate void ReceivedEventHandler(object sender, MsgReceivedEventArgs e);

    public class ErrorEventArgs : EventArgs
    {
        private ErrorID err;

        public ErrorEventArgs(ErrorID error)
        {
            this.err = error;
        }

        public ErrorID Error
        {
            get { return err; }
        }
    }

    public class UserAvailEventArgs : EventArgs
    {
        private string user;
        private bool avail;

        public UserAvailEventArgs(string user, bool avail)
        {
            this.user = user;
            this.avail = avail;
        }

        public string UserName
        {
            get { return user; }
        }

        public bool IsAvailable
        {
            get { return avail; }
        }
    }

    public class MsgReceivedEventArgs : EventArgs
    {
        private string user;
        private string msg;

        public MsgReceivedEventArgs(string user, string msg)
        {
            this.user = user;
            this.msg = msg;
        }

        public string From
        {
            get { return user; }
        }

        public string Message
        {
            get { return msg; }
        }
    }
}