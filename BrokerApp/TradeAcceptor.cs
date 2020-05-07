using QuickFix;
using FixSpec = QuickFix.FIX50SP2;
using System;
using System.Collections.Generic;
using QuickFix.Fields;
using QuickFix.FIX50SP2;

namespace BrokerApp
{
    public class TradeAcceptor : MessageCracker, IApplication
    {
        private readonly Dictionary<string, Stack<decimal>> currencyRates;

        public TradeAcceptor()
        {
            currencyRates = new Dictionary<string, Stack<decimal>>()
            {
                {"EURUSD", new Stack<decimal>(){  } }
            };

            foreach (var currency in currencyRates)
            {
                currency.Value.Push(1);
            }
        }

        public void FromAdmin(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine("IN:  " + message);
        }

        public void FromApp(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine("IN:  " + message);
            Crack(message, sessionID);
        }

        public void OnCreate(SessionID sessionID)
        {
        }

        public void OnLogon(SessionID sessionID)
        {
        }

        public void OnLogout(SessionID sessionID)
        {
        }

        public void ToAdmin(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine("OUT: " + message);
        }

        public void ToApp(QuickFix.Message message, SessionID sessionID)
        {
            Console.WriteLine("OUT: " + message);
        }

        public void OnMessage(FixSpec.MarketDataRequest message, SessionID sessionId)
        {
            ResolveMarketDataRequest(message, out Symbol symbol, out char bidAskObj, out string currencyCodeObj);

            //prepare MassQuote response

            CalculateNewPrice(currencyCodeObj, out decimal bidPrice, out decimal askPrice);

            FixSpec.MassQuote massQuote = new FixSpec.MassQuote(new QuoteID(Guid.NewGuid().ToString("N")));

            FixSpec.MassQuote.NoQuoteSetsGroup quoteSetsGroup = new FixSpec.MassQuote.NoQuoteSetsGroup();

            NoQuoteEntries noQuoteEntries = new NoQuoteEntries(1);
            quoteSetsGroup.Set(noQuoteEntries);

            QuoteSetID quoteSetId = new QuoteSetID(symbol.Obj);
            quoteSetsGroup.Set(quoteSetId);

            FixSpec.MassQuote.NoQuoteSetsGroup.NoQuoteEntriesGroup quoteEntriesGroup =
                new FixSpec.MassQuote.NoQuoteSetsGroup.NoQuoteEntriesGroup();

            quoteEntriesGroup.QuoteEntryID = new QuoteEntryID(currencyCodeObj);

            if (bidAskObj == MDEntryType.BID)
            {
                quoteEntriesGroup.SetField(new BidSpotRate(bidPrice));
            }

            if (bidAskObj == MDEntryType.OFFER)
            {
                quoteEntriesGroup.SetField(new OfferSpotRate(askPrice));
            }

            quoteSetsGroup.AddGroup(quoteEntriesGroup);

            massQuote.AddGroup(quoteSetsGroup);


            Session.LookupSession(sessionId).Send(massQuote);

            Console.WriteLine($"{currencyCodeObj} : bid -> {bidPrice} | ask -> {askPrice} ");
        }

        private static void ResolveMarketDataRequest(MarketDataRequest message, out Symbol symbol, out char bidAskObj, out string currencyCodeObj)
        {
            MDReqID mdReqId = new MDReqID();
            message.Get(mdReqId);

            SubscriptionRequestType subType = new SubscriptionRequestType();
            message.Get(subType);

            MarketDepth marketDepth = new MarketDepth();
            message.Get(marketDepth);

            var symbolGroup = new FixSpec.MarketDataRequest.NoRelatedSymGroup();
            message.GetGroup(1, symbolGroup);
            symbol = new Symbol();
            symbolGroup.Get(symbol);

            FixSpec.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new FixSpec.MarketDataRequest.NoMDEntryTypesGroup();
            message.GetGroup(1, marketDataEntryGroup);
            var mDEntryType = new MDEntryType();
            marketDataEntryGroup.Get(mDEntryType);

            bidAskObj = mDEntryType.Obj;
            currencyCodeObj = symbol.Obj;
            var marketDepthObj = marketDepth.Obj;
        }

        private void CalculateNewPrice(string currencyCodeObj, out decimal bidPrice, out decimal askPrice)
        {
            var spread = (decimal)0.01;

            //max 10% change
            var changeRate = ((decimal)new Random().Next(-99, 99)) / 1000;

            var lastPrice = currencyRates[currencyCodeObj].Peek();

            var newPrice = lastPrice + (changeRate * lastPrice);

            bidPrice = newPrice + (newPrice * spread);
            askPrice = newPrice - (newPrice * spread);
            currencyRates[currencyCodeObj].Push(newPrice);
        }
    }
}
