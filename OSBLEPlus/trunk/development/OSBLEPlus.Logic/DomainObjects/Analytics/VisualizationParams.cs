using System;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Analytics
{
    public class VisualizationParams
    {
        public const int DEFAULT_TIMEOUT = 3;

        public TimeScale TimeScale { get; set; }
        public DateTime? TimeFrom { get; set; }

        private DateTime? _timeTo;
        public DateTime? TimeTo
        {
            get
            {
                return _timeTo;
            }
            set
            {
                if (value.HasValue && TimeFrom.HasValue && value.Value == TimeFrom.Value)
                {
                    _timeTo = value.Value.AddHours(23).AddMinutes(59).AddSeconds(59);
                }
                else
                {
                    _timeTo = value;
                }
            }
        }
        public int? Timeout { get; set; }
        public bool GrayScale { get; set; }
    }
}
