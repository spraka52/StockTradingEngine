# Real-Time Stock Trading Engine 

A high-frequency trading simulator implementing price-time priority matching across 1024 instruments using lock-free concurrency.

## Summary of code Implementation

- The program handles stock trading by managing buy/sell orders for 1,024 different stocks.
- It sorts buy orders from highest to lowest price, and sell orders from lowest to highest price.
- When a buy price matches or exceeds a sell price, it automatically trades shares between them.
- The code safely handles multiple users trading at the same time without crashes or errors.
- It uses special techniques to keep things fast (no locks) while ensuring accurate trades.
- Everything gets logged with colored timestamps to show orders and completed trades clearly.

## Key Features 
- **Massive Scale**: 1,024 independent tickers ( stocks)
- **Lock-Free Design**: Atomic operations with:
  - Interlocked.CompareExchange
  - Volatile.Read
  - Memory barriers
- **O(n) Efficiency**: Linear time matching algorithm
- **Thread-Safe Console**: Colored logging with microsecond timestamps

## Development Environment
- **.NET 9.0 SDK**

- Download: [.NET 9.0 Preview](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

## Installation & Execution

- **Clone repository**
git clone https://github.com/yourusername/stock-trading-engine.git

- **Build and run**
```sh
dotnet build 
dotnet run
```

## Lock-Free Operations Used

| Operation | Technique Used | Guarantees |
| ------ | ------ | ------ |
| Order Insertion | Interlocked.CompareExchange| Atomic linked list updates|
| Quantity Updates | 	Interlocked.Add| Thread-safe modifications|
| Order Removal | Memory barriers(Consistent view of shared state) + CAS(Compare-And-Swap)| Safe head pointer updates|
| Data Visibility | Volatile.Read| 	Fresh memory reads|


## Output:
![image](https://github.com/user-attachments/assets/08c00bcf-1bf4-4f8b-a524-f123bda18dcc)

