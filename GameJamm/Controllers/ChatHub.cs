using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace GameJamm.Controllers
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<string, string> ConnectionUserNamesMap = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, int> ConnectionPointsMap = new ConcurrentDictionary<string, int>();
        public static HashSet<int> CoinLocations = new HashSet<int>();
        public static Random Random = new Random();

        public void Send(string name, string message)
        {
            Clients.All.sendMessage(name, message);
        }

        [HubMethodName("getCoin")]
        public void GetCoin(int index)
        {
            string id = Context.ConnectionId;
            int points = CoinLocations.RemoveWhere(x => x == index);
            if (points == 0)
            {
                return;
            }
            int currentPoints;
            if (ConnectionPointsMap.TryRemove(id, out currentPoints))
            {
                ConnectionPointsMap.TryAdd(id, currentPoints + points * 2);
                Clients.All.removecoin(index);

                string username;
                ConnectionUserNamesMap.TryGetValue(id, out username);
                SendServerMessage(username + " has won Two coins!");
                DisplayAllUsersAndPoints();
            }
        }

        [HubMethodName("putCoin")]
        public void PutCoin()
        {
            string id = Context.ConnectionId;
            int currentPoints;
            if (ConnectionPointsMap.TryRemove(id, out currentPoints))
            {
                if (currentPoints > 0)
                {
                    if (AddCoin())
                    {
                        ConnectionPointsMap.TryAdd(id, currentPoints - 1);
                        DisplayAllUsersAndPoints();
                        return;
                    }
                }
                ConnectionPointsMap.TryAdd(id, currentPoints);
            }
        }

        private void SendServerMessage(string message)
        {
            Clients.All.sendServerMessage(message);
        }

        private void SendJoinLeaveMessage(string message)
        {
            Clients.All.sendJoinLeaveMessage(message);
        }

        private bool AddCoin()
        {
            int index = 0;
            bool added = false;

            if (CoinLocations.Count > 5)
            {
                return false;
            }

            while (!added)
            {
                index = Random.Next(1, 36);
                added = CoinLocations.Add(index);
            }
            Clients.All.addcoin(index);
            return true;
        }


        [HubMethodName("connect")]
        public void Connect(string username)
        {
            string id = Context.ConnectionId;
            ConnectionUserNamesMap.TryAdd(id, username);
            ConnectionPointsMap.TryAdd(id, 2);
            Clients.Caller.getCurrentCoins(CoinLocations);
            DisplayAllUsersAndPoints();
            SendJoinLeaveMessage(username + " has Joined the game!");
        }

        private async Task DisplayAllUsersAndPoints()
        {
            List<string> points = new List<string>();
            foreach (KeyValuePair<string, int> entry in ConnectionPointsMap)
            {
                string username;
                ConnectionUserNamesMap.TryGetValue(entry.Key, out username);
                if (username != null)
                {
                    points.Add(username + ": " + entry.Value);
                }
            }
            await Clients.All.showPoints(points);
        }

        public override async Task OnDisconnected(bool stopCalled)
        {
            string id = Context.ConnectionId;
            string username;
            ConnectionUserNamesMap.TryRemove(id, out username);
            int points;
            ConnectionPointsMap.TryRemove(username, out points);
            DisplayAllUsersAndPoints();
            SendJoinLeaveMessage(username + " has left the game!");
            await base.OnDisconnected(stopCalled);
        }
    }
}