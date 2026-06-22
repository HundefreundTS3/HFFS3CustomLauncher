using System.Windows;

namespace HFFS3CustomLauncher
{
    public partial class InvisibleWindow : Window
    {
        internal enum Operation
        {
            TOP_LEFT,
            TOP,
            TOP_RIGHT,
            LEFT,
            RIGHT,
            BOTTOM_LEFT,
            BOTTOM,
            BOTTOM_RIGHT
        }

        internal Operation SelectedOperation { get; private set; }

        internal InvisibleWindow(Window owner, Operation operation, bool isWindowsXP)
        {
            InitializeComponent();
            Owner = owner;
            SelectedOperation = operation;

            switch (operation)
            {
                case Operation.TOP_LEFT:
                    Border_Left.Visibility = Visibility.Visible;
                    Border_Top.Visibility = Visibility.Visible;
                    break;
                case Operation.TOP:
                    Border_Top.Visibility = Visibility.Visible;
                    break;
                case Operation.TOP_RIGHT:
                    Border_Top.Visibility = Visibility.Visible;
                    Border_Right.Visibility = Visibility.Visible;
                    break;
                case Operation.LEFT:
                    Border_Left.Visibility = Visibility.Visible;
                    break;
                case Operation.RIGHT:
                    Border_Right.Visibility = Visibility.Visible;
                    break;
                case Operation.BOTTOM_LEFT:
                    Border_Bottom.Visibility = Visibility.Visible;
                    Border_Left.Visibility = Visibility.Visible;
                    break;
                case Operation.BOTTOM:
                    Border_Bottom.Visibility = Visibility.Visible;
                    break;
                case Operation.BOTTOM_RIGHT:
                    Border_Right.Visibility = Visibility.Visible;
                    Border_Bottom.Visibility = Visibility.Visible;
                    break;
            }

            if (isWindowsXP)
            {
                Border_Left.Margin = new Thickness(-10, -10, 0, -10);
                Border_Left.CornerRadius = new CornerRadius(0);
                Border_Top.Margin = new Thickness(-10, -10, -10, 0);
                Border_Top.CornerRadius = new CornerRadius(0);
                Border_Right.Margin = new Thickness(0, -10, -10, -10);
                Border_Right.CornerRadius = new CornerRadius(0);
                Border_Bottom.Margin = new Thickness(-10, 0, -10, -10);
                Border_Bottom.CornerRadius = new CornerRadius(0);
            }
        }

        internal bool IgnoreVerticalOperation
        {
            get
            {
                switch (SelectedOperation)
                {
                    case Operation.LEFT:
                    case Operation.RIGHT:
                        return true;
                    default:
                        return false;
                }
            }
        }

        internal bool IgnoreHorizontalOperation
        {
            get
            {
                switch (SelectedOperation)
                {
                    case Operation.TOP:
                    case Operation.BOTTOM:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
