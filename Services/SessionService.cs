using MicroPanelAvalonia.Models;
using System;

namespace MicroPanelAvalonia.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        public ServerInfo? CurrentServer { get; private set; }
        public ServerUser? CurrentUser { get; private set; }
        public string? Token { get; private set; }

        public event EventHandler? SessionStarted;
        public event EventHandler? SessionEnded;

        public void StartSession(ServerInfo server, ServerUser user, string token)
        {
            CurrentServer = server;
            CurrentUser = user;
            Token = token;
            SessionStarted?.Invoke(this, EventArgs.Empty);
        }

        public void EndSession()
        {
            CurrentServer = null;
            CurrentUser = null;
            Token = null;
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateToken(string token)
        {
            Token = token;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
    }
}
