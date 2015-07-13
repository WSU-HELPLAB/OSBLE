using System;
using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class EventCreatedArgs : EventArgs
    {
        public IActivityEvent OsbideEvent { get; private set; }

        public EventCreatedArgs(IActivityEvent osbideEvent)
        {
            OsbideEvent = osbideEvent;
        }
    }
}
