using System;
using System.Collections.Generic;
using CsQuery.ExtensionMethods.Internal;
using Service.PoeTrade;
using Service.PathOfExile;

namespace Service {
    public static class Controller {
        private static readonly List<PoeTrade.Connection> Connections = new List<PoeTrade.Connection>();
        private static bool _isRunning;
        
        public static void Run(string[] urls) {
            if (urls.IsNullOrEmpty()) {
                throw new Exception("Invalid url list");
            }

            if (_isRunning) {
                throw new Exception("Already running");
            }

            foreach (var url in urls) {
                Connections.Add(new PoeTrade.Connection(url));
            }
            
            _isRunning = true;
        }

        public static void Stop() {
            if (!_isRunning) {
                throw new Exception("Not running");
            }
            
            foreach (var connection in Connections) {
                connection.DeleteSocket();
            }

            var allDone = false;
            while (!allDone) {
                foreach (var connection in Connections) {
                    if (connection.IsConnected()) {
                        allDone = false;
                        break;
                    }
                        
                    allDone = true;
                }
            }

            _isRunning = false;
        }
    }
}