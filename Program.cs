using System;
using System.Threading;

public enum OrderType { Buy, Sell }

public class Order
{
    public readonly OrderType Type;
    public readonly decimal Price;
    public int Quantity;
    public Order Next;
    public readonly DateTime CreatedTime = DateTime.UtcNow;
    public readonly long SequenceNumber;

    public Order(OrderType type, int quantity, decimal price, long sequenceNumber)
    {
        Type = type;
        Quantity = quantity;
        Price = price;
        SequenceNumber = sequenceNumber;
    }
}

public class OrderBook
{
    public Order BuyHead;
    public Order SellHead;
}

public static class TradingEngine
{
    private const int NUM_TICKERS = 1024;
    private static readonly OrderBook[] OrderBooks = new OrderBook[NUM_TICKERS];
    private static readonly object consoleLock = new object();
    private static long _orderCounter;
    
    static TradingEngine()
    {
        for (int i = 0; i < NUM_TICKERS; i++)
            OrderBooks[i] = new OrderBook();
    }

    private static int GetTickerIndex(string ticker)
    {
        return int.Parse(ticker.AsSpan(3)) % NUM_TICKERS;
    }

    public static void AddOrder(OrderType type, string ticker, int quantity, decimal price)
    {
        long seq = Interlocked.Increment(ref _orderCounter);
        int index = GetTickerIndex(ticker);
        Order newOrder = new Order(type, quantity, price, seq);
        
        if (type == OrderType.Buy)
        {
            InsertSorted(ref OrderBooks[index].BuyHead, newOrder, 
                (a, b) => a.Price > b.Price || (a.Price == b.Price && a.CreatedTime < b.CreatedTime));
            LogOrder($"[{seq}] BUY  {quantity}@{price} for {ticker}", ConsoleColor.Green);
        }
        else
        {
            InsertSorted(ref OrderBooks[index].SellHead, newOrder, 
                (a, b) => a.Price < b.Price || (a.Price == b.Price && a.CreatedTime < b.CreatedTime));
            LogOrder($"[{seq}] SELL {quantity}@{price} for {ticker}", ConsoleColor.Red);
        }
    }

    private static void InsertSorted(ref Order head, Order newOrder, Func<Order, Order, bool> comparer)
    {
        while (true)
        {
            Order prev = null;
            Order current = Volatile.Read(ref head);
            
            while (current != null && comparer(current, newOrder))
            {
                prev = current;
                current = Volatile.Read(ref current.Next);
            }

            newOrder.Next = current;
            
            if (prev == null)
            {
                if (Interlocked.CompareExchange(ref head, newOrder, current) == current)
                    return;
            }
            else
            {
                if (Interlocked.CompareExchange(ref prev.Next, newOrder, current) == current)
                    return;
            }
        }
    }

     public static void MatchOrder(string ticker)
    {
        int index = GetTickerIndex(ticker);
        OrderBook book = OrderBooks[index];
        
        Order buy = Volatile.Read(ref book.BuyHead);
        Order sell = Volatile.Read(ref book.SellHead);

        while (buy != null && sell != null)
        {
            if (buy.Price < sell.Price) break;

            int fillQty = Math.Min(buy.Quantity, sell.Quantity);
            if (fillQty > 0)
            {
                LogTrade($"{ticker} MATCH: Buy#{buy.SequenceNumber} ({buy.Quantity}@{buy.Price}) " +
                       $"vs Sell#{sell.SequenceNumber} ({sell.Quantity}@{sell.Price}) → {fillQty}@{sell.Price}", 
                       ConsoleColor.Yellow);
            }

            Interlocked.Add(ref buy.Quantity, -fillQty);
            Interlocked.Add(ref sell.Quantity, -fillQty);

            RemoveIfFilled(ref book.BuyHead, ref buy);
            RemoveIfFilled(ref book.SellHead, ref sell);
        }
    }

    private static void RemoveIfFilled(ref Order head, ref Order current)
    {
        if (Volatile.Read(ref current.Quantity) > 0) return;
        
        Order next = Volatile.Read(ref current.Next);
        if (Interlocked.CompareExchange(ref head, next, current) == current)
            current = next;
        else
            current = Volatile.Read(ref head);
    }

    private static void LogOrder(string message, ConsoleColor color)
    {
        lock (consoleLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            Console.ResetColor();
        }
    }

    private static void LogTrade(string message, ConsoleColor color)
    {
        lock (consoleLock)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            Console.ResetColor();
        }
    }

    public static void SimulateTransactions()
    {
        string[] tickers = new string[NUM_TICKERS];
        for (int i = 0; i < NUM_TICKERS; i++)
        {
            tickers[i] = $"TKR{i:0000}";
        }
        
        Random rand = new Random();
        while (true)
        {
            string ticker = tickers[rand.Next(NUM_TICKERS)];
            OrderType orderType = rand.Next(2) == 0 ? OrderType.Buy : OrderType.Sell;
            int quantity = rand.Next(1, 101);
            decimal price = rand.Next(100, 201);
            
            AddOrder(orderType, ticker, quantity, price);
            MatchOrder(ticker);
            Thread.Sleep(50);
        }
    }

    public static void Main()
    {
        Console.WriteLine("=== STOCK TRADING ENGINE STARTED ===");
        Console.WriteLine("Color coding:");
        Console.WriteLine("- Green: Buy orders");
        Console.WriteLine("- Red: Sell orders");
        Console.WriteLine("- Yellow: Trades\n");
        
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(SimulateTransactions) { IsBackground = true }.Start();
        }
        
        Thread.Sleep(Timeout.Infinite);
    }
}