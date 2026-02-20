using GameTimeNext.Core.Application.General.Controller;
using GameTimeNext.Core.Framework.UI.Base;
using System.Windows;

namespace GameTimeNext
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : GTNWindow
    {

        MainWindowController? _mainWindowController;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void InitializeViewOutput(object sender, RoutedEventArgs e)
        {
            // Controller
            _mainWindowController = new MainWindowController();
            SetController(_mainWindowController);
        }
    }
}