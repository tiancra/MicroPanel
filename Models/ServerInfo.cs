using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MicroPanelAvalonia.Models
{
    public class ServerInfo : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _serverAddress = string.Empty;
        private string _serverName = string.Empty;
        private List<ServerUser> _users = new();
        private SystemStatus? _status;
        private bool _isOnline;
        private DateTime _lastUpdate;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set => SetProperty(ref _serverAddress, value);
        }

        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        public List<ServerUser> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public SystemStatus? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            set => SetProperty(ref _lastUpdate, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class ServerUser : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string? _token;
        private DateTime? _tokenExpiry;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string? Token
        {
            get => _token;
            set => SetProperty(ref _token, value);
        }

        public DateTime? TokenExpiry
        {
            get => _tokenExpiry;
            set => SetProperty(ref _tokenExpiry, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class SystemStatus : INotifyPropertyChanged
    {
        private string _cpuInfo = string.Empty;
        private string _ramInfo = string.Empty;
        private string _diskSizeInfo = string.Empty;

        public string CpuInfo
        {
            get => _cpuInfo;
            set => SetProperty(ref _cpuInfo, value);
        }

        public string RamInfo
        {
            get => _ramInfo;
            set => SetProperty(ref _ramInfo, value);
        }

        public string DiskSizeInfo
        {
            get => _diskSizeInfo;
            set => SetProperty(ref _diskSizeInfo, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
