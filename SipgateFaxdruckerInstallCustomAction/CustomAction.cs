using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;

namespace SipgateFaxdruckerInstallCustomAction
{
    /// <summary>
    /// Lotsa notes from here:
    /// http://stackoverflow.com/questions/835624/how-do-i-pass-msiexec-properties-to-a-wix-c-sharp-custom-action
    /// </summary>
    public class CustomActions
    {


        [CustomAction]
        public static ActionResult CheckIfPrinterNotInstalled(Session session)
        {
            ActionResult resultCode;
            SessionLogWriterTraceListener installTraceListener = new SessionLogWriterTraceListener(session);
            SipgateFaxdruckerInstaller installer = new SipgateFaxdruckerInstaller();
            installer.AddTraceListener(installTraceListener);
            try
            {
                resultCode = installer.IsSipgateFaxdruckerPrinterInstalled() ? ActionResult.Success : ActionResult.Failure;
            }
            finally
            {
                installTraceListener.Dispose();
            }

            return resultCode;
        }


        [CustomAction]
        public static ActionResult InstallSipgateFaxdruckerPrinter(Session session)
        {
            ActionResult printerInstalled;

            String driverSourceDirectory = session.CustomActionData["DriverSourceDirectory"];
            String outputCommand = session.CustomActionData["OutputCommand"];
            String outputCommandArguments = session.CustomActionData["OutputCommandArguments"];

            SessionLogWriterTraceListener installTraceListener = new SessionLogWriterTraceListener(session)
            {
                TraceOutputOptions = TraceOptions.DateTime
            };

            SipgateFaxdruckerInstaller installer = new SipgateFaxdruckerInstaller();
            installer.AddTraceListener(installTraceListener);
            try
            {
                printerInstalled = installer.InstallSipgateFaxdruckerPrinter(driverSourceDirectory,
                    outputCommand,
                    outputCommandArguments) ? ActionResult.Success : ActionResult.Failure;

                installTraceListener.CloseAndWriteLog();
            }
            finally
            {
                if (installTraceListener != null)
                    installTraceListener.Dispose();

            }
            return printerInstalled;
        }


        [CustomAction]
        public static ActionResult UninstallSipgateFaxdruckerPrinter(Session session)
        {
            ActionResult printerUninstalled;

            SessionLogWriterTraceListener installTraceListener = new SessionLogWriterTraceListener(session)
            {
                TraceOutputOptions = TraceOptions.DateTime
            };

            SipgateFaxdruckerInstaller installer = new SipgateFaxdruckerInstaller();
            installer.AddTraceListener(installTraceListener);
            try
            {
                printerUninstalled = installer.UninstallSipgateFaxdruckerPrinter() ? ActionResult.Success : ActionResult.Failure;
                installTraceListener.CloseAndWriteLog();
            }
            finally
            {
                installTraceListener.Dispose();
            }
            return printerUninstalled;
        }
    }
}
