using System;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public interface IEventGenerator
    {
        event EventHandler<SubmitAssignmentArgs> SolutionSubmitRequest;
        event EventHandler<SolutionDownloadedEventArgs> SolutionDownloaded;
        event EventHandler<SubmitEventArgs> SubmitEventRequested;
    }
}
