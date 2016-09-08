using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LogViewer
{
    /// <summary>
    /// Interaction logic for Filter.xaml
    /// </summary>
    public partial class Filter : Window
    {
        private List<LogEntry> _entries ;

        public List<LogEntry> Entries
        {
            get { return _entries; }
            set { _entries = value; }
        }

        public string UserName
        {
            get { return this.textBoxUserName.Text; }
        }

        public string Level
        {
            get 
            { 
                if (this.comboBoxLevel.SelectedIndex != -1)
                {
                    return this.comboBoxLevel.SelectedValue.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string Message
        {
            get { return this.textBoxMessage.Text; }
        }

        public Filter(List<LogEntry> entries)
        {
            Entries = entries;
            InitializeComponent();
            PopulateLevelDropDown();
        }

        private void PopulateLevelDropDown()
        {
            var levels = (from e in Entries select e.Level).Distinct().ToList<string>();
            this.comboBoxLevel.ItemsSource = levels;
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            this.textBoxUserName.Text = string.Empty;
            this.textBoxMessage.Text = string.Empty;
            this.comboBoxLevel.SelectedIndex = -1;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
