using Avalonia.Controls;
using Avalonia.Input;

namespace PSTManager.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var pos = e.GetPosition(this);
        if (pos.Y < 49)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }
}
