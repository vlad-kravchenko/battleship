using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Client
{
    public class GameClient
    {
        private HubConnection _connection;
        private IHubProxy _proxy;
        private string user;
        private Thread UIthread;

        public delegate void OnReceiveResult(string result, int row, int col);
        public delegate void OnReceiveShot(int row, int col);
        public delegate void OnWriteLog(string message);
        public OnReceiveResult ReceiveResult;
        public OnReceiveShot ReceiveShot;
        public OnWriteLog WriteLog;

        public GameClient(string user, Thread thread)
        {
            this.user = user;
            this.UIthread = thread;
            _connection = new HubConnection("http://localhost:15552/");
            _proxy = _connection.CreateHubProxy("BattleshipHub");
            _connection.Headers.Add("Username", user);
        }

        public bool StartClient()
        {
            ConfigProxy();
            try
            {
                _connection.Start().Wait(10000);
                //_connection.Start().GetAwaiter().GetResult(); - better than just Wait() but the best is not to use them at all
                if (_connection.State != ConnectionState.Connected)
                    throw new TimeoutException();
                _proxy.Invoke("Login", user);
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        private void ConfigProxy()
        {
            _proxy.On<string>("SendShot", (message) =>
            {
                Dispatcher.FromThread(UIthread).BeginInvoke((Action)(() =>
                {
                    List<string> strs = message.Split(' ').ToList();
                    WriteLog(strs[0] + " " + strs[1] + " " + strs[2]);
                    ReceiveShot(Convert.ToInt32(strs[1]), Convert.ToInt32(strs[2]));
                }));
            });
            _proxy.On<string>("SendResult", (message) =>
            {
                Dispatcher.FromThread(UIthread).BeginInvoke((Action)(() =>
                {
                    List<string> strs = message.Split(' ').ToList();
                    WriteLog(strs[1]);
                    ReceiveResult(strs[1], Convert.ToInt32(strs[2]), Convert.ToInt32(strs[3]));
                }));
            });
            _proxy.On<string>("UserConnected", (message) =>
            {
                MessageBox.Show(message + " entered the game.");
            });
        }

        public void SendShot(int row, int col)
        {
            WriteLog(user + " " + row + " " + col);
            _proxy.Invoke("Shot", user + " " + row + " " + col);
        }

        public void SendResult(string result, int row, int col)
        {
            WriteLog(result);
            _proxy.Invoke("Result", user + " " + result + " " + row + " " + col);
        }

        public void Disconnect()
        {
            _connection.Stop();
        }
    }
}
