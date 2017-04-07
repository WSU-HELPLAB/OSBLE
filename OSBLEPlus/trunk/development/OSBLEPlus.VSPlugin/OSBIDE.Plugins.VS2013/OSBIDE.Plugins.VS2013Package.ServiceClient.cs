using System;
using System.ComponentModel;

using OSBIDE.Library.ServiceClient;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBIDE.Plugins.Base;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private void SetupServiceClient()
        {            
            _eventHandler = new VsEventHandler(this, EventGenerator.GetInstance()); //need the toolManager to open the intervention window
            _eventHandler.InterventionUpdate += InterventionReceivedUpdate;

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

        void InterventionReceivedUpdate(object sender, EventArgs e)
        {
            OpenInterventionToolWindow();
        }
    }
}
