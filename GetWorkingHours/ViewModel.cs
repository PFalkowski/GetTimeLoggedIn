using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;

namespace GetWorkingHours
{
    public class ViewModel : BindableBase
    {
        public ViewModel()
        {
            CalculateResultCommand = new DelegateCommand(async () => await CalculateResultAsync());
        }
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
                    RaisePropertyChanged();
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
                    RaisePropertyChanged();
                }
            }
        }
        private string _textBoxContent;
        public string TextBoxContent
        {
            get
            {
                return _textBoxContent;
            }
            set
            {
                if (value != _textBoxContent)
                {
                    _textBoxContent = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ICommand CalculateResultCommand {get; set; }
        private async Task CalculateResultAsync()
        {
            TextBoxContent = FormatTimespanUserFriendly(await GetDiffBetweenChoosenDates());
        }
        private string FormatTimespanUserFriendly(TimeSpan span)
        {
            return $"{Math.Round(span.TotalHours, 2)} hour(s)";
        }
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

        private async Task<TimeSpan> GetDiffBetweenChoosenDates()
        {
            if (DateFrom.Date > DateTo.Date) throw new ArgumentException(nameof(DateTo));
            var currentCheck = DateFrom;
            TimeSpan summaryDiff = new TimeSpan();
            while (currentCheck <= DateTo)
            {
                var firstLogon = await GetFirstSecurityLogonThatDay(currentCheck).ConfigureAwait(false);
                var lastLogon = await GetLastSecurityLogoffThatDay(currentCheck).ConfigureAwait(false);
                if (firstLogon.Date != currentCheck.Date) throw new IndexOutOfRangeException(nameof(firstLogon));
                if (lastLogon.Date != currentCheck.Date) throw new IndexOutOfRangeException(nameof(lastLogon));
                if (lastLogon < firstLogon) throw new IndexOutOfRangeException(nameof(lastLogon));
                summaryDiff += lastLogon - firstLogon;
                currentCheck = currentCheck.AddDays(1);
            }
            return summaryDiff;
        }
        public async Task<DateTime> GetFirstSecurityLogonThatDay(DateTime date)
        {
            DateTime result = DateTime.MaxValue;
            await Task.Run(() =>
            {
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
            }).ConfigureAwait(false);
            if (result == DateTime.MaxValue)
                return date;
            else return result;
        }
        public async Task<DateTime> GetLastSecurityLogoffThatDay(DateTime date)
        {
            DateTime result = DateTime.MinValue; await Task.Run(() =>
            {
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
            }).ConfigureAwait(false);
            if (result == DateTime.MinValue)
                return date;
            else return result;
        }
    }
}

