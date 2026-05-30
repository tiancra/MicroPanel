using MicroPanel.Models;
using System;
using System.Collections.Generic;

namespace MicroPanel.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        public ServerInfo? CurrentServer { get; private set; }
        public ServerUser? CurrentUser { get; private set; }
        public string? Token { get; private set; }
        public List<string>? UserRoutes { get; private set; }

        public event EventHandler? SessionStarted;
        public event EventHandler? SessionEnded;

        public void StartSession(ServerInfo server, ServerUser user, string token, List<string>? routes = null)
        {
            CurrentServer = server;
            CurrentUser = user;
            Token = token;
            UserRoutes = routes;
            SessionStarted?.Invoke(this, EventArgs.Empty);
        }

        public void EndSession()
        {
            CurrentServer = null;
            CurrentUser = null;
            Token = null;
            UserRoutes = null;
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateToken(string token)
        {
            Token = token;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
    }
}
