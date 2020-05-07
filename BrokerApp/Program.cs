using Acceptor;
using QuickFix;
using System;

namespace BrokerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var configBroker = AppDomain.CurrentDomain.BaseDirectory + "\\sample_acceptor.cfg";

            SessionSettings settings = new SessionSettings(configBroker);

            IMessageStoreFactory storeFactory = new FileStoreFactory(settings);

            ILogFactory logFactory = new FileLogFactory(settings);

            var broker = new TradeAcceptor();

            ThreadedSocketAcceptor acceptor = new ThreadedSocketAcceptor(
                broker,
                storeFactory,
                settings,
                logFactory);

            string HttpServerPrefix = "http://127.0.0.1:5080/";

            HttpServer srv = new HttpServer(HttpServerPrefix, settings);

            acceptor.Start();

            srv.Start();

            Console.WriteLine("View Executor status: " + HttpServerPrefix);
            
            Run();

            srv.Stop();

            acceptor.Stop();
        }

        private static void Run()
        {
            Console.WriteLine("press q to quit");

            while (Console.ReadLine() != "q")
            {
                Console.WriteLine("Please press q for quit broker...");
            }
        }
    }
}
