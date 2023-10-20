using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// WelcomeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        public WelcomeWindow()
        {
            InitializeComponent();


        }

        public static bool IsNewBuilding = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

       
       
     
    }
}
