using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;
using System.ComponentModel;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponent : INotifyPropertyChanged, IComparable
    {
        public WizardBaseController Controller { get; set; }

        private bool _isSelected;
        public bool IsSelected 
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        private bool __isRequred;
        public bool IsRequired 
        {
            get
            {
                return __isRequred;
            }
            set
            {
                __isRequred = value;
                NotifyPropertyChanged("IsRequired");
            }
        }
        public string Name
        {
            get
            {
                return Controller.ControllerName;
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public int CompareTo(object obj)
        {
            if (obj is WizardComponent)
            {
                WizardComponent other = obj as WizardComponent;
                return Controller.ControllerName.CompareTo(other.Controller.ControllerName);
            }
            else
            {
                throw new Exception("Cannot compare WizardComponent to other non-WizardComponent types.");
            }
        }
    }
}