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
                    TextBoxContent = GetDiffBetweenChoosenDates().ToString();
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
                    TextBoxContent = GetDiffBetweenChoosenDates().ToString();
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(TextBoxContent));
                }
            }
        }
        public string TextBoxContent { get; set; }
        private static string WindowsIdentityName { get; } = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private static string FormattedIdentityName
        {
            get
            {
                var splited = WindowsIdentityName.Split('\\');
                return splited.Last();
            }
        }
        private static IEnumerable<EventLogEntry> EagerLog()
        {
            EventLog log = EventLog.GetEventLogs().First(o => o.Log == "Security");
            var logon = log.Entries.Cast<EventLogEntry>();
            return logon;
        }
        private static Lazy<IEnumerable<EventLogEntry>> LazyLog { get; set; } = new Lazy<IEnumerable<EventLogEntry>>(() => { return EagerLog(); });
        private static Lazy<IEnumerable<EventLogEntry>> LazyLogOnLog { get; set; } = new Lazy<IEnumerable<EventLogEntry>>(() => 
        {
            return LazyLog.Value.Where(entry => entry.InstanceId == 4624 && entry.Message.Contains(FormattedIdentityName));
        });
        private static Lazy<IEnumerable<EventLogEntry>> LazyLogOffLog { get; set; } = new Lazy<IEnumerable<EventLogEntry>>(() =>
        {
            return LazyLog.Value.Where(entry => entry.InstanceId == 4634 && entry.Message.Contains(FormattedIdentityName));
        });

        private TimeSpan GetDiffBetweenChoosenDates()
        {
            if (DateFrom.Date > DateTo.Date) throw new ArgumentException(nameof(DateTo));
            var currentCheck = DateFrom;
            TimeSpan summaryDiff = new TimeSpan();
            while (currentCheck <= DateTo)
            {
                var firstLogon = GetFirstSecurityLogonThatDay(currentCheck);
                var lastLogon = GetLastSecurityLogoffThatDay(currentCheck);
                if (firstLogon.Date != currentCheck) throw new IndexOutOfRangeException(nameof(firstLogon));
                if (lastLogon.Date != currentCheck) throw new IndexOutOfRangeException(nameof(lastLogon));
                if (lastLogon < firstLogon) throw new IndexOutOfRangeException(nameof(lastLogon));
                summaryDiff += lastLogon - firstLogon;
                currentCheck = currentCheck.AddDays(1);
            }
            return summaryDiff;
        }
        public DateTime GetFirstSecurityLogonThatDay(DateTime date)
        {
            DateTime result = DateTime.MaxValue;
            foreach (var entry in LazyLogOnLog.Value)
            {
                if (entry.TimeWritten.Date == date.Date)
                {
                    if (result > entry.TimeWritten)
                    {
                        result = entry.TimeWritten;
                    }
                }
            }
            if (result == DateTime.MaxValue)
                return date;
            else return result;
        }
        public DateTime GetLastSecurityLogoffThatDay(DateTime date)
        {
            DateTime result = DateTime.MinValue;
            foreach (var entry in LazyLogOffLog.Value)
            {
                if (entry.TimeWritten.Date == date.Date)
                {
                    if (result < entry.TimeWritten)
                    {
                        result = entry.TimeWritten;
                    }
                }
            }
            if (result == DateTime.MinValue)
                return date;
            else return result;
        }
    }
}

