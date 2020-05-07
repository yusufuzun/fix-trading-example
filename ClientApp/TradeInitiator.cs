using QuickFix;
using QuickFix.Fields;
using FixSpec = QuickFix.FIX50SP2;
using System;

namespace ClientApp
{
    public class TradeInitiator : MessageCracker, IApplication
    {
        private SessionID ClientSessionID { get; set; }

        public void FromAdmin(Message message, SessionID sessionID)
        {
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Console.WriteLine("IN:  " + message.ToString());
            try
            {
                Crack(message, sessionID);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==Cracker exception==");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void OnCreate(SessionID sessionID)
        {
            ClientSessionID = sessionID;
        }

        public void OnLogon(SessionID sessionID)
        {
            Console.WriteLine("Logon - " + sessionID.ToString());
        }

        public void OnLogout(SessionID sessionID)
        {
            Console.WriteLine("Logout - " + sessionID.ToString());
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
        }

        public void ToApp(Message message, SessionID sessionID)
        {
            try
            {
                bool possDupFlag = false;
                if (message.Header.IsSetField(QuickFix.Fields.Tags.PossDupFlag))
                {
                    possDupFlag = QuickFix.Fields.Converters.BoolConverter.Convert(
                        message.Header.GetString(QuickFix.Fields.Tags.PossDupFlag));
                }
                if (possDupFlag)
                    throw new DoNotSend();
            }
            catch (FieldNotFoundException)
            { }

            Console.WriteLine();
            Console.WriteLine("OUT: " + message.ToString());
        }

        public void OnMessage(FixSpec.MassQuote message, SessionID sessionId)
        {
            decimal? bid = null, ask = null;

            FixSpec.MassQuote.NoQuoteSetsGroup quoteSetsGroup = new FixSpec.MassQuote.NoQuoteSetsGroup();
            message.GetGroup(1, quoteSetsGroup);

            NoQuoteEntries noQuoteEntries = new NoQuoteEntries();
            QuoteSetID quoteSetId = new QuoteSetID();

            quoteSetsGroup.Get(noQuoteEntries);
            quoteSetsGroup.Get(quoteSetId);

            FixSpec.MassQuote.NoQuoteSetsGroup.NoQuoteEntriesGroup quoteEntriesGroup =
                new FixSpec.MassQuote.NoQuoteSetsGroup.NoQuoteEntriesGroup();

            quoteSetsGroup.GetGroup(1, quoteEntriesGroup);

            if (quoteEntriesGroup.IsSetField(new BidSpotRate()))
            {
                bid = quoteEntriesGroup.GetField(new BidSpotRate()).getValue();
            }

            if (quoteEntriesGroup.IsSetField(new OfferSpotRate()))
            {
                ask = quoteEntriesGroup.GetField(new OfferSpotRate()).getValue();
            }

            var currencyCode = quoteSetId.Obj;
            
            Console.WriteLine($"{currencyCode} : bid -> {bid} | ask -> {ask} ");
        }

        public void Run()
        {
            Console.WriteLine("Press q to leave market, otherwise send market data request...");

            while (Console.ReadLine() != "q")
            {
                var m = QueryMarketDataRequest();

                Session.LookupSession(ClientSessionID).Send(m);
            }
        }

        private FixSpec.MarketDataRequest QueryMarketDataRequest()
        {
            MDReqID mdReqId = new MDReqID("CLIAPP");
            SubscriptionRequestType subType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT);
            MarketDepth marketDepth = new MarketDepth(0);

            FixSpec.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new FixSpec.MarketDataRequest.NoMDEntryTypesGroup();
            marketDataEntryGroup.Set(new MDEntryType(MDEntryType.BID));

            var symbolGroup = new FixSpec.MarketDataRequest.NoRelatedSymGroup();
            symbolGroup.Set(new Symbol("EURUSD"));

            var message = new FixSpec.MarketDataRequest(mdReqId, subType, marketDepth);
            message.AddGroup(marketDataEntryGroup);
            message.AddGroup(symbolGroup);

            return message;
        }
    }
}
