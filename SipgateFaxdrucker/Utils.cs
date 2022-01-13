using PhoneNumbers;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SipgateFaxdrucker.Properties;

namespace SipgateFaxdrucker
{
    public enum ValidationMessageCode
    {
        Ok = 0,
        TooShort = 1,
        IllegalPrefix = 2,
        NumberInvalid = 3
    }

    public class NumberValidationResult
    {
        public bool wasSuccessful;
        public ValidationMessageCode validationMessageCode;
        public Exception exception;
        public PhoneNumber phonenumber;
        public NumberValidationResult(bool success, ValidationMessageCode message, PhoneNumber number = null, Exception error = null)
        {
            wasSuccessful = success;
            validationMessageCode = message;
            phonenumber = number;
            exception = error;
        }
    }

    public static class Utils
    {

        public static string[] numberValidationMessages = { "Nummer OK", "Nummer zu kurz", "Nicht unterstütze Vorwahl", "Keine gültige Faxnummer" };

        public static string appName = "sipgateFaxdrucker";

        public static TraceSource faxdruckerTraceSource = new TraceSource(appName);

        public static void InitializeTraceSource()
        {
            string logfileDirectory = $@"{Environment.ExpandEnvironmentVariables("%userprofile%")}\AppData\Local\{appName}\Logs\";

            try
            {
                Directory.CreateDirectory(logfileDirectory);
                RemoveOldLogs(logfileDirectory);

                var filename = $"{appName}_{DateTime.Now:dd-MM-yyyy}.log";
                FileStream logFile = new FileStream($"{logfileDirectory}{filename}", FileMode.Append, FileAccess.Write);

                TextWriterTraceListener faxdruckerListener = new TextWriterTraceListener(logFile)
                {
                    TraceOutputOptions = TraceOptions.DateTime
                };
                faxdruckerTraceSource.Listeners.Clear();
                faxdruckerTraceSource.Listeners.Add(faxdruckerListener);
                Trace.AutoFlush = true;

            }
            catch (Exception)
            {
                //
            }
        }

        private static void RemoveOldLogs(string directory)
        {
            int logfileStorageDurationInDays = int.Parse(Settings.Default.LogFileStorageInDays);

            string[] files = Directory.GetFiles(directory);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTime < DateTime.Now.AddDays(logfileStorageDurationInDays * -1))
                {
                    fileInfo.Delete();
                }

            }
        }

        public static string E164TargetNumber(PhoneNumber phoneNumber)
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            return phoneNumberUtil.Format(phoneNumber, PhoneNumberFormat.E164);
        }

        public static string ConvertPdfToBase64(string fileName)
        {
#if DEBUG
            if (fileName == null)
            {
                fileName = @"C:\Users\admin\Desktop\dokument-samples\sample.pdf";
            }
#endif
            FileStream outputFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            byte[] bytes;

            using (var memoryStream = new MemoryStream())
            {
                outputFile.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            outputFile.Close();

            return Convert.ToBase64String(bytes);
        }

        public static NumberValidationResult GetNumberValidationResult(string number)
        {
            if (number.Length < 7)
            {
                if (!Int32.TryParse(number, out int _))
                {
                    return new NumberValidationResult(false, ValidationMessageCode.NumberInvalid);
                }

                return new NumberValidationResult(false, ValidationMessageCode.TooShort);
            }

            var phoneNumberUtil = PhoneNumberUtil.GetInstance();

            try
            {
                PhoneNumber phoneNumber = phoneNumberUtil.Parse(number, "DE");
                var nationalNumber = phoneNumber.NationalNumber.ToString();

                Match matchesIllegalPrefix = Regex.Match(nationalNumber, "^(137|138|1212|185|1807|181|188|900)");
                if (matchesIllegalPrefix.Success)
                {
                    return new NumberValidationResult(false, ValidationMessageCode.IllegalPrefix);
                }

                bool isValid = phoneNumberUtil.IsValidNumber(phoneNumber);
                if (!isValid)
                {
                    return new NumberValidationResult(false, ValidationMessageCode.NumberInvalid);
                }
                return new NumberValidationResult(true, ValidationMessageCode.Ok, phoneNumber);
            }
            catch (NumberParseException err)
            {
                return new NumberValidationResult(false, ValidationMessageCode.NumberInvalid, null, err);
            }
        }

        public static string CreateSha256Hash(string clearString)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(clearString));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }

        public static double GetFileSize(string fileName)
        {
            return new FileInfo(fileName).Length;
        }

        public static string GetFormattedFileSize(double len)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"File Size: {len:0.##} {sizes[order]}";
        }
       
        #region Logs
        public static void LogCritical(string logLine, int eventId = 0)
        {
            faxdruckerTraceSource.TraceEvent(TraceEventType.Critical, eventId, logLine);
        }

        public static void LogError(string logLine, int eventId = 0)
        {
            faxdruckerTraceSource.TraceEvent(TraceEventType.Error, eventId, logLine);
        }

        public static void LogInformation(string logLine, int eventId = 0)
        {
            faxdruckerTraceSource.TraceEvent(TraceEventType.Information, eventId, logLine);
        }

        public static void LogWarning(string logLine, int eventId = 0)
        {
            faxdruckerTraceSource.TraceEvent(TraceEventType.Warning, eventId, logLine);
        }

        #endregion
    }
}
