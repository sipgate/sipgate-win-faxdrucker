using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using SipgateFaxdruckerCore;

namespace SipgateFaxdruckerTests
{
    public class Tests
    {
        #region SipgateFaxdruckerCore Tests
        //[Test]
        public void Test_DeleteSipgateFaxdruckerPort()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.DeleteSipgateFaxdruckerPort();
        }

        //[Test]
        public void Test_RemoveSipgateFaxdruckerDriver()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.RemovePDFScribePrinterDriver();
        }

        //[Test]
        public void Test_AddSipgateFaxdruckerPort()
        {

            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.AddSipgateFaxdruckerPort_Test();
        }

        //[Test]
        public void Test_IsPrinterDriverInstalled()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.IsPrinterDriverInstalled_Test("sipgate faxdrucker");
        }

        //[Test]
        public void Test_InstallSipgateFaxdruckerPrinter()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.InstallSipgateFaxdruckerPrinter(@"C:\Code\SipgateFaxdrucker\Lib\", String.Empty, String.Empty);
        }

        //[Test]
        public void Test_UninstallSipgateFaxdruckerPrinter()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.UninstallSipgateFaxdruckerPrinter();
        }

        //[Test]
        public void Test_RemoveSipgateFaxdruckerPortMonitor()
        {
            var sgFaxInstaller = new SipgateFaxdruckerInstaller();
            sgFaxInstaller.RemoveSipgateFaxdruckerPortMonitor();
        }

        #endregion

    }
}
