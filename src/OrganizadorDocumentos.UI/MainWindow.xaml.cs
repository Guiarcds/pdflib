using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using OrganizadorDocumentos.UI.ViewModels;
using OrganizadorDocumentos.UI.Views;

namespace OrganizadorDocumentos.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly VisaoGeralView _visaoGeralView;
    private readonly ProcessamentoView _processamentoView;
    private readonly RevisaoView _revisaoView;
    private readonly ConfiguracaoView _configuracaoView;

    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystembackdropType = 38;
    private const int DwmsbtabMainWindow = 3;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Win32Point lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct Win32Point
    {
        public int X;
        public int Y;
    }

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;

        _visaoGeralView = new VisaoGeralView { DataContext = _viewModel.VisaoGeralViewModel };
        _processamentoView = new ProcessamentoView { DataContext = _viewModel.ProcessamentoViewModel };
        _revisaoView = new RevisaoView { DataContext = _viewModel.RevisaoViewModel };
        _configuracaoView = new ConfiguracaoView { DataContext = _viewModel.ConfiguracaoViewModel };

        _viewModel.NavegacaoSolicitada += OnNavegacaoSolicitada;
        _viewModel.NavegarVisaoGeralCommand.Execute(null);

        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        SourceInitialized -= OnSourceInitialized;
        TryEnableAcrylicBackdrop();
    }

    private void TryEnableAcrylicBackdrop()
    {
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Window handle not available");

            int darkMode = 1;
            int backdropType = DwmsbtabMainWindow;

            int hr1 = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));
            int hr2 = DwmSetWindowAttribute(handle, DwmwaSystembackdropType, ref backdropType, sizeof(int));

            if (hr1 != 0 || hr2 != 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        catch
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x13, 0x2B));
        }
    }

    private void OnNavegacaoSolicitada(object? sender, string titulo)
    {
        ContentArea.Content = titulo switch
        {
            "Visão Geral" => _visaoGeralView,
            "Processamento" => _processamentoView,
            "Revisão" => _revisaoView,
            "Configurações" => _configuracaoView,
            _ => _visaoGeralView
        };
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.MinimizeWindow(this);
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        SystemCommands.CloseWindow(this);
    }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                if (WindowState == WindowState.Maximized)
                {
                    // Get the current mouse position in screen coordinates
                    GetCursorPos(out Win32Point mouseScreen);
                    // Get the mouse position relative to the title bar grid
                    Point offsetInTitleBar = e.GetPosition(TitleBarGrid);

                    // Restore the window
                    WindowState = WindowState.Normal;

                    // Set the window's position so that the mouse pointer remains at the same relative point in the title bar
                    Left = (double)mouseScreen.X - offsetInTitleBar.X;
                    Top = (double)mouseScreen.Y - offsetInTitleBar.Y;

                    // Now start the drag operation
                    DragMove();
                }
                else
                {
                    DragMove();
                }
            }
        }
}
