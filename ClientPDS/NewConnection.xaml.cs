using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
   
    //Finestra di avvio: permette di impostare i parametri della connessione come IP e Porta del server
    public partial class NewConnection : Window
    {
       
        // IAsyncResult è un'interfaccia utilizzata da classi che contengono metodi asincroni.
        // La classe NewConnection contiene metodi asincroni perchè deve gestire la connessione con un server.
        private IAsyncResult connectionResult;
        private ObservableCollection<string> IPaddresses;
 
        // TcpClient fornisce le funzioni basiche per instaurare una connessione TCP
        private TcpClient client;
        

        // Classe utilizzata per passare informazioni relative al client alla callback di connessione
        private class ClientProperties
        {
            public TcpClient client { get; set; }
            public string address { get; set; }
        }

        // Costruttore della classe NewConnection. Imposta semplicemente il focus sul box relativo all'indirizzo IP da inserire.
        public NewConnection()
        {
            InitializeComponent();

            //Inizializzo la lista degli indirizzi IP collegata alla ComboBox che conterrà la storia degli indirizzi immessi.
            IPaddresses = new ObservableCollection<string>();


            //Gestione di un file che contiene la storia degli indirizzi IP immessi
            int counter = 0;
            string line;
           
            //leggo gli indirizzi IP dal file e li aggiungo alla ObservableCollection
            FileInfo f = new FileInfo("IPdatabase.txt");
            FileStream s = f.Open(FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader reader = new StreamReader(s);
            while ((line = reader.ReadLine()) != null)
            {
                IPaddresses.Add(line);
                counter++;
            }
            s.Close();
            IPaddressesBox.ItemsSource = IPaddresses;
            txtAddress1.Focus();
        }

      
        //Funzione invocata dall'evento di inserimento testo.
        //Impedisce di inserire nella textbox della Porta elementi che non siano numeri
        public void IsAllowedCharacter(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[0-9]+");

            // Se il carattere inserito è diverso da un numero (Non si ha match con la regular expression)
            if (regex.IsMatch(e.Text) == false)
                e.Handled = true;

        }

        //funzione invocata da ConnectButton_Click, ritorna true se la stringa è un indirizzo IPv4 valido.
        public bool IsValidIP(string input)
        {
            Regex ip = new Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            bool ipvalid = false;
            MatchCollection result = ip.Matches(input);
            int occurrences = result.Count;
            if (occurrences == 1)
            {
                ipvalid = true;
            }

            return ipvalid;
        }
        
        /*Associata all'evento click del Bottone Connetti, tenta la connessione all'indirizzo IP specificato
         -controlla che l'ip sia valido
         -inserisce l'ip nel file di storia
         -controlla se esiste già una connessione attiva a tale indirizzo
         -si tenta la connessione con BeginConnect e si fa una chiamata asincrona a ConnectionRequest
          che in caso di successo, crea una MainWindow oppure una nuova tab da aggiungere ad essa.
        */
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string input = txtAddress1.Text;

            if (!IsValidIP(input))
            {
                MessageBox.Show("Inserire un ip valido", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string address = input;
           
            //controlla se indirizzo IP esiste già nel file, e non lo scrivere nel caso
            FileInfo fail = new FileInfo("IPdatabase.txt");
            FileStream s = fail.Open(FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(s);
            string line;
            bool exists = false;
            while ((line = reader.ReadLine()) != null)
            {
                if (address == line) {
                    exists = true;
                }
            }
            s.Close();


            if (!exists) {
                FileInfo f = new FileInfo("IPdatabase.txt");
                StreamWriter writer = f.AppendText();
                writer.WriteLine(address); //scrivo nel file l'indirizzo 
                IPaddresses.Add(address); //scrivo nella lista l'indirizzo
                writer.Close();
            }



            if (address == "0.0.0.0")
            {
                MessageBox.Show("Impossibile connettersi all'host specificato", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Int32 port;
            try
            {
                // Conversione del testo della porta in Int32 e gestione delle eventuali eccezioni
                port = Convert.ToInt32(txtPort.Text);
            }
            catch (FormatException)
            {
                port = 2000;
            }
            catch (OverflowException)
            {
                port = 2000;
            }

            // Verifica dell'eventuale esistenza di un tab connesso allo stesso indirizzo
            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is MainWindow)
                {
                    MainWindow m = w as MainWindow;

                    if (m.ActiveConnectionsIPList.Contains(address))
                    {
                        MessageBox.Show("Il server del quale si è inserito l'indirizzo IP è già collegato ");
                        return;
                    }
                }
            }
            // Nel caso in cui l'indirizzo indicato non sia relativo ad alcun server già connesso, si procede normalmente
            Console.WriteLine("Connessione verso: {0} - {1}", address, port);

            try
            {
                client = new TcpClient();  //creo un oggetto TCPClient per poter usare i suoi metodi che mi permettono di creare
                                           //un socket verso il server.

                //l'oggetto properties permette al thread che gestisce il 
                ClientProperties properties = new ClientProperties();
                properties.client = client;
                properties.address = address; //address estratto dalla textbox

                // Invia una richiesta di connessione asincrona: il client non si blocca in attesa del risultato
                // La callback specificata come parametro viene lanciata quando l'operazione di connessione è completa

                this.connectionResult = client.BeginConnect(address, port, new AsyncCallback(ConnectionRequest), properties);

                // Disabilitazione dell'interfaccia per evitare richieste simultanee che non sono gestibili
                ConnectButton.IsEnabled = false;
                txtAddress1.IsEnabled = false;
                txtPort.IsEnabled = false;
                IPaddressesBox.IsEnabled = false;
                this.Cursor = Cursors.AppStarting; // Cursore relativo ad un'app appena lanciata
            }
            catch (SecurityException)
            {
                MessageBox.Show("Accesso negato: non hai i permessi necessari", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (ObjectDisposedException)
            {
                MessageBox.Show("Errore di connessione", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show("Numero di porta non valido. I valori ammessi sono [1 - 65536]", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SocketException)
            {
                MessageBox.Show("Connessione fallita", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

      
        /* Funzione che tenta di instaurare la connessione. Riavvia l'interfaccia di connessione se la connessione fallisce.
           In caso di connessione riuscita, crea la Window principale (MainWindow) se non già esistente
           Se già esistente invece, perchè già si è collegati ad un altro server, crea un nuovo tab
        */
        private void ConnectionRequest(IAsyncResult result)
        {
            ClientProperties properties = result.AsyncState as ClientProperties;

            // Nel caso in cui la connessione fallisce

            if ((properties == null) || (!properties.client.Connected))
            {
                MessageBox.Show("Connessione fallita");

                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
                {
                    // Abilitazione dei vari componenti dell'interfaccia
                    ConnectButton.IsEnabled = true;
                    txtAddress1.IsEnabled = true;
                    txtPort.IsEnabled = true;
                    IPaddressesBox.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;
                }));

                return;
            }

            NetworkStream stream = properties.client.GetStream();

            // Imposto il tempo in cui il client si mette in attesa di ricevere dati
            stream.ReadTimeout = 5000;

            // Se la connessione ha avuto successo, bisogna verificare se esiste già una MainWindow
            //  - Se esiste, significa che non bisogna crearne una nuova, ma bisogna solo aggiungere un tab
            //  - Se non esiste, significa che bisogna crearne una nuova


            //il dispatcher è un oggetto associato alla UIThread ed è condiviso tra più thread.
            //un thread secondario non può aggiornare la grafica percui deve inserire nel dispatcher
            //un metodo per farlo. Questo metodo inserito nel dispatcher verrà eseguito dallo UIthread.

            //BeginInvoke fa eseguire tale metodo in maniera asincrona a differenza di invoke che è bloccante.

            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
            {
                // Caso in cui MainWindow esiste già
                foreach (Window window in System.Windows.Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        MainWindow w = window as MainWindow;
                        w.NewTab(properties.client, stream, properties.address);
                        w.ActiveConnectionsIPList.Add(properties.address);
                        this.Close();
                        return;
                    }
                }

                // Caso in cui MainWindows non esiste: creazione di una nuova MainWindow
                MainWindow main = new MainWindow(properties.client, stream, properties.address);
                main.ActiveConnectionsIPList.Add(properties.address);
                this.Close();
                main.Show();
            }));

            try
            {
                properties.client.EndConnect(result);
            }
            catch (SocketException)
            {
                // In caso di errore sul socket: chiusura del nuovo tab
                ExceptionHandler.ConnectionError();
            }
            catch (ObjectDisposedException)
            {
                // In caso di chiusura del socket: chiusura del tab
                ExceptionHandler.ConnectionError();
            }
        }

        //Se viene selezionato un IP nella ComboBox, questo viene inserito nella TextBox.
        private void chooseIP(object sender, SelectionChangedEventArgs e)
        {
            txtAddress1.Text = IPaddressesBox.SelectedItem as string;
        }
    }
}
