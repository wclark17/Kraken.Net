﻿using System.Net.Http;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Kraken.Net.Interfaces;

namespace Kraken.Net
{
    /// <summary>
    /// Options for the Kraken client
    /// </summary>
    public class KrakenClientOptions : RestClientOptions
    {
        /// <summary>
        /// The static password configured as two-factor authentication for the API key. Will be send as otp parameter on private requests.
        /// </summary>
        public string? StaticTwoFactorAuthenticationPassword { get; set; }

        /// <summary>
        /// Optional nonce provider for signing requests. Careful providing a custom provider; once a nonce is sent to the server, every request after that needs a higher nonce than that
        /// </summary>
        public INonceProvider? NonceProvider { get; set; }

        /// <summary>
        /// Create new client options
        /// </summary>
        public KrakenClientOptions() : this(null, "https://api.kraken.com")
        {
        }

        /// <summary>
        /// Create new client options
        /// </summary>
        /// <param name="client">HttpClient to use for requests from this client</param>
        public KrakenClientOptions(HttpClient client) : this(client, "https://api.kraken.com")
        {
        }

        /// <summary>
        /// Create new client options
        /// </summary>
        /// <param name="apiAddress">Custom API address to use</param>
        /// <param name="client">HttpClient to use for requests from this client</param>
        public KrakenClientOptions(HttpClient? client, string apiAddress) : base(apiAddress)
        {
            HttpClient = client;
        }

        /// <summary>
        /// Copy this instance
        /// </summary>
        /// <returns></returns>
        public KrakenClientOptions Copy()
        {
            var copy = Copy<KrakenClientOptions>();
            copy.StaticTwoFactorAuthenticationPassword = StaticTwoFactorAuthenticationPassword;
            copy.NonceProvider = NonceProvider;
            return copy;
        }
    }

    /// <summary>
    /// Options for the Kraken socket client
    /// </summary>
    public class KrakenSocketClientOptions : SocketClientOptions
    {
        private string _authBaseAddress = "wss://ws-auth.kraken.com/";
        /// <summary>
        /// The base address for authenticated subscriptions
        /// </summary>
        public string AuthBaseAddress
        {
            get => _authBaseAddress;
            set
            {
                var newValue = value;
                if (!newValue.EndsWith("/"))
                    newValue += "/";
                _authBaseAddress = newValue;
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        public KrakenSocketClientOptions(): base("wss://ws.kraken.com")
        {
            SocketSubscriptionsCombineTarget = 10;
        }

        /// <summary>
        /// Copy this instance
        /// </summary>
        /// <returns></returns>
        public KrakenSocketClientOptions Copy()
        {
            var copy = Copy<KrakenSocketClientOptions>();
            copy.AuthBaseAddress = AuthBaseAddress;
            return copy;
        }
    }

    /// <summary>
    /// Options for the Kraken symbol order book
    /// </summary>
    public class KrakenOrderBookOptions : OrderBookOptions
    {
        /// <summary>
        /// The client to use for the socket connection. When using the same client for multiple order books the connection can be shared.
        /// </summary>
        public IKrakenSocketClient? SocketClient { get; }

        /// <summary>
        /// </summary>
        /// <param name="client">The client to use for the socket connection. When using the same client for multiple order books the connection can be shared.</param>
        public KrakenOrderBookOptions(IKrakenSocketClient? client = null) : base("Kraken", false, true)
        {
            SocketClient = client;
        }
    }
}

