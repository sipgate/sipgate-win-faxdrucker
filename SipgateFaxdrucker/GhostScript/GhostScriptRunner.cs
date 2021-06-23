using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SipgateFaxdrucker.GhostScript
{
    public class GhostScriptRunner
    {
        
        private String _outputFilename = String.Empty;
        
        const string TraceSourceName = "SipgateFaxdrucker";
        private static readonly TraceSource LogEventSource = new TraceSource(TraceSourceName);
        

        public string Print(String standardInputFilename)
        {
            ErrorDialog errorDialog = new ErrorDialog(LogEventSource);

            var sizeInfo = Utils.GetFileSize(standardInputFilename);
            Utils.LogCritical("standardinputfilename before : "+ sizeInfo);
            try
            {
                using (BinaryReader standardInputReader = new BinaryReader(Console.OpenStandardInput()))
                {
                    using (FileStream standardInputFile =
                        new FileStream(standardInputFilename, FileMode.Create, FileAccess.ReadWrite))
                    {
                        standardInputReader.BaseStream.CopyTo(standardInputFile);                             

                    }                    
                }

                if (GetPdfOutputFilename(ref _outputFilename))
                {
                    File.Delete(_outputFilename);
                    String[] ghostScriptArguments =
                    {
                        "-dBATCH", "-dNOPAUSE", "-dSAFER", "-sDEVICE=pdfwrite", "-dCompatibilityLevel=1.4", "-dPDFSETTINGS=/ebook",
                        $"-sOutputFile={_outputFilename}", standardInputFilename,
                        "-c", @"[/Creator(SipgateFaxdrucker 4.0.0 (PSCRIPT5)) /DOCINFO pdfmark", "-f"
                    };

                    // the current process will usually run in 32 bits, but that also depends on the OS, spooler, printer drivers, executable location, etc.
                    // call the ghostscript dll accordingly
                    if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
                    {
                        GhostScript64.CallApi(ghostScriptArguments);
                    }
                    else
                    {
                        GhostScript32.CallApi(ghostScriptArguments);
                    }
                }
            }
            catch (IOException ioEx)
            {
                errorDialog.Show(ioEx,_outputFilename);
            }
            catch (UnauthorizedAccessException unauthorizedEx)
            {
                errorDialog.Show(unauthorizedEx, _outputFilename);
            }
            catch (ExternalException ghostscriptEx)
            {
                errorDialog.Show(ghostscriptEx);
            }
            catch (Exception ex)
            {
                errorDialog.Show(ex);
            }
            finally
            {
                try
                {
                    File.Delete(standardInputFilename);
                }
                catch
                {
                    errorDialog.HandleFileNotDeleted(standardInputFilename);
                }
                LogEventSource.Flush();
            }

            return _outputFilename;
        }

        private bool GetPdfOutputFilename(ref String outputFile)
        {
            bool filenameRetrieved = false;
            ErrorDialog errorDialog = new ErrorDialog(LogEventSource);
            
            try
            {
                outputFile = GetOutputFilename();
                using (FileStream _ = File.Create(outputFile))
                {
                    // Tests if we can write to the destination
                }

                File.Delete(outputFile);
                filenameRetrieved = true;
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is NotSupportedException ||
                                       ex is DirectoryNotFoundException)
            {
                errorDialog.Show(ex);
            }
            catch (PathTooLongException ex)
            {
                errorDialog.Show(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                errorDialog.Show(ex);
            }

            return filenameRetrieved;
        }

        private String GetOutputFilename()
        {
            var currentDateTimeString = DateTime.Now.ToString("dd-MM-yyyy HH-mm");
            var settingsPathPart = Path.GetTempPath();
            var completePath = settingsPathPart + currentDateTimeString + ".pdf";
            
            String defaultOutputFilename =
                Path.GetFullPath(completePath);
            
            // Check if there are any % characters -
            // even though it's a legal Windows filename character,
            // it is a special character to Ghostscript
            if (defaultOutputFilename.Contains("%"))
                throw new ArgumentException("OutputFile setting contains % character.");
            return defaultOutputFilename;
        }
    }
}