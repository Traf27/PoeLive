using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using Domain;
using Domain.PathOfExile;
using Newtonsoft.Json;
using WebSocketSharp;

namespace Service.Connection {
    public sealed class PoeConnection : BaseConnection {
        public static readonly Regex UrlRegex =
            new Regex(@"^https?:\/\/www\.pathofexile\.com\/trade\/search\/([a-zA-Z0-9]+)\/([a-zA-Z0-9]+)$");

        private string _league;

        public PoeConnection(ConnectionInfo info) : base(info) {
            WsUrl = BuildWebSocketUrl();
        }
        
        public override void Connect() {
            CreateSocket();
            base.Connect();
        }

        public override string BuildWebSocketUrl() {
            // https://www.pathofexile.com/trade/search/Betrayal/xX7kHP
            var match = UrlRegex.Match(Info.Url);

            if (!match.Success || match.Groups.Count != 3) {
                throw new ArgumentException("Could not parse url");
            }

            _league = match.Groups[1].Value;
            Identifier = match.Groups[2].Value;

            return $"wss://www.pathofexile.com/api/trade/live/{_league}/{Identifier}";
        }

        public override async void DispatchSearchAsync(string[] ids = null) {
            if (ids == null) {
                throw new ArgumentException();
            }

            for (var i = 0; i <= ids.Length / 10; i++) {
                var subIds = ids.Skip(i * 10).Take(10);
                var concatIds = string.Join(",", subIds);

                PrintColorMsg(ConsoleColor.Blue, "New", concatIds);

                if (string.IsNullOrWhiteSpace(concatIds) || string.IsNullOrWhiteSpace(concatIds)) {
                    continue;
                }

                // Make POST request
                var jsonString = await AsyncRequest(concatIds);

                try {
                    var data = jsonString?.ParseJSON<ApiDeserializer>();
                    data?.result.ForEach(t => DispatchNewItem?.Invoke(Converter.ConvertPoe(t)));
                } catch (JsonSerializationException e) {
                    Console.WriteLine(jsonString);
                    Console.WriteLine(e);
                }
            }
        }

        protected override void SocketOnMessage(object sender, MessageEventArgs e) {
            var msg = e.Data.ParseJSON<WsDeserializer>();

            if (!msg.@new.IsNullOrEmpty()) {
                msg.@new.ForEach(t => DispatchDelItem?.Invoke(t));
                DispatchSearchAsync(msg.@new);
            }
        }

        public override async Task<string> AsyncRequest(string concatIds) {
            var url = $"https://www.pathofexile.com/api/trade/fetch/{concatIds}?query={Identifier}";

            try {
                var response = await WebClient.GetAsync(url);
                return await response.Content.ReadAsStringAsync();
            } catch {
                return null;
            }
        }
    }
}