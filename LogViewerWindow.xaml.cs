using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;
using System.Reflection;
using LogViewer.Helpers;
using LogViewer.ViewModels;


namespace LogViewer
{
    public partial class LogViewerWindow : Window
    {
        #region Data Members

        private string _fileName = string.Empty;
        private List<LogEntry> _entries = new List<LogEntry>();
        private int _currentIndex = 0;
        private bool _isLoaded;
        private ListSortDirection _direction = ListSortDirection.Descending;
        private string _filterText;

        #endregion

        #region Properties

        private string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                RecentFileList.InsertFile(value);
            }
        }

        public List<LogEntry> Entries
        {
            get { return _entries; }
            set { _entries = value; }
        }

        public ICollectionView EntryCollection { get; set; }

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                _filterText = value;
                FilterEntries();
            }
        }

        private void FilterEntries()
        {
            string level = comboBoxLevel.SelectedItem.ToString();
            string text = _filterText;
            if (string.IsNullOrEmpty(text) && level == ELogLevelType.All.ToString())
            {
                EntryCollection.Filter = null;
                return;
            }

            if(level == ELogLevelType.All.ToString() && !string.IsNullOrEmpty(text))
            {
                EntryCollection.Filter = item =>
                {
                    LogEntry vitem = item as LogEntry;
                    return vitem != null && vitem.Message.ToLower().Contains(text.ToLower());
                };
            }
            else if (level != ELogLevelType.All.ToString() && string.IsNullOrEmpty(text))
            {
                EntryCollection.Filter = item =>
                {
                    LogEntry vitem = item as LogEntry;
                    return vitem != null && vitem.Level.ToLower() == level.ToLower();
                };
            }
            else if (level != ELogLevelType.All.ToString() && !string.IsNullOrEmpty(text))
            {
                EntryCollection.Filter = item =>
                {
                    LogEntry vitem = item as LogEntry;
                    return vitem != null && vitem.Message.ToLower().Contains(text.ToLower())
                                         && vitem.Level.ToLower() == level.ToLower();
                };
            }
        }
        #endregion

        #region Constractors/Initializition

        public LogViewerWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            listView1.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ListView1_HeaderClicked));

            RecentFileList.UseXmlPersister();
            RecentFileList.MenuClick += (s, e) => OpenFile(e.Filepath);

            InitLevelIcons();

            Title = string.Format("LogViewer  v.{0}", Assembly.GetExecutingAssembly().GetName().Version);

            EntryCollection = CollectionViewSource.GetDefaultView(Entries);
            PopulateLevelDropDown();
        }

        private void InitLevelIcons()
        {
            imageError.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Error.Handle, Int32Rect.Empty, null);
            imageInfo.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Information.Handle, Int32Rect.Empty, null);
            imageWarn.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Warning.Handle, Int32Rect.Empty, null);
            imageDebug.Source = Imaging.CreateBitmapSourceFromHIcon(SystemIcons.Question.Handle, Int32Rect.Empty, null);
        }

        private void PopulateLevelDropDown()
        {
            var levels = Enum.GetValues(typeof(ELogLevelType)).Cast<ELogLevelType>();
            this.comboBoxLevel.ItemsSource = levels;
            comboBoxLevel.SelectedIndex = 0;
        }

        #endregion

        #region Private Methods

        private void Clear()
        {
            _entries.Clear();
            listView1.ItemsSource = null;
            textBoxMessage.Text = string.Empty;
            textBoxThrowable.Text = string.Empty;
        }

        private void OpenFile(string fileName)
        {
            FileName = fileName;
            LoadFile();
            ShowLevelsCount();
        }

        private void ShowLevelsCount()
        {
            int errorCount =
                (
                    from entry in Entries
                    where entry.Level == "ERROR"
                    select entry
                    ).Count();

            if (errorCount > 0)
            {
                labelErrorCount.Content = string.Format("{0:#,#}  ", errorCount);
                labelErrorCount.Visibility = Visibility.Visible;
                imageError.Visibility = Visibility.Visible;
            }
            else
            {
                labelErrorCount.Visibility = Visibility.Hidden;
                imageError.Visibility = Visibility.Hidden;
            }

            int infoCount =
                (
                    from entry in Entries
                    where entry.Level == "INFO"
                    select entry
                    ).Count();

            if (infoCount > 0)
            {
                labelInfoCount.Content = string.Format("{0:#,#}  ", infoCount);
                labelInfoCount.Visibility = Visibility.Visible;
                imageInfo.Visibility = Visibility.Visible;
            }
            else
            {
                labelInfoCount.Visibility = Visibility.Hidden;
                imageInfo.Visibility = Visibility.Hidden;
            }

            int warnCount =
                (
                    from entry in Entries
                    where entry.Level == "WARN"
                    select entry
                    ).Count();

            if (warnCount > 0)
            {
                labelWarnCount.Content = string.Format("{0:#,#}  ", warnCount);
                labelWarnCount.Visibility = Visibility.Visible;
                imageWarn.Visibility = Visibility.Visible;
            }
            else
            {
                labelWarnCount.Visibility = Visibility.Hidden;
                imageWarn.Visibility = Visibility.Hidden;
            }

            int debugCount =
                (
                    from entry in Entries
                    where entry.Level == "DEBUG"
                    select entry
                    ).Count();

            if (debugCount > 0)
            {
                labelDebugCount.Content = string.Format("{0:#,#}  ", debugCount);
                labelDebugCount.Visibility = Visibility.Visible;
                imageDebug.Visibility = Visibility.Visible;
            }
            else
            {
                labelDebugCount.Visibility = Visibility.Hidden;
                labelDebugCount.Visibility = Visibility.Hidden;
            }

        }

        private void LoadFile()
        {
            textboxFileName.Text = FileName;

            Clear();

            XMLReader.LoadXmlFile(FileName, _entries);

            listView1.ItemsSource = _entries;
            _isLoaded = true;
        }

        #endregion

        #region ListView Events
        ////////////////////////////////////////////////////////////////////////////////
        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                LogEntry logentry = this.listView1.SelectedItem as LogEntry;
                if (logentry != null)
                {
                    this.textBoxMessage.Text = logentry.Message;
                    this.textBoxThrowable.Text = logentry.Throwable;
                }
            }
            catch { }
        }

        private void ListView1_HeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
            ListView source = e.Source as ListView;
            try
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(source.ItemsSource);
                dataView.SortDescriptions.Clear();
                _direction = _direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                SortDescription description = new SortDescription(header.Content.ToString(), _direction);
                dataView.SortDescriptions.Add(description);
                dataView.Refresh();
            }
            catch (Exception)
            {
            }
        }
        ////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region DragDrop
        ////////////////////////////////////////////////////////////////////////////////
        private delegate void VoidDelegate();
        private void listView1_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                try
                {
                    Array a = (Array)e.Data.GetData(DataFormats.FileDrop);
                    if (a != null)
                    {
                        FileName = a.GetValue(0).ToString();
                        Dispatcher.BeginInvoke(
                            DispatcherPriority.Background,
                            new VoidDelegate(delegate { LoadFile(); })
                            );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in Drag Drop: " + ex.Message);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        #endregion

        #region Events Handlers

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        private void MenuFileOpen_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog oOpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            if (oOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileName = oOpenFileDialog.FileName;
                LoadFile();
            }
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFile();
            listView1.SelectedIndex = listView1.Items.Count - 1;
            if (listView1.Items.Count > 4)
            {
                listView1.SelectedIndex -= 3;
            }
            listView1.ScrollIntoView(listView1.SelectedItem);
            ListViewItem lvi = listView1.ItemContainerGenerator.ContainerFromIndex(listView1.SelectedIndex) as ListViewItem;
            if (lvi != null)
            {
                lvi.BringIntoView();
                lvi.Focus();
            }
        }

        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Find(int direction)
        {
            if (textBoxFind.Text.Length > 0)
            {
                if (direction == 0)
                {
                    for (int i = _currentIndex + 1; i < listView1.Items.Count; i++)
                    {
                        LogEntry item = (LogEntry)listView1.Items[i];
                        if (item.Message.Contains(textBoxFind.Text))
                        {
                            listView1.SelectedIndex = i;
                            listView1.ScrollIntoView(listView1.SelectedItem);
                            ListViewItem lvi = listView1.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                            lvi.BringIntoView();
                            lvi.Focus();
                            _currentIndex = i;
                            break;
                        }
                    }
                }
                else
                {
                    for (int i = _currentIndex - 1; i > 0 && i < listView1.Items.Count; i--)
                    {
                        LogEntry item = (LogEntry)listView1.Items[i];
                        if (item.Message.Contains(textBoxFind.Text))
                        {
                            listView1.SelectedIndex = i;
                            listView1.ScrollIntoView(listView1.SelectedItem);
                            ListViewItem lvi = listView1.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                            lvi.BringIntoView();
                            lvi.Focus();
                            _currentIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void buttonFindNext_Click(object sender, RoutedEventArgs e)
        {
            Find(0);
        }

        private void buttonFindPrevious_Click(object sender, RoutedEventArgs e)
        {
            Find(1);
        }

        private void textBoxFind_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (textBoxFind.Text.Length > 0)
                {
                    Find(0);
                }
            }
        }

        private void ComboBoxLevel_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterEntries();
        }

        #endregion
    }

}
