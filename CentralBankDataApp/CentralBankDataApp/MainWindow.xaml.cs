using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;

namespace CentralBankDataApp
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            XDocument xdoc = XDocument.Load("http://www.cbr.ru/scripts/XML_daily.asp?date_req=05/05/2020");
            foreach (XElement phoneElement in xdoc.Element("ValCurs").Elements("Valute"))
            {
                XAttribute id = phoneElement.Attribute("ID");
                XElement name = phoneElement.Element("Name");
                XElement priceElement = phoneElement.Element("Value");

                var newItem = new ComboBoxItem();
                newItem.Content = name.Value.ToString();
                newItem.Tag = id.Value.ToString();
                Valuta.Items.Add(newItem);
            }

            DataGridTextColumn c1 = new DataGridTextColumn();
            c1.Header = "Value";
            c1.Binding = new Binding("Value");
            c1.Width = 110;
            dgData.Columns.Add(c1);
            DataGridTextColumn c2 = new DataGridTextColumn();
            c2.Header = "Date";
            c2.Binding = new Binding("DateTime");
            c2.Width = 110;
            dgData.Columns.Add(c2);
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            DateTime? dtSatrt = Start.SelectedDate;
            DateTime? dtEnd = End.SelectedDate;
            if (dtSatrt == null || dtEnd == null)
            {
                MessageBox.Show("Установите две даты");
                return;
            }
            if (dtSatrt >= dtEnd)
            {
                MessageBox.Show("Дата начала торговли должна быть меньше даты конца торговли");
                return;
            }

            string stStart = dtSatrt.Value.ToString("dd/MM/yyyy");
            string stEnd = dtEnd.Value.ToString("dd/MM/yyyy");
            string valutaCode = ((ComboBoxItem)Valuta.SelectedItem).Tag.ToString();
            string valutaName = ((ComboBoxItem)Valuta.SelectedItem).Content.ToString();
            string query = string.Concat("http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=", stStart, "&date_req2=", stEnd, "&VAL_NM_RQ=", valutaCode);
            XDocument xdoc = XDocument.Load(query);

            currencyChart.Clear();
            dgData.Items.Clear();
            currencyChart.ChartName = valutaName;
            foreach (XElement phoneElement in xdoc.Element("ValCurs").Elements("Record"))
            {
                XAttribute date = phoneElement.Attribute("Date");
                XElement value = phoneElement.Element("Value");
                double val = double.Parse(value.Value.ToString());
                string strDate = date.Value.ToString();
                CultureInfo provider = CultureInfo.InstalledUICulture;
                DateTime dt = DateTime.ParseExact(strDate, "dd.MM.yyyy", provider);

                currencyChart.AddItem(val, dt);
                dgData.Items.Add(new DataModel() { Value = val, DateTime = dt });
            }
        }
    }
}
