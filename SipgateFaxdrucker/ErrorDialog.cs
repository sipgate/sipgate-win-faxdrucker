using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SipgateFaxdrucker
{
    public class ErrorDialog
    {
        #region Message constants

        const string ErrorDialogInstructionPdfGeneration = "There was a PDF generation error.";
        const string ErrorDialogInstructionCouldNotWrite = "Could not create the output file.";
        const string ErrorDialogInstructionUnexpectedError = "There was an internal error. Enable tracing for details.";
        const string ErrorDialogTextGhostScriptConversion = "Ghostscript error code {0}.";
        const string ErrorDialogCaption = "sipgate Faxdrucker";

        const string ErrorDialogOutputFilenameInvalid =
            "Output file path is not valid. Check the \"OutputFile\" setting in the config file.";

        const string ErrorDialogOutputFilenameTooLong =
            "Output file path too long. Check the \"OutputFile\" setting in the config file.";

        const string ErrorDialogOutputFileAccessDenied = "Access denied - check permissions on output folder.";

        const string WarnFileNotDeleted = "{0} could not be deleted.";

        #endregion

        private TraceSource _logEventSource;

        public ErrorDialog(TraceSource logEventSource)
        {
            this._logEventSource = logEventSource;
        }

        public void Show(Exception e)
        {
            if (e.InnerException is ArgumentException || e.InnerException is NotSupportedException ||
                e.InnerException is DirectoryNotFoundException)
            {
                {
                    _logEventSource.TraceEvent(TraceEventType.Error,
                        (int) TraceEventType.Error,
                        ErrorDialogOutputFilenameInvalid + Environment.NewLine +
                        "Exception message: " + e.Message);
                    DisplayErrorMessage(ErrorDialogCaption,
                        ErrorDialogOutputFilenameInvalid);
                }
            }
            else
            {
                DisplayErrorMessage(ErrorDialogCaption, "General Error: " + e);
            }
        }

        public void Show(ExternalException ghostscriptEx)
        {
            // Ghostscript error
            _logEventSource.TraceEvent(TraceEventType.Error,
                (int) TraceEventType.Error,
                String.Format(ErrorDialogTextGhostScriptConversion, ghostscriptEx.ErrorCode.ToString()) +
                Environment.NewLine +
                "Exception message: " + ghostscriptEx.Message);
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogInstructionPdfGeneration + Environment.NewLine +
                String.Format(ErrorDialogTextGhostScriptConversion, ghostscriptEx.ErrorCode.ToString()));
        }

        public void Show(PathTooLongException ex)
        {
            // filename is greater than 260 characters
            _logEventSource.TraceEvent(TraceEventType.Error,
                (int) TraceEventType.Error,
                ErrorDialogOutputFilenameTooLong + Environment.NewLine +
                "Exception message: " + ex.Message);
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogOutputFilenameTooLong);
        }

        public void Show(UnauthorizedAccessException ex)
        {
            _logEventSource.TraceEvent(TraceEventType.Error,
                (int) TraceEventType.Error,
                ErrorDialogOutputFileAccessDenied + Environment.NewLine +
                "Exception message: " + ex.Message);
            // Can't write to target dir
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogOutputFileAccessDenied);
        }

        public void Show(IOException ioEx, String outputFilename)
        {
            // We couldn't delete, or create a file
            // because it was in use
            _logEventSource.TraceEvent(TraceEventType.Error,
                (int) TraceEventType.Error,
                ErrorDialogInstructionCouldNotWrite +
                Environment.NewLine +
                "Exception message: " + ioEx.Message);
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogInstructionCouldNotWrite + Environment.NewLine +
                $"{outputFilename} is in use.");
        }

        public void Show(UnauthorizedAccessException unauthorizedEx, String outputFilename)
        {
            // Couldn't delete a file
            // because it was set to readonly
            // or couldn't create a file
            // because of permissions issues
            _logEventSource.TraceEvent(TraceEventType.Error,
                (int) TraceEventType.Error,
                ErrorDialogInstructionCouldNotWrite +
                Environment.NewLine +
                "Exception message: " + unauthorizedEx.Message);
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogInstructionCouldNotWrite + Environment.NewLine +
                $"Insufficient privileges to either create or delete {outputFilename}");
        }


        public void HandleFileNotDeleted(string standardInputFilename)
        {
            _logEventSource.TraceEvent(TraceEventType.Warning,
                (int) TraceEventType.Warning,
                String.Format(WarnFileNotDeleted, standardInputFilename));
        }

        public void HandleUnhandledException(UnhandledExceptionEventArgs e)
        {
            _logEventSource.TraceEvent(TraceEventType.Critical,
                (int) TraceEventType.Critical,
                ((Exception) e.ExceptionObject).Message + Environment.NewLine +
                ((Exception) e.ExceptionObject).StackTrace);
            DisplayErrorMessage(ErrorDialogCaption,
                ErrorDialogInstructionUnexpectedError);
        }
        /// <summary>
        /// Displays up a topmost, OK-only message box for the error message
        /// </summary>
        /// <param name="boxCaption">The box's caption</param>
        /// <param name="boxMessage">The box's message</param>
        private void DisplayErrorMessage(String boxCaption,
            String boxMessage)
        {
            MessageBox.Show(boxMessage,
                boxCaption,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1,
                MessageBoxOptions.DefaultDesktopOnly);
        }
    }
}