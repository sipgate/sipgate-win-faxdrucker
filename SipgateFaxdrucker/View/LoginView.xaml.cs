using SipgateFaxdrucker.Utils.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SipgateFaxdrucker.SipgateForm;
using static SipgateFaxdrucker.SipgateAPI.SipgateApi;
using System.Windows.Threading;
using SipgateFaxdrucker.Properties;

namespace SipgateFaxdrucker.View
{
   /// <summary>
   /// Interaction logic for LoginView.xaml
   /// </summary>
   public partial class LoginView : UserControl
   {
//#if DEBUG
//      private readonly string _apiUrl = @"https://api.dev.sipgate.com/v2/";
//#else
        private readonly string _apiUrl = Settings.Default.ApiBaseUrl + @"/v2/";
//#endif


      public LoginView()
      {
         InitializeComponent();
      }

      private void LoginView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
      {
         ErrorText.Visibility = Visibility.Hidden;
      }

      private async void LoginButton_Click(object sender, RoutedEventArgs e)
      {
         await LoginUser();
      }

      private void ClearErrorMessage()
      {
         if (ErrorText.Text == "")
         {
            return;
         }

         ErrorText.Text = "";
         ErrorText.Visibility = Visibility.Hidden;
      }

      private async Task LoginUser()
      {
         var auth = new Authentication();
         HttpListenerContext context = null;

         ClearErrorMessage();

         try
         {
            SipgateCredentials sipgateCredentials = credentialManager.GetCredentials();
            if (sipgateCredentials == null)
            {

               FaxDruckerUtils.LogInformation($"Redirect uri: {auth.redirectUri}");
               HttpListener httpListener = auth.CreateLoginBrowserWindow();
               FaxDruckerUtils.LogInformation("Initialized http listener");
               context = await auth.AwaitResponse(httpListener);
               FaxDruckerUtils.LogInformation("Created listener");
               FaxDruckerUtils.LogInformation("About to send http response");

               FaxDruckerUtils.LogInformation("Http response sent");

               Instance.Activate();

               string authCode = auth.ProcessResult(context);
               FaxDruckerUtils.LogInformation($"Auth code retrieved:\n{authCode}");

               if (authCode != "")
               {
                  sipgateCredentials = await auth.PerformCodeExchange(authCode);
                  credentialManager.SaveCredentials(sipgateCredentials);
                  SipgateForm.Instance.ShowPage(FormPage.TargetNumber);
                  var apiClient = SipgateForm.Instance.GetApiClient();

                  await SipgateForm.Instance.SetEligibleFaxlines(apiClient);
                  FaxDruckerUtils.LogInformation($"Token:\n{sipgateCredentials.AccessToken}");
                  auth.SendHttpResponse(context, true);
               }
               else
               {
                  FaxDruckerUtils.LogWarning("Login: no valid auth code to retrieve token with");
                  auth.SendHttpResponse(context, false);
               }

               if (httpListener.IsListening)
               {
                  DispatcherTimer timer = new DispatcherTimer
                  {
                     Interval = TimeSpan.FromSeconds(4)
                  };

                  timer.Tick += (sender, e) =>
                  {
                     httpListener.Stop();
                  };
                  timer.Start();
                  timer.Stop();

                  FaxDruckerUtils.LogInformation("http Listener stopped");
               }
            }
         }
         catch (AuthorizationException authex)
         {
            FaxDruckerUtils.LogCritical($"Error Authenticating: {authex.Message}");
            ErrorText.Visibility = Visibility.Visible;
            ErrorText.Text = "Fehler beim Login";
            if (context != null)
            {
               FaxDruckerUtils.LogInformation("Sending Failure Page");
               auth.SendHttpResponse(context, false);
            }

         }
         catch (Exception exc)
         {
            MessageBox.Show(exc.Message);
            FaxDruckerUtils.LogCritical($"Login error: {exc.Message}, {exc.GetType()}");
         }
      }

      public void showErrorOnLoginView(string errorMessage)
      {
         if (errorMessage == "")
         {
            return;
         }

         ErrorText.Text = errorMessage;
         ErrorText.Visibility = Visibility.Visible;
      }
   }
}
