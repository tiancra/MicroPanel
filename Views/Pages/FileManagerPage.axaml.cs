using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MicroPanelAvalonia.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MicroPanelAvalonia.Views.Pages
{
    public partial class FileManagerPage : UserControl
    {
        private readonly Services.FileService _fileService;
        private ObservableCollection<FileItem> _files = new();
        private string _currentPath = "0";

        public FileManagerPage()
        {
            InitializeComponent();
            _fileService = new Services.FileService();
            
            Loaded += OnLoaded;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            var dataGrid = this.FindControl<DataGrid>("FilesDataGrid");
            if (dataGrid != null)
            {
                dataGrid.ItemsSource = _files;
                dataGrid.DoubleTapped += OnFileDoubleTapped;
            }
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                await LoadDirectoryAsync("0");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 加载目录失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 加载目录内容
        /// </summary>
        private async Task LoadDirectoryAsync(string path)
        {
            try
            {
                _currentPath = path ?? "0";
                
                var pathTextBox = this.FindControl<TextBox>("PathTextBox");
                if (pathTextBox != null)
                {
                    pathTextBox.Text = _currentPath;
                }

                var response = await _fileService.ListDirectoryAsync(_currentPath);
                if (response?.Code == 200 && response.Data?.Children != null)
                {
                    _files.Clear();
                    
                    // 先添加目录，再添加文件
                    var dirs = response.Data.Children.Where(c => c.Type == "directory").OrderBy(c => c.Name);
                    var files = response.Data.Children.Where(c => c.Type == "file").OrderBy(c => c.Name);
                    
                    foreach (var dir in dirs)
                    {
                        _files.Add(dir);
                    }
                    
                    foreach (var file in files)
                    {
                        _files.Add(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 加载目录失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 返回上级目录
        /// </summary>
        private async void OnGoBackClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentPath == "0") return;
                
                var lastSepIndex = _currentPath.LastIndexOf('/');
                if (lastSepIndex == -1)
                {
                    lastSepIndex = _currentPath.LastIndexOf('\\');
                }
                
                if (lastSepIndex > 0)
                {
                    var parentPath = _currentPath.Substring(0, lastSepIndex);
                    await LoadDirectoryAsync(parentPath);
                }
                else
                {
                    await LoadDirectoryAsync("0");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 返回上级目录失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 前往指定路径
        /// </summary>
        private async void OnGoClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var pathTextBox = this.FindControl<TextBox>("PathTextBox");
                if (pathTextBox != null)
                {
                    await LoadDirectoryAsync(pathTextBox.Text ?? "0");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 前往路径失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 路径文本框按键
        /// </summary>
        private async void OnPathTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var pathTextBox = sender as TextBox;
                    if (pathTextBox != null)
                    {
                        await LoadDirectoryAsync(pathTextBox.Text ?? "0");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 路径输入失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 文件双击
        /// </summary>
        private async void OnFileDoubleTapped(object? sender, TappedEventArgs e)
        {
            try
            {
                var dataGrid = sender as DataGrid;
                if (dataGrid?.SelectedItem is FileItem item)
                {
                    if (item.Type == "directory")
                    {
                        await LoadDirectoryAsync(item.Path);
                    }
                    else
                    {
                        // 打开文件编辑器
                        await OpenFileEditorAsync(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 打开文件失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 打开文件编辑器
        /// </summary>
        private async Task OpenFileEditorAsync(FileItem item)
        {
            try
            {
                // TODO: 打开文件编辑器
                System.Diagnostics.Debug.WriteLine($"打开文件: {item.Path}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 打开文件编辑器失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 新建选择变化
        /// </summary>
        private async void OnCreateSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            try
            {
                var comboBox = sender as ComboBox;
                if (comboBox?.SelectedItem is ComboBoxItem item)
                {
                    var type = item.Content?.ToString();
                    if (type == "文件")
                    {
                        await CreateNewFileAsync();
                    }
                    else if (type == "目录")
                    {
                        await CreateNewDirectoryAsync();
                    }
                    
                    comboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 创建失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 创建新文件
        /// </summary>
        private async Task CreateNewFileAsync()
        {
            try
            {
                // TODO: 显示对话框输入文件名
                var fileName = "newfile.txt";
                var path = _currentPath == "0" ? fileName : $"{_currentPath}/{fileName}";
                
                var response = await _fileService.CreateFileAsync(path);
                if (response?.Code == 200)
                {
                    await LoadDirectoryAsync(_currentPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 创建文件失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 创建新目录
        /// </summary>
        private async Task CreateNewDirectoryAsync()
        {
            try
            {
                // TODO: 显示对话框输入目录名
                var dirName = "newfolder";
                var path = _currentPath == "0" ? dirName : $"{_currentPath}/{dirName}";
                
                var response = await _fileService.CreateDirectoryAsync(path);
                if (response?.Code == 200)
                {
                    await LoadDirectoryAsync(_currentPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 创建目录失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 上传按钮点击
        /// </summary>
        private void OnUploadClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: 实现文件上传
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 上传失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 刷新按钮点击
        /// </summary>
        private async void OnRefreshClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                await LoadDirectoryAsync(_currentPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 刷新失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索文本框按键
        /// </summary>
        private async void OnSearchTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    var searchTextBox = sender as TextBox;
                    if (searchTextBox != null)
                    {
                        await SearchFilesAsync(searchTextBox.Text ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 搜索失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索文件
        /// </summary>
        private async Task SearchFilesAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    await LoadDirectoryAsync(_currentPath);
                    return;
                }

                var response = await _fileService.SearchFilesAsync(_currentPath, keyword);
                if (response?.Code == 200 && response.Data?.Children != null)
                {
                    _files.Clear();
                    foreach (var item in response.Data.Children)
                    {
                        _files.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FileManagerPage: 搜索文件失败 - {ex.Message}");
            }
        }
    }
}
