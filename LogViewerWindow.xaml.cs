using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Interop;
using System.Reflection;
using LogViewer.ViewModels;


namespace LogViewer
{
    public partial class LogViewerWindow : Window
    {
        #region Data Members

        private string _fileName = string.Empty;
        private List<LogEntry> _entries = new List<LogEntry>();
        private int _currentIndex = 0;

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
            var levels = Enum.GetValues(typeof (ELogLevelType)).Cast<ELogLevelType>();
            this.comboBoxLevel.ItemsSource = levels;
        }

        #endregion

        #region Private Methods

        private void Clear()
        {
            textBoxMessage.Text = string.Empty;
            textBoxThrowable.Text = string.Empty;
        }

        private void OpenFile(string fileName)
        {
            FileName = fileName;
            LoadFile();
        }

        private void LoadFile()
        {
            textboxFileName.Text = FileName;
            _entries.Clear();
            listView1.ItemsSource = null;

            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string sXml = string.Empty;
            int iIndex = 1;

            Clear();

            try
            {
                FileStream oFileStream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read,
                    FileShare.ReadWrite);
                StreamReader oStreamReader = new StreamReader(oFileStream);
                var sBuffer = string.Format("<root>{0}</root>", oStreamReader.ReadToEnd());
                oStreamReader.Close();
                oFileStream.Close();

                #region Read File Buffer

                ////////////////////////////////////////////////////////////////////////////////
                StringReader oStringReader = new StringReader(sBuffer);
                XmlTextReader oXmlTextReader = new XmlTextReader(oStringReader);
                oXmlTextReader.Namespaces = false;
                while (oXmlTextReader.Read())
                {
                    if ((oXmlTextReader.NodeType == XmlNodeType.Element) && (oXmlTextReader.Name == "log4j:event"))
                    {
                        LogEntry logentry = new LogEntry();

                        logentry.Item = iIndex;

                        double dSeconds = Convert.ToDouble(oXmlTextReader.GetAttribute("timestamp"));
                        logentry.TimeStamp = dt.AddMilliseconds(dSeconds).ToLocalTime();
                        logentry.Thread = oXmlTextReader.GetAttribute("thread");

                        #region get level

                        ////////////////////////////////////////////////////////////////////////////////
                        logentry.Level = oXmlTextReader.GetAttribute("level");
                        switch (logentry.Level)
                        {
                            case "ERROR":
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Error);
                                break;
                            }
                            case "INFO":
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Info);
                                break;
                            }
                            case "DEBUG":
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Debug);
                                break;
                            }
                            case "WARN":
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Warn);
                                break;
                            }
                            case "FATAL":
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Fatal);
                                break;
                            }
                            default:
                            {
                                logentry.Image = LogEntry.Images(LogEntry.ImageType.Custom);
                                break;
                            }
                        }
                        ////////////////////////////////////////////////////////////////////////////////

                        #endregion

                        #region read xml

                        ////////////////////////////////////////////////////////////////////////////////
                        while (oXmlTextReader.Read())
                        {
                            if (oXmlTextReader.Name == "log4j:event") // end element
                                break;
                            else
                            {
                                switch (oXmlTextReader.Name)
                                {
                                    case ("log4j:message"):
                                    {
                                        logentry.Message = oXmlTextReader.ReadString();
                                        break;
                                    }
                                    case ("log4j:data"):
                                    {
                                        switch (oXmlTextReader.GetAttribute("name"))
                                        {
                                            case ("log4jmachinename"):
                                            {
                                                logentry.MachineName = oXmlTextReader.GetAttribute("value");
                                                break;
                                            }
                                            case ("log4net:HostName"):
                                            {
                                                logentry.HostName = oXmlTextReader.GetAttribute("value");
                                                break;
                                            }
                                            case ("log4net:UserName"):
                                            {
                                                logentry.UserName = oXmlTextReader.GetAttribute("value");
                                                break;
                                            }
                                            case ("log4japp"):
                                            {
                                                logentry.App = oXmlTextReader.GetAttribute("value");
                                                break;
                                            }
                                        }
                                        break;
                                    }
                                    case ("log4j:throwable"):
                                    {
                                        logentry.Throwable = oXmlTextReader.ReadString();
                                        break;
                                    }
                                    case ("log4j:locationInfo"):
                                    {
                                        logentry.Class = oXmlTextReader.GetAttribute("class");
                                        logentry.Method = oXmlTextReader.GetAttribute("method");
                                        logentry.File = oXmlTextReader.GetAttribute("file");
                                        logentry.Line = oXmlTextReader.GetAttribute("line");
                                        break;
                                    }
                                }
                            }
                        }
                        ////////////////////////////////////////////////////////////////////////////////

                        #endregion

                        _entries.Add(logentry);
                        iIndex++;

                        #region Show Counts

                        ////////////////////////////////////////////////////////////////////////////////
                        int ErrorCount =
                            (
                                from entry in Entries
                                where entry.Level == "ERROR"
                                select entry
                                ).Count();

                        if (ErrorCount > 0)
                        {
                            labelErrorCount.Content = string.Format("{0:#,#}  ", ErrorCount);
                            labelErrorCount.Visibility = Visibility.Visible;
                            imageError.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            labelErrorCount.Visibility = Visibility.Hidden;
                            imageError.Visibility = Visibility.Hidden;
                        }

                        int InfoCount =
                            (
                                from entry in Entries
                                where entry.Level == "INFO"
                                select entry
                                ).Count();

                        if (InfoCount > 0)
                        {
                            labelInfoCount.Content = string.Format("{0:#,#}  ", InfoCount);
                            labelInfoCount.Visibility = Visibility.Visible;
                            imageInfo.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            labelInfoCount.Visibility = Visibility.Hidden;
                            imageInfo.Visibility = Visibility.Hidden;
                        }

                        int WarnCount =
                            (
                                from entry in Entries
                                where entry.Level == "WARN"
                                select entry
                                ).Count();

                        if (WarnCount > 0)
                        {
                            labelWarnCount.Content = string.Format("{0:#,#}  ", WarnCount);
                            labelWarnCount.Visibility = Visibility.Visible;
                            imageWarn.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            labelWarnCount.Visibility = Visibility.Hidden;
                            imageWarn.Visibility = Visibility.Hidden;
                        }

                        int DebugCount =
                            (
                                from entry in Entries
                                where entry.Level == "DEBUG"
                                select entry
                                ).Count();

                        if (DebugCount > 0)
                        {
                            labelDebugCount.Content = string.Format("{0:#,#}  ", DebugCount);
                            labelDebugCount.Visibility = Visibility.Visible;
                            imageDebug.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            labelDebugCount.Visibility = Visibility.Hidden;
                            labelDebugCount.Visibility = Visibility.Hidden;
                        }
                        ////////////////////////////////////////////////////////////////////////////////

                        #endregion
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////

                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            listView1.ItemsSource = _entries;
        }

        #endregion

        #region ListView Events
        ////////////////////////////////////////////////////////////////////////////////
        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Clear();
                LogEntry logentry = this.listView1.SelectedItem as LogEntry;
                this.textBoxMessage.Text = logentry.Message;
                this.textBoxThrowable.Text = logentry.Throwable;

            }
            catch { }
        }

        private ListSortDirection _Direction = ListSortDirection.Descending;

        private void ListView1_HeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
            ListView source = e.Source as ListView;
            try
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(source.ItemsSource);
                dataView.SortDescriptions.Clear();
                _Direction = _Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                SortDescription description = new SortDescription(header.Content.ToString(), _Direction);
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
            string level = comboBoxLevel.SelectedItem.ToString();
            if (level == ELogLevelType.All.ToString())
            {
                EntryCollection.Filter = null;
            }
            else
            {
                EntryCollection.Filter = item =>
                {
                    LogEntry vitem = item as LogEntry;
                    return vitem != null && vitem.Level.ToLower() == level.ToLower();
                };
            }
        }

        #endregion
    }

}
