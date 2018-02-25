using System.Threading;
using hMailServer.Core.Protocols;

namespace hMailServer.Core
{
    public class SessionManager
    {
        private static long _smtpdSessionCount;
        private static long _pop3dSessionCount;
        private static long _imapdSessionCount;

        public static void IncreaseSessionCount(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.IMAPD:
                    Interlocked.Increment(ref _imapdSessionCount);
                    break;
                case Protocol.POP3D:
                    Interlocked.Increment(ref _pop3dSessionCount);
                    break;
                case Protocol.SMTPD:
                    Interlocked.Increment(ref _smtpdSessionCount);
                    break;
            }
        }

        public static void DecreaseSessionCount(Protocol protocol)
        {
            switch (protocol)
            {
                case Protocol.IMAPD:
                    Interlocked.Decrement(ref _imapdSessionCount);
                    break;
                case Protocol.POP3D:
                    Interlocked.Decrement(ref _pop3dSessionCount);
                    break;
                case Protocol.SMTPD:
                    Interlocked.Decrement(ref _smtpdSessionCount);
                    break;
            }

        }
    }
}
