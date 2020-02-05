using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace Server
{
    public class BattleshipHub : Hub
    {
        public async Task Shot(string message)
        {
            await Clients.Others.SendShot(message);
        }

        public async Task Result(string message)
        {
            await Clients.Others.SendResult(message);
        }

        public async Task Login(string user)
        {
            string userName = Context.Headers["Username"];
            
            await Groups.Add(Context.ConnectionId, "Game1");

            await Clients.Others.UserConnected(user);
        }
    }
}