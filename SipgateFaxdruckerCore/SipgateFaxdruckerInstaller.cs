﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Security;

using Microsoft.Win32;

namespace SipgateFaxdruckerCore
{
 
    public class SipgateFaxdruckerInstaller
    {
        #region Printer Driver Win32 API Constants

        const uint DRIVER_KERNELMODE = 0x00000001;
        const uint DRIVER_USERMODE =  0x00000002;
        
        const uint APD_STRICT_UPGRADE =  0x00000001;
        const uint APD_STRICT_DOWNGRADE = 0x00000002;
        const uint APD_COPY_ALL_FILES = 0x00000004;
        const uint APD_COPY_NEW_FILES = 0x00000008;
        const uint APD_COPY_FROM_DIRECTORY = 0x00000010;
        
        const uint DPD_DELETE_UNUSED_FILES = 0x00000001;
        const uint DPD_DELETE_SPECIFIC_VERSION = 0x00000002;
        const uint DPD_DELETE_ALL_FILES = 0x00000004;

        const int WIN32_FILE_ALREADY_EXISTS = 183; // Returned by XcvData "AddPort" if the port already exists
        #endregion

        private readonly TraceSource logEventSource;
        private readonly String logEventSourceNameDefault = "SipgateFaxdruckerCore";

        const string ENVIRONMENT_64 = null; // auto-detect
        const string PRINTERNAME = "sipgate-Faxdrucker";
        const string DRIVERNAME = "sipgate faxdrucker Virtual Printer";
        const string HARDWAREID = "SGFAX_Driver0101";
        const string PORTMONITOR = "SGFAX";
        private readonly string MONITORDLL = Environment.Is64BitOperatingSystem ? "redmon64pdfscribe.dll" : "redmon32pdfscribe.dll";
        const string PORTNAME = "SGFAX:";
        const string PRINTPROCESOR = "winprint";

        const string DRIVERMANUFACTURER = "sipgate GmbH";
        
        const string DRIVERFILE = "PSCRIPT5.DLL";
        const string DRIVERUIFILE = "PS5UI.DLL";
        const string DRIVERHELPFILE = "PSCRIPT.HLP";
        const string DRIVERDATAFILE = "SCPDFPRN.PPD";
        
        enum DriverFileIndex
        {
            Min = 0,
            DriverFile = Min,
            UIFile,
            HelpFile,
            DataFile,
            Max = DataFile
        };

        readonly String[] printerDriverFiles = new String[] { DRIVERFILE, DRIVERUIFILE, DRIVERHELPFILE, DRIVERDATAFILE };
        readonly String[] printerDriverDependentFiles = new String[] { "PSCRIPT.NTF" };

        #region Error messages for Trace/Debug

        const string FILENOTDELETED_INUSE = "{0} is being used by another process. File was not deleted.";
        const string FILENOTDELETED_UNAUTHORIZED = "{0} is read-only, or its file permissions do not allow for deletion.";

        const string FILENOTCOPIED_PRINTERDRIVER = "Printer driver file was not copied. Exception message: {0}";
        const string FILENOTCOPIED_ALREADYEXISTS = "Destination file {0} was not copied/created - it already exists.";

        const string WIN32ERROR = "Win32 error code {0}.";

        const string NATIVE_COULDNOTENABLE64REDIRECTION = "Could not enable 64-bit file system redirection.";
        const string NATIVE_COULDNOTREVERT64REDIRECTION = "Could not revert 64-bit file system redirection.";

        const string INSTALL_ROLLBACK_FAILURE_AT_FUNCTION = "Partial uninstallation failure. Function {0} returned false.";

        const string REGISTRYCONFIG_NOT_ADDED = "Could not add port configuration to registry. Exception message: {0}";
        const string REGISTRYCONFIG_NOT_DELETED = "Could not delete port configuration from registry. Exception message: {0}";

        const String INFO_INSTALLPORTMONITOR_FAILED = "Port monitor installation failed.";
        const String INFO_INSTALLCOPYDRIVER_FAILED = "Could not copy printer driver files.";
        const String INFO_INSTALLPORT_FAILED = "Could not add redirected port.";
        const String INFO_INSTALLPRINTERDRIVER_FAILED = "Printer driver installation failed.";
        const String INFO_INSTALLPRINTER_FAILED = "Could not add printer.";
        const String INFO_INSTALLCONFIGPORT_FAILED = "Port configuration failed.";

        #endregion

        public void AddTraceListener(TraceListener additionalListener)
        {
            this.logEventSource.Listeners.Add(additionalListener);
        }

        
        #region Constructors

        public SipgateFaxdruckerInstaller()
        {
            this.logEventSource = new TraceSource(logEventSourceNameDefault)
            {
                Switch = new SourceSwitch("SipgateFaxdruckerCoreAll")
                {
                    Level = SourceLevels.All
                }
            };
        }
        /*
        /// <summary>
        /// This override sets the
        /// trace source to a specific name
        /// </summary>
        /// <param name="eventSourceName">Trace source name</param>
        public SipgateFaxdruckerInstaller(String eventSourceName)
        {
            if (!String.IsNullOrEmpty(eventSourceName))
            {
                this.logEventSource = new TraceSource(eventSourceName);
            }
            else
            {
                throw new ArgumentNullException("eventSourceName");
            }
            this.logEventSource.Switch = new SourceSwitch("SipgateFaxdruckerCoreSwitch");
            this.logEventSource.Switch.Level = SourceLevels.All;
        }
        */
        #endregion

        #region Port operations

#if DEBUG
        public bool AddSipgateFaxdruckerPort_Test()
        {
            return AddSipgateFaxdruckerPort();
        }
#endif

        private bool AddSipgateFaxdruckerPort()
        {
            bool portAdded = false;

            int portAddResult = DoXcvDataPortOperation(PORTNAME, PORTMONITOR, "AddPort");
            switch (portAddResult)
            {
                case 0:
                case WIN32_FILE_ALREADY_EXISTS: // Port already exists - this is OK, we'll just keep using it
                    portAdded = true;
                    break;
            }
            return portAdded;
        }

        public bool DeleteSipgateFaxdruckerPort()
        {
            bool portDeleted = false;

            int portDeleteResult = DoXcvDataPortOperation(PORTNAME, PORTMONITOR, "DeletePort");
            switch (portDeleteResult)
            {
                case 0:
                    portDeleted = true;
                    break;
            }
            return portDeleted;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="portMonitor"></param>
        /// <param name="xcvDataOperation"></param>
        /// <returns></returns>
        /// <remarks>I can't remember the name/link of the developer who wrote this code originally,
        /// so I can't provide a link or credit.</remarks>
        private int DoXcvDataPortOperation(string portName, string portMonitor, string xcvDataOperation)
        {

            int win32ErrorCode;

            PRINTER_DEFAULTS def = new PRINTER_DEFAULTS
            {
                pDatatype = null, 
                pDevMode = IntPtr.Zero,
                DesiredAccess = 1
            };

            //Server Access Administer

            IntPtr hPrinter = IntPtr.Zero;

            if (NativeMethods.OpenPrinter(",XcvMonitor " + portMonitor, ref hPrinter, def) != 0)
            {
                if (!portName.EndsWith("\0"))
                    portName += "\0"; // Must be a null terminated string

                // Must get the size in bytes. Rememeber .NET strings are formed by 2-byte characters
                uint size = (uint)(portName.Length * 2);

                // Alloc memory in HGlobal to set the portName
                IntPtr portPtr = Marshal.AllocHGlobal((int)size);
                Marshal.Copy(portName.ToCharArray(), 0, portPtr, portName.Length);

                NativeMethods.XcvData(hPrinter, xcvDataOperation, portPtr, size, IntPtr.Zero, 0, out var needed, out var xcvResult);

                NativeMethods.ClosePrinter(hPrinter);
                Marshal.FreeHGlobal(portPtr);
                win32ErrorCode = (int)xcvResult;
            }
            else
            {
                win32ErrorCode = Marshal.GetLastWin32Error();
            }
            return win32ErrorCode;

        }

        #endregion

        #region Port Monitor

        /// <summary>
        /// Adds the sipgate faxdrucker port monitor
        /// </summary>
        /// <param name="monitorFilePath">Directory where the uninstalled monitor dll is located</param>
        /// <returns>true if the monitor is installed, false if install failed</returns>
        public bool AddSipgateFaxdruckerPortMonitor(String monitorFilePath)
        {
            bool monitorAdded = false;

            IntPtr oldRedirectValue = IntPtr.Zero;

            try
            {
                oldRedirectValue = DisableWow64Redirection();
                if (!DoesMonitorExist(PORTMONITOR))
                {
                    // Copy the monitor DLL to
                    // the system directory
                    String fileSourcePath = Path.Combine(monitorFilePath, MONITORDLL);
                    String fileDestinationPath = Path.Combine(Environment.SystemDirectory, MONITORDLL);
                    try
                    {
                        File.Copy(fileSourcePath, fileDestinationPath, true);
                    }
                    catch (IOException)
                    {
                        // File in use, log -
                        // this is OK because it means the file is already there
                    }

                    MONITOR_INFO_2 newMonitor = new MONITOR_INFO_2
                    {
                        pName = PORTMONITOR, pEnvironment = ENVIRONMENT_64, pDLLName = MONITORDLL
                    };
                    if (!AddPortMonitor(newMonitor))
                        logEventSource.TraceEvent(TraceEventType.Error,
                                                  (int)TraceEventType.Error,
                                                  $"Could not add port monitor {PORTMONITOR}" + Environment.NewLine +
                                                  String.Format(WIN32ERROR, Marshal.GetLastWin32Error().ToString()));
                    else
                        monitorAdded = true;
                }
                else
                {
                    // Monitor already installed -
                    // log it, and keep going
                    logEventSource.TraceEvent(TraceEventType.Warning,
                                              (int)TraceEventType.Warning,
                                              $"Port monitor {PORTMONITOR} already installed.");
                    monitorAdded = true;
                }

            }
            finally
            {
                if (oldRedirectValue != IntPtr.Zero) RevertWow64Redirection(oldRedirectValue);
            }


            return monitorAdded;
        }


        /// <summary>
        /// Disables WOW64 system directory file redirection
        /// if the current process is both
        /// 32-bit, and running on a 64-bit OS -
        /// Compiling for 64-bit OS, and setting the install dir to "ProgramFiles64"
        /// should ensure this code never runs in production
        /// </summary>
        /// <returns>A Handle, which should be retained to reenable redirection</returns>
        private IntPtr DisableWow64Redirection()
        {
            IntPtr oldValue = IntPtr.Zero;
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                if (!NativeMethods.Wow64DisableWow64FsRedirection(ref oldValue))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not disable Wow64 file system redirection.");
            return oldValue;
        }

        /// <summary>
        /// Reenables WOW64 system directory file redirection
        /// if the current process is both
        /// 32-bit, and running on a 64-bit OS -
        /// Compiling for 64-bit OS, and setting the install dir to "ProgramFiles64"
        /// should ensure this code never runs in production
        /// </summary>
        /// <param name="oldValue">A Handle value - should be retained from call to <see cref="DisableWow64Redirection"/></param>
        private void RevertWow64Redirection(IntPtr oldValue)
        {
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                if (!NativeMethods.Wow64RevertWow64FsRedirection(oldValue))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not reenable Wow64 file system redirection.");
                }
            }
        }

        /// <summary>
        /// Removes the sipgate faxdrucker port monitor
        /// </summary>
        /// <returns>true if monitor successfully removed, false if removal failed</returns>
        public bool RemoveSipgateFaxdruckerPortMonitor()
        {
            bool monitorRemoved = false;
            if ((NativeMethods.DeleteMonitor(null, ENVIRONMENT_64, PORTMONITOR)) != 0)
            {
                monitorRemoved = true;
                // Try to remove the monitor DLL now
                if (!DeleteSipgateFaxdruckerPortMonitorDll())
                {
                    logEventSource.TraceEvent(TraceEventType.Warning,
                                              (int)TraceEventType.Warning,
                                              "Could not remove port monitor dll.");
                }
            }
            return monitorRemoved;
        }

        private bool DeleteSipgateFaxdruckerPortMonitorDll()
        {
            return DeletePortMonitorDll(MONITORDLL);
        }

        private bool DeletePortMonitorDll(String monitorDll)
        {
            bool monitorDllRemoved = false;

            String monitorDllFullPathname = String.Empty;
            IntPtr oldRedirectValue = IntPtr.Zero;
            try
            {
                oldRedirectValue = DisableWow64Redirection();

                monitorDllFullPathname = Path.Combine(Environment.SystemDirectory, monitorDll);
                
                File.Delete(monitorDllFullPathname);
                monitorDllRemoved = true;
            }
            catch (Win32Exception windows32Ex)
            {
                // This one is likely very bad -
                // log and rethrow so we don't continue
                // to try to uninstall
                logEventSource.TraceEvent(TraceEventType.Critical, 
                                          (int)TraceEventType.Critical, 
                                          NATIVE_COULDNOTENABLE64REDIRECTION + String.Format(WIN32ERROR, windows32Ex.NativeErrorCode.ToString()));
                throw;
            }
            catch (IOException)
            {
                // File still in use
                logEventSource.TraceEvent(TraceEventType.Error, (int)TraceEventType.Error, String.Format(FILENOTDELETED_INUSE, monitorDllFullPathname));  
            }
            catch (UnauthorizedAccessException)
            {
                // File is readonly, or file permissions do not allow delete
                logEventSource.TraceEvent(TraceEventType.Error, (int)TraceEventType.Error, String.Format(FILENOTDELETED_INUSE, monitorDllFullPathname));
            }
            finally
            {
                try
                {
                    if (oldRedirectValue != IntPtr.Zero) RevertWow64Redirection(oldRedirectValue);
                }
                catch (Win32Exception windows32Ex)
                {
                    // Couldn't turn file redirection back on -
                    // this is not good
                    logEventSource.TraceEvent(TraceEventType.Critical, 
                                              (int)TraceEventType.Critical, 
                                              NATIVE_COULDNOTREVERT64REDIRECTION + String.Format(WIN32ERROR, windows32Ex.NativeErrorCode.ToString()));
                    throw;
                }
            }

            return monitorDllRemoved;

        }

        private bool AddPortMonitor(MONITOR_INFO_2 newMonitor)
        {
            bool monitorAdded = false;
            if ((NativeMethods.AddMonitor(null, 2, ref newMonitor) != 0))
            {
                monitorAdded = true;
            }
            return monitorAdded;
        }

        private bool DeletePortMonitor(String monitorName)
        {
            bool monitorDeleted = false;
            if ((NativeMethods.DeleteMonitor(null, ENVIRONMENT_64, monitorName)) != 0)
            {
                monitorDeleted = true;
            }
            return monitorDeleted;
        }

        private bool DoesMonitorExist(String monitorName)
        {
            bool monitorExists = false;
            List<MONITOR_INFO_2> portMonitors = EnumerateMonitors();
            foreach (MONITOR_INFO_2 portMonitor in portMonitors)
            {
                if (portMonitor.pName == monitorName)
                {
                    monitorExists = true;
                    break;
                }
            }
            return monitorExists;
        }


        public List<MONITOR_INFO_2> EnumerateMonitors()
        {
            List<MONITOR_INFO_2> portMonitors = new List<MONITOR_INFO_2>();

            uint pcbNeeded = 0;
            uint pcReturned = 0;

            if (!NativeMethods.EnumMonitors(null, 2, IntPtr.Zero, 0, ref pcbNeeded, ref pcReturned))
            {
                IntPtr pMonitors = Marshal.AllocHGlobal((int)pcbNeeded);
                if (NativeMethods.EnumMonitors(null, 2, pMonitors, pcbNeeded, ref pcbNeeded, ref pcReturned))
                {
                    IntPtr currentMonitor = pMonitors;

                    for (int i = 0; i < pcReturned; i++)
                    {
                        portMonitors.Add((MONITOR_INFO_2)Marshal.PtrToStructure(currentMonitor, typeof(MONITOR_INFO_2)));
                        currentMonitor = IntPtr.Add(currentMonitor, Marshal.SizeOf(typeof(MONITOR_INFO_2)));
                    }
                    Marshal.FreeHGlobal(pMonitors);

                }
                else
                {
                    // Failed to retrieve enumerated monitors
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate port monitors.");
                }

            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Call to EnumMonitors in winspool.drv succeeded with a zero size buffer - unexpected error.");
            }

            return portMonitors;
        }

        #endregion

        #region Printer Install

        public String RetrievePrinterDriverDirectory()
        {
            StringBuilder driverDirectory = new StringBuilder(1024);
            int dirSizeInBytes = 0;
            if (!NativeMethods.GetPrinterDriverDirectory(null,
                                                         null,
                                                         1,
                                                         driverDirectory,
                                                         1024,
                                                         ref dirSizeInBytes))
                throw new DirectoryNotFoundException("Could not retrieve printer driver directory.");
            return driverDirectory.ToString();
        }


        delegate bool undoInstall();

        /// <summary>
        /// Installs the port monitor, port,
        /// printer drivers, and sipgate faxdrucker virtual printer
        /// </summary>
        /// <param name="driverSourceDirectory">Directory where the uninstalled printer driver files are located</param>
        /// <param name="outputHandlerCommand"></param>
        /// <param name="outputHandlerArguments"></param>
        /// <returns>true if installation suceeds, false if failed</returns>
        public bool InstallSipgateFaxdruckerPrinter(String driverSourceDirectory,
                                            String outputHandlerCommand,
                                            String outputHandlerArguments)
        {
            bool printerInstalled = false;

            Stack<undoInstall> undoInstallActions = new Stack<undoInstall>();

            String driverDirectory = RetrievePrinterDriverDirectory();
            undoInstallActions.Push(this.DeleteSipgateFaxdruckerPortMonitorDll);
            if (AddSipgateFaxdruckerPortMonitor(driverSourceDirectory))
            {
                this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                               (int)TraceEventType.Verbose,
                                               "Port monitor successfully installed.");
                undoInstallActions.Push(this.RemoveSipgateFaxdruckerPortMonitor);
                if (CopyPrinterDriverFiles(driverSourceDirectory, printerDriverFiles.Concat(printerDriverDependentFiles).ToArray()))
                {
                    this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                                   (int)TraceEventType.Verbose,
                                                   "Printer drivers copied or already exist.");
                    undoInstallActions.Push(this.RemoveSipgateFaxdruckerPortMonitor);
                    if (AddSipgateFaxdruckerPort())
                    {
                        this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                                       (int)TraceEventType.Verbose,
                                                       "Redirection port added.");
                        undoInstallActions.Push(this.RemovePDFScribePrinterDriver);
                        if (InstallSipgateFaxdruckerPrinterDriver())
                        {
                            this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                                           (int)TraceEventType.Verbose,
                                                           "Printer driver installed.");
                            undoInstallActions.Push(this.DeleteSipgateFaxdruckerPrinter);
                            if (AddSipgateFaxdruckerPrinter())
                            {
                                this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                                               (int)TraceEventType.Verbose,
                                                               "Virtual printer installed.");
                                undoInstallActions.Push(this.RemoveSipgateFaxdruckerPortConfig);
                                if (ConfigureSipgateFaxdruckerPort(outputHandlerCommand, outputHandlerArguments))
                                {
                                    this.logEventSource.TraceEvent(TraceEventType.Verbose,
                                                                   (int)TraceEventType.Verbose,
                                                                   "Printer configured.");
                                    printerInstalled = true;
                                }
                                else
                                    // Failed to configure port
                                    this.logEventSource.TraceEvent(TraceEventType.Error,
                                                                    (int)TraceEventType.Error,
                                                                    INFO_INSTALLCONFIGPORT_FAILED);
                            }
                            else
                                // Failed to install printer
                                this.logEventSource.TraceEvent(TraceEventType.Error,
                                                                (int)TraceEventType.Error,
                                                                INFO_INSTALLPRINTER_FAILED);
                        }
                        else
                            // Failed to install printer driver
                            this.logEventSource.TraceEvent(TraceEventType.Error,
                                                            (int)TraceEventType.Error,
                                                            INFO_INSTALLPRINTERDRIVER_FAILED);
                    }
                    else
                        // Failed to add printer port
                        this.logEventSource.TraceEvent(TraceEventType.Error,
                                                        (int)TraceEventType.Error,
                                                        INFO_INSTALLPORT_FAILED);
                }
                else
                    //Failed to copy printer driver files
                    this.logEventSource.TraceEvent(TraceEventType.Error,
                                                    (int)TraceEventType.Error,
                                                    INFO_INSTALLCOPYDRIVER_FAILED);
            }
            else
                //Failed to add port monitor
                this.logEventSource.TraceEvent(TraceEventType.Error,
                                                (int)TraceEventType.Error,
                                                INFO_INSTALLPORTMONITOR_FAILED);
            if (printerInstalled == false)
            {
                // Printer installation failed -
                // undo all the install steps
                while (undoInstallActions.Count > 0)
                {
                    undoInstall undoAction = undoInstallActions.Pop();
                    try
                    {
                        if (!undoAction())
                        {
                            this.logEventSource.TraceEvent(TraceEventType.Error,
                                                            (int)TraceEventType.Error,
                                                            String.Format(INSTALL_ROLLBACK_FAILURE_AT_FUNCTION, undoAction.Method.Name));
                        }
                    }
                    catch (Win32Exception win32Ex)
                    {
                        this.logEventSource.TraceEvent(TraceEventType.Error,
                                                        (int)TraceEventType.Error,
                                                        String.Format(INSTALL_ROLLBACK_FAILURE_AT_FUNCTION, undoAction.Method.Name) +
                                                        String.Format(WIN32ERROR, win32Ex.ErrorCode.ToString()));
                    }
                }
            }
            this.logEventSource.Flush();
            return printerInstalled;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool UninstallSipgateFaxdruckerPrinter()
        {
            bool printerUninstalledCleanly = true;

            if (!DeleteSipgateFaxdruckerPrinter())
                printerUninstalledCleanly = false;
            if (!RemovePDFScribePrinterDriver())
                printerUninstalledCleanly = false;
            if (!DeleteSipgateFaxdruckerPort())
                printerUninstalledCleanly = false;
            if (!RemoveSipgateFaxdruckerPortMonitor())
                printerUninstalledCleanly = false;
            if (!RemoveSipgateFaxdruckerPortConfig())
                printerUninstalledCleanly = false;
            DeleteSipgateFaxdruckerPortMonitorDll();
            return printerUninstalledCleanly;
        }

        private bool CopyPrinterDriverFiles(String driverSourceDirectory,
                                            String[] filesToCopy)
        {
            bool filesCopied = false;
            String driverDestinationDirectory = RetrievePrinterDriverDirectory();
            try
            {
                foreach (var file in filesToCopy)
                {
                    String fileSourcePath = Path.Combine(driverSourceDirectory, file);
                    String fileDestinationPath = Path.Combine(driverDestinationDirectory, file);
                    try
                    {
                        File.Copy(fileSourcePath, fileDestinationPath);
                    }
                    catch (PathTooLongException)
                    {
                        // Will be caught by outer
                        // IOException catch block
                        throw;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Will be caught by outer
                        // IOException catch block
                        throw;
                    }
                    catch (FileNotFoundException)
                    {
                        // Will be caught by outer
                        // IOException catch block
                        throw;
                    }
                    catch (IOException)
                    {
                        // Just keep going - file was already there
                        // Not really a problem
                        logEventSource.TraceEvent(TraceEventType.Verbose,
                            (int)TraceEventType.Verbose,
                            String.Format(FILENOTCOPIED_ALREADYEXISTS, fileDestinationPath));
                        continue;
                    }
                }

                filesCopied = true;
            }
            catch (IOException ioEx)
            { 
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          String.Format(FILENOTCOPIED_PRINTERDRIVER, ioEx.Message));
            }
            catch (UnauthorizedAccessException unauthorizedEx)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                            (int)TraceEventType.Error,
                            String.Format(FILENOTCOPIED_PRINTERDRIVER, unauthorizedEx.Message));
            }
            catch (NotSupportedException notSupportedEx)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                    (int)TraceEventType.Error,
                    String.Format(FILENOTCOPIED_PRINTERDRIVER, notSupportedEx.Message));
            }


            return filesCopied;
        }

        private bool DeletePrinterDriverFiles(String driverSourceDirectory,
                                              String[] filesToDelete)
        {
            bool allFilesDeleted = true;
            foreach (var file in filesToDelete)
            {
                try
                {
                    File.Delete(Path.Combine(driverSourceDirectory, file));
                }
                catch
                {
                    allFilesDeleted = false;
                }
            }
            return allFilesDeleted;
        }


#if DEBUG
        public bool IsPrinterDriverInstalled_Test(String driverName)
        {
            return IsPrinterDriverInstalled(driverName);
        }
#endif
        private bool IsPrinterDriverInstalled(String driverName)
        {
            bool driverInstalled = false;
            List<DRIVER_INFO_6> installedDrivers = EnumeratePrinterDrivers();
            foreach (DRIVER_INFO_6 printerDriver in installedDrivers)
            {
                if (printerDriver.pName == driverName)
                {
                    driverInstalled = true;
                    break;
                }
            }
            return driverInstalled;
        }

        public List<DRIVER_INFO_6> EnumeratePrinterDrivers()
        {
            List<DRIVER_INFO_6> installedPrinterDrivers = new List<DRIVER_INFO_6>();

            uint pcbNeeded = 0;
            uint pcReturned = 0;

            if (!NativeMethods.EnumPrinterDrivers(null, ENVIRONMENT_64, 6, IntPtr.Zero, 0, ref pcbNeeded, ref pcReturned))
            {
                IntPtr pDrivers = Marshal.AllocHGlobal((int)pcbNeeded);
                if (NativeMethods.EnumPrinterDrivers(null, ENVIRONMENT_64, 6, pDrivers, pcbNeeded, ref pcbNeeded, ref pcReturned))
                {
                    IntPtr currentDriver = pDrivers;
                    for (int loop = 0; loop < pcReturned; loop++)
                    {
                        installedPrinterDrivers.Add((DRIVER_INFO_6)Marshal.PtrToStructure(currentDriver, typeof(DRIVER_INFO_6)));
                        //currentDriver = (IntPtr)(currentDriver.ToInt32() + Marshal.SizeOf(typeof(DRIVER_INFO_6)));
                        currentDriver = IntPtr.Add(currentDriver, Marshal.SizeOf(typeof(DRIVER_INFO_6)));
                    }
                    Marshal.FreeHGlobal(pDrivers);
                }
                else
                {
                    // Failed to enumerate printer drivers
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate printer drivers.");
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Call to EnumPrinterDrivers in winspool.drv succeeded with a zero size buffer - unexpected error.");
            }

            return installedPrinterDrivers;
        }

        private bool InstallSipgateFaxdruckerPrinterDriver()
        {
            bool SipgateFaxdruckerPrinterDriverInstalled = false;

            if (!IsPrinterDriverInstalled(DRIVERNAME))
            {
                String driverSourceDirectory = RetrievePrinterDriverDirectory();

                StringBuilder nullTerminatedDependentFiles = new StringBuilder();
                if (printerDriverDependentFiles.Length > 0)
                {
                    for (int loop = 0; loop <= printerDriverDependentFiles.GetUpperBound(0); loop++)
                    {
                        nullTerminatedDependentFiles.Append(printerDriverDependentFiles[loop]);
                        nullTerminatedDependentFiles.Append("\0");
                    }
                    nullTerminatedDependentFiles.Append("\0");
                }
                else
                {
                    nullTerminatedDependentFiles.Append("\0\0");
                }

                DRIVER_INFO_6 printerDriverInfo = new DRIVER_INFO_6
                {
                    cVersion = 3,
                    pName = DRIVERNAME,
                    pEnvironment = ENVIRONMENT_64,
                    pDriverPath = Path.Combine(driverSourceDirectory, DRIVERFILE),
                    pConfigFile = Path.Combine(driverSourceDirectory, DRIVERUIFILE),
                    pHelpFile = Path.Combine(driverSourceDirectory, DRIVERHELPFILE),
                    pDataFile = Path.Combine(driverSourceDirectory, DRIVERDATAFILE),
                    pDependentFiles = nullTerminatedDependentFiles.ToString(),
                    pMonitorName = PORTMONITOR,
                    pDefaultDataType = String.Empty,
                    dwlDriverVersion = 0x0000000200000000U,
                    pszMfgName = DRIVERMANUFACTURER,
                    pszHardwareID = HARDWAREID,
                    pszProvider = DRIVERMANUFACTURER
                };

                SipgateFaxdruckerPrinterDriverInstalled = InstallPrinterDriver(ref printerDriverInfo);
            }
            else
            {
                SipgateFaxdruckerPrinterDriverInstalled = true; // Driver is already installed, we'll just use the installed driver
            }

            return SipgateFaxdruckerPrinterDriverInstalled;
        }

        private bool InstallPrinterDriver(ref DRIVER_INFO_6 printerDriverInfo)
        {
            bool printerDriverInstalled = false;

            printerDriverInstalled = NativeMethods.AddPrinterDriver(null, 6, ref printerDriverInfo);
            if (printerDriverInstalled == false)
            {
                //int lastWinError = Marshal.GetLastWin32Error();
                //throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not add printer sipgate faxdrucker printer driver.");
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          "Could not add sipgate faxdrucker printer driver. " +
                                          String.Format(WIN32ERROR, Marshal.GetLastWin32Error().ToString()));
            }
            return printerDriverInstalled;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool RemovePDFScribePrinterDriver()
        {
            bool driverRemoved = NativeMethods.DeletePrinterDriverEx(null, ENVIRONMENT_64, DRIVERNAME, DPD_DELETE_UNUSED_FILES, 3);
            if (!driverRemoved)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          "Could not remove sipgate faxdrucker printer driver. " +
                                          String.Format(WIN32ERROR, Marshal.GetLastWin32Error().ToString()));
            }
            return driverRemoved;
        }


        private bool AddSipgateFaxdruckerPrinter()
        {
            bool printerAdded = false;
            PRINTER_INFO_2 sipgateFaxdruckerPrinter = new PRINTER_INFO_2
            {
                pServerName = null,
                pPrinterName = PRINTERNAME,
                pPortName = PORTNAME,
                pDriverName = DRIVERNAME,
                pPrintProcessor = PRINTPROCESOR,
                pDatatype = "RAW",
                Attributes = 0x00000041
            };


            int sipgateFaxdruckerPrinterHandle = NativeMethods.AddPrinter(null, 2, ref sipgateFaxdruckerPrinter);
            if (sipgateFaxdruckerPrinterHandle != 0)
            {
                // Added ok
                int closeCode = NativeMethods.ClosePrinter((IntPtr)sipgateFaxdruckerPrinterHandle);
                printerAdded = true;
            }
            else
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          "Could not add sipgate faxdrucker virtual printer. " + 
                                          String.Format(WIN32ERROR, Marshal.GetLastWin32Error().ToString()));
            }
            return printerAdded;
        }

        private bool DeleteSipgateFaxdruckerPrinter()
        {
            bool printerDeleted = false;

            PRINTER_DEFAULTS scribeDefaults = new PRINTER_DEFAULTS
            {
                DesiredAccess = 0x000F000C, // All access
                pDatatype = null,
                pDevMode = IntPtr.Zero
            }; 

            IntPtr scribeHandle = IntPtr.Zero;
            try
            {
                if (NativeMethods.OpenPrinter(PRINTERNAME, ref scribeHandle, scribeDefaults) != 0)
                {
                    if (NativeMethods.DeletePrinter(scribeHandle))
                        printerDeleted = true;
                }
                else
                {
                    logEventSource.TraceEvent(TraceEventType.Error,
                                              (int)TraceEventType.Error,
                                              "Could not delete sipgate faxdrucker virtual printer. "  +
                                              String.Format(WIN32ERROR, Marshal.GetLastWin32Error().ToString()));
                }
            }
            finally
            {
                if (scribeHandle != IntPtr.Zero) NativeMethods.ClosePrinter(scribeHandle);
            }
            return printerDeleted;
        }


        public bool IsSipgateFaxdruckerPrinterInstalled()
        {
            bool sipgateFaxdruckerInstalled = false;

            PRINTER_DEFAULTS scribeDefaults = new PRINTER_DEFAULTS
            {
                DesiredAccess = 0x00008, // Use access
                pDatatype = null,
                pDevMode = IntPtr.Zero
            };

            IntPtr scribeHandle = IntPtr.Zero;
            if (NativeMethods.OpenPrinter(PRINTERNAME, ref scribeHandle, scribeDefaults) != 0)
            {
                sipgateFaxdruckerInstalled = true;
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 0x5) sipgateFaxdruckerInstalled = true; // Printer is installed, but user
                                                                 // has no privileges to use it
            }

            return sipgateFaxdruckerInstalled;
        }

        #endregion





        #region Configuration and Registry changes

#if DEBUG
        public bool ConfigureSipgateFaxdruckerPort_Test()
        {
            return ConfigureSipgateFaxdruckerPort();
        }
#endif

        private bool ConfigureSipgateFaxdruckerPort()
        {
            return ConfigureSipgateFaxdruckerPort(String.Empty, String.Empty);

        }

        
        private bool ConfigureSipgateFaxdruckerPort(String commandValue,
                                            String argumentsValue)
        {
            bool registryChangesMade = false;
            // Add all the registry info
            // for the port and monitor
            RegistryKey portConfiguration;
            try
            {
                portConfiguration = Registry.LocalMachine.CreateSubKey("SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\" + 
                                                                                PORTMONITOR +
                                                                                "\\Ports\\" + PORTNAME);
                portConfiguration.SetValue("Description", "sipgate faxdrucker", RegistryValueKind.String);
                portConfiguration.SetValue("Command", commandValue, RegistryValueKind.String);
                portConfiguration.SetValue("Arguments", argumentsValue, RegistryValueKind.String);
                portConfiguration.SetValue("Printer", PRINTERNAME, RegistryValueKind.String);
                portConfiguration.SetValue("Output", 0, RegistryValueKind.DWord);
                portConfiguration.SetValue("ShowWindow", 2, RegistryValueKind.DWord);
                portConfiguration.SetValue("RunUser", 1, RegistryValueKind.DWord);
                portConfiguration.SetValue("Delay", 300, RegistryValueKind.DWord);
                portConfiguration.SetValue("LogFileUse", 0, RegistryValueKind.DWord);
                portConfiguration.SetValue("LogFileName", "", RegistryValueKind.String);
                portConfiguration.SetValue("LogFileDebug", 0, RegistryValueKind.DWord);
                portConfiguration.SetValue("PrintError", 0, RegistryValueKind.DWord);
                registryChangesMade = true;
            }

            catch (UnauthorizedAccessException unauthorizedEx)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          String.Format(REGISTRYCONFIG_NOT_ADDED, unauthorizedEx.Message));
            }
            catch (SecurityException securityEx)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                            (int)TraceEventType.Error,
                            String.Format(REGISTRYCONFIG_NOT_ADDED, securityEx.Message));
            }

            return registryChangesMade;
        }

        private bool RemoveSipgateFaxdruckerPortConfig()
        {
            bool registryEntriesRemoved = false;

            try
            {
                Registry.LocalMachine.DeleteSubKey("SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors\\" +
                                                    PORTMONITOR + "\\Ports\\" + PORTNAME, false);
                registryEntriesRemoved = true;
            }
            catch (UnauthorizedAccessException unauthorizedEx)
            {
                logEventSource.TraceEvent(TraceEventType.Error,
                                          (int)TraceEventType.Error,
                                          String.Format(REGISTRYCONFIG_NOT_DELETED, unauthorizedEx.Message));
            }

            return registryEntriesRemoved;

        }

        #endregion

    }
}
