﻿using AutoUpdaterDotNET;
using Ink_Canvas.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace Ink_Canvas
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        

        System.Threading.Mutex mutex;
        public static string[] StartArgs = null;
        public static string RootPath = Environment.GetEnvironmentVariable("APPDATA") + "\\Ink Canvas\\";

        public App()
        {
            this.Startup += new StartupEventHandler(App_Startup);
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Ink_Canvas.MainWindow.ShowNewMessage("抱歉，出现未预期的异常，可能导致 PowerCanvas 画板运行不稳定。\n建议保存墨迹后重启应用。", true);
            LogHelper.NewLog(e.Exception.ToString());
            e.Handled = true;
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            if (!StoreHelper.IsStoreApp) RootPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;

            LogHelper.NewLog(string.Format("Ink Canvas Starting (Version: {0})", Assembly.GetExecutingAssembly().GetName().Version.ToString()));

            bool ret;
            mutex = new System.Threading.Mutex(true, "Ink_Canvas", out ret);

            if (!ret && !e.Args.Contains("-m")) //-m multiple
            {
                LogHelper.NewLog("Detected existing instance");
                MessageBox.Show("你已经打开 PowerCanvas 了！");
                LogHelper.NewLog("Ink Canvas automatically closed");
                Environment.Exit(0);
            }

            StartArgs = e.Args;
            /*
            if (!StoreHelper.IsStoreApp)
            {
                AutoUpdater.Start($"http://ink.wxriw.cn:1957/update");
                AutoUpdater.ApplicationExitEvent += () =>
                {
                    Environment.Exit(0);
                };
            } */
        }
    }
}
