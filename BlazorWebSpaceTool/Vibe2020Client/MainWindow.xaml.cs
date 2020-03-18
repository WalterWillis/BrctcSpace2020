using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vibe2020Client.ViewModels;

namespace Vibe2020Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ADIS16460ViewModel ViewModel { get; set; }
        public MainWindow(IConfiguration configuration)
        {
            InitializeComponent();

            ViewModel = new ADIS16460ViewModel(configuration);
            DataContext = ViewModel;
            settingsButton.Click += (sender, e) => { ViewModel.GetSettings(); };
        }
    }
}
