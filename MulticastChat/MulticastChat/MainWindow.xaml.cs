using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;

namespace MulticastChat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool done = true;   //флаг останвки следующего потока
        private UdpClient client;   //сокет клиента
        private IPAddress groupAddress; //групповой адрес рассылки
        private int localPort;  //локальный порт для приёма сообщений
        private int remotePort; //удалённый порт для отправки сообщений
        private int ttl;

        private IPEndPoint remoteEndPoint;
        private UnicodeEncoding encoding = new UnicodeEncoding();

        private string name;    //имя юзера в разговоре
        private string massage; //сообщение для отправки

        private readonly SynchronizationContext _syncContext;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                NameValueCollection configuration = ConfigurationSettings.AppSettings;
                groupAddress = IPAddress.Parse(configuration["GroupAddress"]);
                localPort = int.Parse(configuration["LocalPort"]);
                remotePort = int.Parse(configuration["RemotePort"]);
                ttl = int.Parse(configuration["TTL"]);
            }
            catch
            {
                MessageBox.Show(this, "Error in app configuration file!", "Error Multicast Chart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            _syncContext = SynchronizationContext.Current;
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            name = textName.Text;
            textName.IsReadOnly = true;

            try
            {
                //присоединяемся к группе рассылки
                client = new UdpClient(localPort);
                client.JoinMulticastGroup(groupAddress, ttl);

                remoteEndPoint = new IPEndPoint(groupAddress, remotePort);

                Thread receiver = new Thread(new ThreadStart(Listener));
                receiver.IsBackground = true;
                receiver.Start();

                byte[] data = encoding.GetBytes(name + " has joined the chat");
                client.Send(data, data.Length, remoteEndPoint);

                buttonStart.IsEnabled = false;
                buttonStop.IsEnabled = true;
                buttonSend.IsEnabled = true;
            }
            catch (SocketException ex)
            {
                MessageBox.Show(this, ex.Message, "Error Multicast Chart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Listener()
        {
            done = false;

            try
            {
                while (!done)
                {
                    IPEndPoint ep = null;
                    byte[] buffer = client.Receive(ref ep);
                    massage = encoding.GetString(buffer);

                    _syncContext.Post(o => DisplayReceivedMassage(), null);
                }
            }
            catch(Exception ex)
            {
                if (done)
                    return;
                else
                    MessageBox.Show(this, ex.Message, "Error Multicast Chart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayReceivedMassage()
        {
            string time = DateTime.Now.ToString("t");
            textMassages.Text = time + " " + massage + "\r\n" + textMassages.Text;
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            StopListener();
        }

        private void StopListener()
        {
            try
            {
                byte[] data = encoding.GetBytes(name + " has left the chart");
                client.Send(data, data.Length, remoteEndPoint);

                client.DropMulticastGroup(groupAddress);
                client.Close();

                done = true;

                buttonStart.IsEnabled = true;
                buttonStop.IsEnabled = false;
                buttonSend.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error Multicast Chart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] data = encoding.GetBytes(name + ": " + textMassage.Text);
                client.Send(data, data.Length, remoteEndPoint);
                textMassage.Clear();
                textMassage.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error Multicast Chart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!done)
                StopListener();
        }
    }
}
