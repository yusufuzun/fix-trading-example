using Acceptor;
using QuickFix;
using QuickFix.Transport;
using System;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var configClient = AppDomain.CurrentDomain.BaseDirectory + "\\sample_initiator.cfg";

            SessionSettings settings = new SessionSettings(configClient);

            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);

            ILogFactory logFactory = new FileLogFactory(settings);

            var client = new TradeInitiator();

            SocketInitiator initiator = new SocketInitiator(
                client,
                storeFactory,
                settings,
                logFactory);

            initiator.Start();

            client.Run();

            initiator.Stop();

        }
    }
}
