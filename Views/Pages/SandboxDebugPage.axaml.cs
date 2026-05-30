using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MicroPanel.Views.Pages
{
    public partial class SandboxDebugPage : UserControl
    {
        public SandboxDebugPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
