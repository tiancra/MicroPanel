using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using FluentAvalonia.UI.Windowing;

namespace MicroPanel.Controls;

public partial class MyWindow : AppWindow
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PseudoClasses")]
    private static extern IPseudoClasses GetPseudoClasses(StyledElement element);

    private bool _enableMicaWindow;
    
    public static readonly DirectProperty<MyWindow, bool> EnableMicaWindowProperty = AvaloniaProperty.RegisterDirect<MyWindow, bool>(
        nameof(EnableMicaWindow), o => o.EnableMicaWindow, (o, v) => o.EnableMicaWindow = v);

    private bool _isMicaSupported;

    public static readonly DirectProperty<MyWindow, bool> IsMicaSupportedProperty = AvaloniaProperty.RegisterDirect<MyWindow, bool>(
        nameof(IsMicaSupported), o => o.IsMicaSupported, (o, v) => o.IsMicaSupported = v);

    public bool IsMicaSupported
    {
        get => _isMicaSupported;
        set => SetAndRaise(IsMicaSupportedProperty, ref _isMicaSupported, value);
    }

    
    public bool EnableMicaWindow
    {
        get => _enableMicaWindow;
        set => SetAndRaise(EnableMicaWindowProperty, ref _enableMicaWindow, value);
    }

    public static readonly AttachedProperty<MyWindowState?> StateProperty =
        AvaloniaProperty.RegisterAttached<MyWindow, Window, MyWindowState?>("State");

    internal static void SetState(Window obj, MyWindowState? value) => obj.SetValue(StateProperty, value);
    internal static MyWindowState? GetState(Window obj) => obj.GetValue(StateProperty);

    public static void SetupMyWindowExt(Window window)
    {
        var state = new MyWindowState();
        SetState(window, state);
        
        window.Initialized += (sender, e) =>
        {
            // Additional initialization
        };
        
        window.Loaded += (sender, e) =>
        {
            // Window loaded
        };
        
        RenderOptions.SetBitmapInterpolationMode(window, BitmapInterpolationMode.HighQuality);
        
        window.KeyDown += (sender, e) =>
        {
            // Debug functionality can be added here
        };
        
        window.PointerPressed += (sender, e) =>
        {
            // PointerStateAssist.SetIsTouchMode(window, state.SuppressTouchMode || e.Pointer.Type == PointerType.Touch);
        };
        
        window.Closed += (sender, e) =>
        {
            // Cleanup if needed
        };
    }

    public MyWindow()
    {
        IsMicaSupported = CheckMicaSupport();
        Loaded += OnLoaded;
        SetupMyWindowExt(this);
    }

    private static bool CheckMicaSupport()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return false;
            
            var version = Environment.OSVersion.Version;
            if (version.Major < 10)
                return false;
            
            if (version.Build < 22000)
                return false;
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (EnableMicaWindow && IsMicaSupported)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            Background = Brushes.Transparent;
        }
    }

    public class MyWindowState
    {
        public bool IsAdornerAdded { get; set; }
        public bool EnableMicaWindow { get; set; }
        public int DebugGraphState { get; set; }
        public bool SuppressTouchMode { get; set; }
    }
}
