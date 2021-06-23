using System;
using System.Diagnostics;

namespace SipgateFaxdrucker
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += Application_UnhandledException;

            Utils.InitializeTraceSource();

#if DEBUG
            Utils.faxdruckerTraceSource.Switch.Level = SourceLevels.All;
#endif

            Utils.LogInformation("Started Programm");

            using (SipgateForm sipgateForm = new SipgateForm()) 
            {
                sipgateForm.ShowDialog();
                sipgateForm.Close();
            }

            Utils.LogInformation("Exited Programm");      
            Utils.faxdruckerTraceSource.Close();
        }


        /// <summary>
        /// All unhandled exceptions will bubble their way up here -
        /// a final error dialog will be displayed before the crash and burn
        /// </summary>
        /// <param name="sender"></param>pack
        /// <param name="uex"></param>
        static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs uex)
        {
            Exception exception = uex.ExceptionObject as Exception;
            Utils.LogCritical($"Unhandled exception: {uex.ExceptionObject} {exception?.GetType()}");
            Utils.faxdruckerTraceSource.Close();

            ErrorDialog errorDialog = new ErrorDialog(Utils.faxdruckerTraceSource);
            errorDialog.HandleUnhandledException(uex);

        }
    }        
}