using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LogViewer.ViewModels
{
    public class LogViewerViewModel
    {
        #region Data Members

        private List<LogEntry> _entries = new List<LogEntry>();

        #endregion

        #region Prroperties

        public ICollectionView EntryCollection { get; set; }

        public List<LogEntry> Entries
        {
            get { return _entries; }
            set { _entries = value; }
        }

        public List<ELogLevelType> LevelList { get; set; }

        #endregion

        #region Constractors/initializations

        public LogViewerViewModel()
        {
            LevelList = new List<ELogLevelType>(Enum.GetValues(typeof(ELogLevelType)).Cast<ELogLevelType>().ToList());
        }

        #endregion

    }
}
