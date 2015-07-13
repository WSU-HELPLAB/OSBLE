using System;
using System.Collections.Generic;
using System.ComponentModel;

using OSBLEPlus.Logic.DomainObjects.Interface;

namespace OSBIDE.Library.ServiceClient
{
    public class TransmissionStatus : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private List<IActivityEvent> _currentTransmission;
        private List<IActivityEvent> _lastTransmission;
        private DateTime _lastTransmissionTime = DateTime.MinValue;

        private int _numberOfTransmissions;
        private int _completedTransmissions;

        private bool _isActive;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }
        public List<IActivityEvent> CurrentTransmission
        {
            get
            {
                return _currentTransmission;
            }
            set
            {
                _currentTransmission = value;
                OnPropertyChanged("CurrentTransmission");
            }
        }
        public List<IActivityEvent> LastTransmission
        {
            get
            {
                return _lastTransmission;
            }
            set
            {
                _lastTransmission = value;
                OnPropertyChanged("LastTransmission");
            }
        }
        public int NumberOfTransmissions
        {
            get
            {
                return _numberOfTransmissions;
            }
            set
            {
                _numberOfTransmissions = value;
                OnPropertyChanged("NumberOfTransmissions");
            }
        }
        public int CompletedTransmissions
        {
            get
            {
                return _completedTransmissions;
            }
            set
            {
                _completedTransmissions = value;
                OnPropertyChanged("CompletedTransmissions");
            }
        }
        public DateTime LastTransmissionTime
        {
            get
            {
                return _lastTransmissionTime;
            }
            set
            {
                _lastTransmissionTime = value;
                OnPropertyChanged("LastTransmissionTime");
            }
        }
    }
}
