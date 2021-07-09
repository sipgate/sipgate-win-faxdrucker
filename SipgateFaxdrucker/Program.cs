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

            FaxDruckerUtils.InitializeTraceSource();

#if DEBUG
            FaxDruckerUtils.faxdruckerTraceSource.Switch.Level = SourceLevels.All;
#endif

            FaxDruckerUtils.LogInformation("Started Programm");

            using (SipgateForm sipgateForm = new SipgateForm())
            {
                sipgateForm.ShowDialog();
                sipgateForm.Close();
            }

            FaxDruckerUtils.LogInformation("Exited Programm");
            FaxDruckerUtils.faxdruckerTraceSource.Close();
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
            FaxDruckerUtils.LogCritical($"Unhandled exception: {uex.ExceptionObject} {exception?.GetType()}");
            FaxDruckerUtils.faxdruckerTraceSource.Close();

            ErrorDialog errorDialog = new ErrorDialog(FaxDruckerUtils.faxdruckerTraceSource);
            errorDialog.HandleUnhandledException(uex);

        }
    }
}