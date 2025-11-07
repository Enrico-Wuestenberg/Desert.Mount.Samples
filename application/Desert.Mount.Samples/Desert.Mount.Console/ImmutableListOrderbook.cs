using System.Collections.Immutable;

namespace Desert.Mount.ConsoleApp
{
    public class ImmutableListOrderbook
    {
        public void Run()
        {
            // Beispielnutzung im Intraday‑Handel
            var day = new DateTime(2025, 01, 15);
            var book = OrderBook.Seed(day);

            // Eingehende Orders (z. B. von Broker/API)
            var incoming = new[]
            {
                new TradeOrder("B-new", day.AddHours(12), TimeSpan.FromMinutes(15), 20m, 80m, Side.Buy),
                new TradeOrder("S-new", day.AddHours(12), TimeSpan.FromMinutes(15), 15m, 79m, Side.Sell),
            };

            // Batch‑Update
            book = book.AddRange(incoming); // Bulk‑Add 

            // Sortierte Einfügungen (Top‑of‑Book stabil halten)
            book = book.InsertSorted(new TradeOrder("B-Top", day.AddHours(12), TimeSpan.FromMinutes(15), 25m, 82m, Side.Buy));
            book = book.InsertSorted(new TradeOrder("S-Top", day.AddHours(12), TimeSpan.FromMinutes(15), 25m, 78m, Side.Sell));

            // Matching
            book = book.MatchTopOfBook();

            // Cancel
            book = book.Cancel("B0"); // Entfernt erstes Seed‑Gebot für Block 00:00 

            book.Asks.ForEach(trade => Console.WriteLine(trade));
            book.Bids.ForEach(trade => Console.WriteLine(trade));
        }
    }

    public sealed class OrderBook
    {
        public ImmutableList<TradeOrder> Bids { get; init; }
        public ImmutableList<TradeOrder> Asks { get; init; }
        public static OrderBook Empty => new()
        {
            Bids = ImmutableList<TradeOrder>.Empty,
            Asks = ImmutableList<TradeOrder>.Empty
        };

        // Seed: Erzeuge für einen Handelstag je 96 15‑Min‑Blöcke
        // initiale Orders (Bulk via Builder) 
        public static OrderBook Seed(DateTime day)
        {
            // Der Builder ist eine mutable Hülle die viele Änderungen effizient puffert
            // So werden eine Vielzahl von Zwischeninstanzen vermieden
            var bidsB = ImmutableList.CreateBuilder<TradeOrder>(); 
            var asksB = ImmutableList.CreateBuilder<TradeOrder>();

            for (int i = 0; i < 96; i++)
            {
                var start = day.Date.AddMinutes(15 * i);
                // Beispielkurve: leicht fallende Gebote, steigende Angebote
                bidsB.Add(new TradeOrder($"B{i}", start, TimeSpan.FromMinutes(15), 10m, 60m - i * 0.1m, Side.Buy));
                asksB.Add(new TradeOrder($"S{i}", start, TimeSpan.FromMinutes(15), 10m, 65m + i * 0.1m, Side.Sell));
            }

            return new OrderBook
            {
                // Bulk‑Operationen/Builder reduzieren GC‑Druck
                // -> viele kleine Änderungen erzeugen kurzlebige Objekte,
                // die der GC einsammeln muss, bulk-operationen verringern
                // die Anzahl an Zwischenobjekten
                Bids = bidsB.ToImmutable(),  
                Asks = asksB.ToImmutable()
            };
        }

        // Batch‑Update eingehender Orders (z. B. von MarketData‑Feed)
        public OrderBook AddRange(IEnumerable<TradeOrder> orders)
        {
            var bidsB = Bids.ToBuilder();
            var asksB = Asks.ToBuilder();

            foreach (var o in orders)
            {
                if (o.Side == Side.Buy) bidsB.Add(o);
                else asksB.Add(o);
            }

            return new OrderBook { Bids = bidsB.ToImmutable(), Asks = asksB.ToImmutable() }; 
        }

        // Order stornieren (Cancel) per Id
        public OrderBook Cancel(string orderId)
        {
            var iBid = Bids.FindIndex(x => x.Id == orderId);
            if (iBid >= 0) return new OrderBook { Bids = Bids.RemoveAt(iBid), Asks = Asks };

            var iAsk = Asks.FindIndex(x => x.Id == orderId);
            if (iAsk >= 0) return new OrderBook { Bids = Bids, Asks = Asks.RemoveAt(iAsk) };

            return this;
        }

        // Einfaches Matching: Top‑of‑Book kreuzt (gleiches Lieferintervall, Bid >= Ask)
        public OrderBook MatchTopOfBook()
        {
            if (Bids.Count == 0 || Asks.Count == 0) return this;

            var bestBid = Bids[0]; // Indexer
            var bestAsk = Asks[0]; // Indexer

            if (bestBid.DeliveryStart == bestAsk.DeliveryStart &&
                bestBid.PriceEurMWh >= bestAsk.PriceEurMWh)
            {
                // Ausführen: Spitze entfernen (neue unveränderliche Zustände)
                var newBids = Bids.RemoveAt(0);
                var newAsks = Asks.RemoveAt(0);
                return new OrderBook { Bids = newBids, Asks = newAsks };
            }
            return this;
        }

        // Optional: Preis‑sortierte Einfügung mit binärer Suche auf Builder
        public OrderBook InsertSorted(TradeOrder order)
        {
            if (order.Side == Side.Buy)
            {
                var b = Bids.ToBuilder();
                // O(log N) Suche; Insert in ImmutableList ist effizient
                int idx = BinarySearchInsertIndex(b, order, CompareBid);   
                b.Insert(idx, order);                                    
                return new OrderBook { Bids = b.ToImmutable(), Asks = Asks };
            }
            else
            {
                var a = Asks.ToBuilder();
                int idx = BinarySearchInsertIndex(a, order, CompareAsk);   
                a.Insert(idx, order);                                    
                return new OrderBook { Bids = Bids, Asks = a.ToImmutable() };
            }
        }

        private static int CompareBid(TradeOrder x, TradeOrder y)
        {
            // Bids: hoher Preis zuerst, dann frühere Lieferung
            int byPriceDesc = -x.PriceEurMWh.CompareTo(y.PriceEurMWh);
            return byPriceDesc != 0 
                ? byPriceDesc 
                : x.DeliveryStart.CompareTo(y.DeliveryStart);
        }
        private static int CompareAsk(TradeOrder x, TradeOrder y)
        {
            // Asks: niedriger Preis zuerst, dann frühere Lieferung
            int byPriceAsc = x.PriceEurMWh.CompareTo(y.PriceEurMWh);
            return byPriceAsc != 0 
                ? byPriceAsc 
                : x.DeliveryStart.CompareTo(y.DeliveryStart);
        }

        private static int BinarySearchInsertIndex(ImmutableList<TradeOrder>.Builder b, TradeOrder item, Comparison<TradeOrder> cmp)
        {
            int lo = 0, hi = b.Count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) / 2);
                int c = cmp(b[mid], item);            // Indexer auf Builder
                if (c <= 0) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }
    }

    public enum Side { Buy, Sell }

    public readonly record struct TradeOrder(
        string Id,
        DateTime DeliveryStart,
        TimeSpan Duration,
        decimal VolumeMw,
        decimal PriceEurMWh,
        Side Side
    );
}
