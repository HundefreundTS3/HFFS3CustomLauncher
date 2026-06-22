using System.Windows;
using System.Windows.Input;

namespace HFFS3CustomLauncher
{
    /// <summary>
    /// Interaktionslogik für DialogueWindow.xaml
    /// </summary>
    public partial class DialogueWindow : Window
    {
        internal DialogueWindow(Window owner, DialogueLogic logic)
        {
            InitializeComponent();
            Owner = owner;
            DataContext = logic;
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(logic.DS.Language + "-" + logic.DS.Country);

            if (logic.DS.IsWindowsXP)
            {
                AllowsTransparency = false;
            }
        }

        private void OnMoveWindow(object sender, RoutedEventArgs e)
        {
            if (e is MouseButtonEventArgs mouseEventArgs && mouseEventArgs.LeftButton == MouseButtonState.Pressed && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnClickDialogueYes(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void OnClickDialogueNo(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnClickDialogueOK(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
