﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.OrderBook;
using CryptoExchange.Net.Sockets;
using Force.Crc32;
using Kraken.Net.Interfaces;
using Kraken.Net.Objects;
using Microsoft.Extensions.Logging;

namespace Kraken.Net
{
    /// <summary>
    /// Live order book implementation
    /// </summary>
    public class KrakenSymbolOrderBook : SymbolOrderBook
    {
        private readonly IKrakenSocketClient socketClient;
        private readonly bool _socketOwner;
        private bool initialSnapshotDone;

        /// <summary>
        /// Create a new order book instance
        /// </summary>
        /// <param name="symbol">The symbol the order book is for</param>
        /// <param name="limit">The initial limit of entries in the order book</param>
        /// <param name="options">Options for the order book</param>
        public KrakenSymbolOrderBook(string symbol, int limit, KrakenOrderBookOptions? options = null) : base(symbol, options ?? new KrakenOrderBookOptions())
        {
            socketClient = options?.SocketClient ?? new KrakenSocketClient(new KrakenSocketClientOptions
            {
                LogLevel = options?.LogLevel ?? LogLevel.Information
            });
            _socketOwner = options?.SocketClient == null;

            Levels = limit;
        }

        /// <inheritdoc />
        protected override async Task<CallResult<UpdateSubscription>> DoStartAsync()
        {
            var result = await socketClient.SubscribeToOrderBookUpdatesAsync(Symbol, Levels!.Value, ProcessUpdate).ConfigureAwait(false);
            if (!result)
                return result;

            Status = OrderBookStatus.Syncing;

            var setResult = await WaitForSetOrderBookAsync(10000).ConfigureAwait(false);
            return setResult ? result : new CallResult<UpdateSubscription>(null, setResult.Error);
        }

        /// <inheritdoc />
        protected override void DoReset()
        {
            initialSnapshotDone = false;
        }

        private void ProcessUpdate(DataEvent<KrakenStreamOrderBook> data)
        {
            if (!initialSnapshotDone)
            {
                var maxNumber = Math.Max(data.Data.Bids.Max(b => b.Sequence), data.Data.Asks.Max(b => b.Sequence));
                SetInitialOrderBook(maxNumber, data.Data.Bids, data.Data.Asks);
                initialSnapshotDone = true;
            }
            else
            {
                UpdateOrderBook(data.Data.Bids, data.Data.Asks);
                AddChecksum((int)data.Data.Checksum!);
            }
        }

        /// <inheritdoc />
        protected override bool DoChecksum(int checksum)
        {
            var checksumValues = new List<string>();
            for (var i = 0; i < 10; i++)
            {
                var ask = (KrakenStreamOrderBookEntry)asks.ElementAt(i).Value;
                checksumValues.Add(ToChecksumString(ask.RawPrice));
                checksumValues.Add(ToChecksumString(ask.RawQuantity));
            }
            for (var i = 0; i < 10; i++)
            {
                var bid = (KrakenStreamOrderBookEntry)bids.ElementAt(i).Value;
                checksumValues.Add(ToChecksumString(bid.RawPrice));
                checksumValues.Add(ToChecksumString(bid.RawQuantity));
            }

            var checksumString = string.Join("", checksumValues);
            var ourChecksumUtf = (int)Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(checksumString));

            if (ourChecksumUtf != checksum)
            {
                log.Write(LogLevel.Warning, $"Invalid checksum. Received from server: {checksum}, calculated local: {ourChecksumUtf}");
                return false;
            }

            return true;
        }

        private static string ToChecksumString(string value)
        {
            return value.Replace(".", "").TrimStart('0');
        }

        /// <inheritdoc />
        protected override async Task<CallResult<bool>> DoResyncAsync()
        {
            return await WaitForSetOrderBookAsync(10000).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public override void Dispose()
        {
            processBuffer.Clear();
            asks.Clear();
            bids.Clear();

            if(_socketOwner)
                socketClient?.Dispose();
        }
    }
}
