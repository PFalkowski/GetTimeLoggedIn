using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetWorkingHours
{
    public class ViewModel : BindableBase
    {
        private DateTime _DateFrom = DateTime.Now;
        public DateTime DateFrom
        {
            get
            {
                return _DateFrom;
            }
            set
            {
                if (value != _DateFrom)
                {
                    _DateFrom = value;
                    TextBoxContent = $"{GetHoursBetweenChoosenDates()} hours";
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TextBoxContent));
                }
            }
        }
        private DateTime _DateTo = DateTime.Now;
        public DateTime DateTo
        {
            get
            {
                return _DateTo;
            }
            set
            {
                if (value != _DateTo)
                {
                    _DateTo = value;
                    TextBoxContent = $"{GetHoursBetweenChoosenDates()} hours";
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TextBoxContent));
                }
            }
        }
        public string TextBoxContent { get; set; }

        private double GetHoursBetweenChoosenDates()
        {
            if (DateFrom > DateTo) throw new ArgumentException(nameof(DateTo));
            var currentCheck = DateFrom;
            TimeSpan summaryDiff = new TimeSpan();
            while (currentCheck < DateTo)
            {
                var firstLogon = GetFirstSecurityLogonThatDay(currentCheck);
                var lastLogon = GetLastSecurityLogoffThatDay(currentCheck);
                if (firstLogon.Date != currentCheck) throw new IndexOutOfRangeException(nameof(firstLogon));
                if (lastLogon.Date != currentCheck) throw new IndexOutOfRangeException(nameof(lastLogon));
                if (lastLogon < firstLogon) throw new IndexOutOfRangeException(nameof(lastLogon));
                summaryDiff += lastLogon - firstLogon;
                currentCheck = currentCheck.AddDays(1);
            }
            return summaryDiff.TotalHours;
        }

        private string windowsIdentityName = null;
        public string WindowsIdentityName
        {
            get
            {
                if (windowsIdentityName == null)
                {
                    windowsIdentityName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                }
                return windowsIdentityName;
            }
        }
        public DateTime GetFirstSecurityLogonThatDay(DateTime date)
        {
            EventLog log = EventLog.GetEventLogs().First(o => o.Log == "Security");
            var splited = WindowsIdentityName.Split('\\');
            var nameToCheckOn = splited.Last();
            var logon = log.Entries.Cast<EventLogEntry>()
                .Where(entry => entry.TimeWritten.Date == date.Date && entry.InstanceId == 4624 && entry.Message.Contains(nameToCheckOn))
                .OrderBy(i => i.TimeWritten)
                .FirstOrDefault();
            if (logon == null) return date;
            return logon.TimeWritten;
        }
        public DateTime GetLastSecurityLogoffThatDay(DateTime date)
        {
            EventLog log = EventLog.GetEventLogs().First(o => o.Log == "Security");
            var splited = WindowsIdentityName.Split('\\');
            var nameToCheckOn = splited.Last();
            var logon = log.Entries.Cast<EventLogEntry>()
                .Where(entry => entry.TimeWritten.Date == date.Date && entry.InstanceId == 4634 && entry.Message.Contains(nameToCheckOn))
                .OrderByDescending(i => i.TimeWritten)
                .FirstOrDefault();
            if (logon == null) return date;
            return logon.TimeWritten;
        }
    }
}

