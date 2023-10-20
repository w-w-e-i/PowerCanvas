using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for ChangeLogWindow.xaml
    /// </summary>
    public partial class ChangeLogWindow : Window
    {
        public ChangeLogWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
            Version version = Assembly.GetExecutingAssembly().GetName().Version;



        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HyperlinkSource_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/WXRIW/Ink-Canvas");
        }
        private void HyperlinkHome_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://wenzhao.top/");
        }
    }
}
