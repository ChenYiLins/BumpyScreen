using System.Windows.Input;
using BumpyScreen.Utils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BumpyScreen.ViewModels;

public partial class TrayIconViewModel : ObservableRecipient
{
    [ObservableProperty]
    private bool _isStartWithWindows;

    [ObservableProperty]
    private bool _isHorizontal;
    [ObservableProperty]
    private bool _isRetroflexHorizontal;
    [ObservableProperty]
    private bool _isVertical;
    [ObservableProperty]
    private bool _isRetroflexVertical;

    public ICommand SwitchScreenDirectionCommand
    {
        get;
    }

    public ICommand CloseCommand
    {
        get;
    }


    public TrayIconViewModel()
    {
        switch (Display.GetDisplayOrientation(1))
        {
            case Display.DisplayRotation.Rotate0:
                IsHorizontal = true;
                break;
            case Display.DisplayRotation.Rotate90:
                IsVertical = true;
                break;
            case Display.DisplayRotation.Rotate180:
                IsRetroflexVertical = true;
                break;
            case Display.DisplayRotation.Rotate270:
                IsRetroflexHorizontal = true;
                break;
        }

        SwitchScreenDirectionCommand = new RelayCommand<string>((parm) =>
        {
            switch (parm)
            {
                case "Horizontal":
                    Display.Rotate(1, Display.DisplayRotation.Rotate0);
                    break;

                case "RetroflexHorizontal":
                    Display.Rotate(1, Display.DisplayRotation.Rotate270);
                    break;

                case "Vertical":
                    Display.Rotate(1, Display.DisplayRotation.Rotate90);
                    break;

                case "RetroflexVertical":
                    Display.Rotate(1, Display.DisplayRotation.Rotate180);
                    break;
            }
        });

        CloseCommand = new RelayCommand(() =>
        {
            App.Current.Exit();
        });
    }
}
