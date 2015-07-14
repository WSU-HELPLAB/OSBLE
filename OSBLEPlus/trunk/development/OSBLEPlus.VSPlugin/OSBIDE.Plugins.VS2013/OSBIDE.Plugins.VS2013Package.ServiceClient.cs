using System;
using System.ComponentModel;

using OSBIDE.Library.ServiceClient;
using OSBIDE.Library.ServiceClient.ServiceHelpers;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private void SetupServiceClient()
        {
            _eventHandler = new VsEventHandler(this, EventGenerator.GetInstance());
            _client = ServiceClient.GetInstance(_eventHandler, _errorLogger);
            _client.PropertyChanged += ServiceClientPropertyChanged;
            _client.ReceivedNewSocialActivity += ServiceClientReceivedSocialUpdate;
        }
        void ServiceClientPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateServiceConnectionStatus();
        }

        void ServiceClientReceivedSocialUpdate(object sender, EventArgs e)
        {
            ToggleProfileImage(true);
        }
    }
}
