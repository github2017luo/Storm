﻿using System.Windows;

namespace Storm
{
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.LogException(e.Exception);
        }
    }
}
