using System.Reactive.Linq;
using ReactiveUI;

namespace WpfRxDemo
{
    public partial class MainWindow
    {
        private MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            ViewModel = new MainWindowViewModel(this);

            InitializeComponent();
            
            DataContext = ViewModel;
        }
    }
}
