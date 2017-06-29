using System;

namespace Client
{
    public enum IMError : byte
    {
        TooUserName = Networking.Protocol.IM_TooUsername,
        TooPassword = Networking.Protocol.IM_TooPassword,
        Exists = Networking.Protocol.IM_Exists,
        NoExists = Networking.Protocol.IM_NoExists,
        WrongPassword = Networking.Protocol.IM_WrongPass
    }

    public delegate void IMErrorEventHandler(object sender, IMErrorEventArgs e);

    public delegate void IMAvailEventHandler(object sender, IMAvailEventArgs e);

    public delegate void IMReceivedEventHandler(object sender, IMReceivedEventArgs e);

    public class IMErrorEventArgs : EventArgs
    {
        private IMError err;

        public IMErrorEventArgs(IMError error)
        {
            this.err = error;
        }

        public IMError Error
        {
            get { return err; }
        }
    }

    public class IMAvailEventArgs : EventArgs
    {
        private string user;
        private bool avail;

        public IMAvailEventArgs(string user, bool avail)
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

    public class IMReceivedEventArgs : EventArgs
    {
        private string user;
        private string msg;

        public IMReceivedEventArgs(string user, string msg)
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