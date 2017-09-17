using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;


namespace Client
{
    
    //definizione del tipo di delegato in cui verranno registrati i metodi di chiusura di ogni singola tab.
    public delegate void CloseHandler();

    public partial class MainWindow : Window
    {

        //delegato che invoca il metodo serverTabClose per ogni tab che si è registrato in esso (tutti i tab creati)
        public event CloseHandler ClosingEvent;

       
        //Lista di tab dei Server
        public ObservableCollection<MyTabItem> TabItemList { get; }

        // Proprietà che incapsula l'icona di default
        public BitmapFrame DefaultIcon { get; set; }

        //Lista delle app in Focus di tutte le tab 
        public ObservableCollection<AppInFocus> AppsInFocus {get;}

        //Lista di indirizzi IP delle connessioni attive.
        public List <string> ActiveConnectionsIPList { get; set; }

        //flag per gestire la chiusura della finestra in caso di errore sul socket.
        public bool Error { get; set; } = false;

        
        // Costruttore della classe MainWindow, crea anche un primo tab di default
        // client : Informazioni del socket 
        // stream : Informazioni sullo stream 
        // address: Indirizzo del server al quale collegarsi
        public MainWindow(TcpClient client, NetworkStream stream, String address)
        {
            InitializeComponent();

            // Icona di default, presa dalla cartella Resources dell'applicazione
            DefaultIcon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/default.ico"));

            ActiveConnectionsIPList = new List<string>();

           //Inizializzo la lista di AppsInFocus e la collego alla ListBox in cui comparirà
            AppsInFocus = new ObservableCollection<AppInFocus>();
            AppsInFocusBox.ItemsSource = AppsInFocus;

            // Permette a più thread di accedere alla lista di app in foreground, e blocca la lista stessa all'accesso
            BindingOperations.EnableCollectionSynchronization(AppsInFocus, AppsInFocus);

            //inializzo la lista dei tabs(TabItem) al TabControl che le visualizzerà.
            TabItemList = new ObservableCollection<MyTabItem>();
            ServerTabControl.ItemsSource = TabItemList;
            
            //crea un nuovo Tab
            NewTab(client, stream, address);

            //abilita l'invio dei comandi al server.
            SendCommands_Start();
        }

        //Funzione invocata dalla pressione del bottone Chiudi (X) oppure dalla chiamata a this.Close().
        private void CloseWindow (object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Se la chiusura è stata causata da un errore, esegue il delegato ClosingEvent() 
            if (Error)
            {
                //esegue il metodo serverTabClose su tutte le tab. Questo provoca la chiusura dei relativi socket che a sua volta
                //provoca la chiusura della MainWindow.
                ClosingEvent();
            } 
            else
            {
                MessageBoxResult res = MessageBox.Show("Sei sicuro di voler terminare l'applicazione?\nTutte le connessioni istaurate andranno perse.", "titolo", MessageBoxButton.YesNo);

                switch (res)
                {
                    case MessageBoxResult.Yes:
                        ClosingEvent();
                        break;

                    case MessageBoxResult.No:
                        e.Cancel = true;      //Evento rientrato
                        break;
                }

            }
                
        }

        // Funzione che chiude un singolo tab nel caso si preme Disconnetti
        public void CloseTab (MyTabItem tab)
        {
            // Caso in cui il tab da chiudere sia l'ultimo attivo: chiusura dell'intera finestra
            if (TabItemList.Count == 1)
            {
                this.Close();
                return;
            }

            
            MyTabManagement servertab = tab.TabManager;

            // In caso di chiusura della main window, la funzione di chiusura di questo tab non andrà più eseguita dal delegato
            ClosingEvent -= servertab.ServerTabClose;

            // Chiusura del tab
            servertab.ServerTabClose();

            // Rimozione di questo tab dalla lista dei tab dei server
            TabItemList.Remove(tab);

            // Rimozione dei questa connessione dalla lista di connessioni attive
            ActiveConnectionsIPList.Remove(tab.TabServerIP);

            //rimuove dalla list box l'app in focus della tab in chiusura
            int index = AppsInFocus.IndexOf(new AppInFocus(tab.AppInFocus));
            if (index != -1)
            {
               AppsInFocus.RemoveAt(index);
            }
            

        }

        //Funzione che crea un nuovo tab
        public void NewTab (TcpClient client, NetworkStream stream, String address)
        {
            
            MyTabItem tab = new MyTabItem(this);
            MyTabManagement s = new MyTabManagement(tab);
           
            //assegno la proprietà Header(ereditata da TabItem) e la proprietà "copia" interna di MyTabItem
            tab.Header = tab.TabServerIP = address; 
            

            s.Connection = client;
            s.Stream = stream;
            s.StartServerDataExchange();

            //assegno la proprietà Content(ereditata da TabItem) e la proprietà "copia" interna di MyTabItem
            tab.Content = tab.TabManager = s;
            
            // Aggiunta del nuovo tab alla lista
            TabItemList.Add(tab);

            // Evidenziazione dell'ultimo tab creato
            ServerTabControl.SelectedIndex = TabItemList.Count - 1;

        }

        
        // Funzione associata al tasto del Menù File->Connetti. Apre il menu di connessione.
        private void Menu_Connect_Click(object sender, RoutedEventArgs e)
        {
            NewConnection C = new NewConnection();
            C.Show();
        }
        
        // Funzione associata al tasto del Menù File->Disconnetti
        private void Menu_Disconnect_Click(object sender, RoutedEventArgs e)
        {
            /*non ho un riferimento diretto alla MyTabItem quindi, dall'oggetto grafico ServerTabControl
            /mi faccio dare l'attuale tab selezionata e da questa prendo l'oggetto MyTabItem da passare a CloseTab */
            MyTabManagement s = ServerTabControl.SelectedContent as MyTabManagement;

            if (s != null)
                CloseTab(s.MyTab);
        }
    }
}
