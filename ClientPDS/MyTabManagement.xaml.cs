using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Client
{
  
    // Classe che contiene la logica per la gestione e la visualizzazione grafica di un tab relativo ad un singolo server
    // Estende la classe UserControl 
    public partial class MyTabManagement : UserControl
    {
        
        // Proprietà che incapsula il tab relativo al server associato al MyTabManagemen
        public MyTabItem MyTab { get; private set; }

        
        // Socket connesso
        private TcpClient _socket;

        
        // Stream (lettura e scrittura)
        private NetworkStream _stream;

        // Struttura che mantiene il timestamp della creazione del TabManager
        private DateTime MyTabBirth;

       
        // Struttura che mantiene il timestamp dell'ultimo aggiornamento della percentuale
        private DateTime LastPercentageUpdate;

        
        // Timer che scandisce ogni quanto tempo bisogna aggiornare la percentuale
        private System.Timers.Timer PercentageRefreshTimer;

       
        // Lista delle applicazioni attive sul server relativo a questo tab
        public ObservableCollection<AppItem> Applications { get; }

      
        // Funzione da assegnare al thread Listener (vedi sotto)
        private SocketManagement Listener;

        //Thread che verrà lanciato e messo in attesa di informazioni dal server
        private Thread ListenerThread;

       
        // Proprietà che incapsula le informazioni relative al socket
        public TcpClient Connection
        {
            get { return _socket; }
            set { _socket = value; }
        }

        // Proprietà che incapsula le informazioni relative allo stream (lettura o scrittura)
        public NetworkStream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }

     
        // Costruttore della classe MyTabManagement
        public MyTabManagement(MyTabItem MTI)
        {
            InitializeComponent();

            MyTab = MTI;

            // Ogni TabManager si iscrive all'evento di chiusura della mainwindow
            MyTab.MainWndw.ClosingEvent += ServerTabClose;

            // Si impostano i timestamp iniziali all'istante corrente
            MyTabBirth = LastPercentageUpdate = DateTime.Now;

            // La percentuale viene aggiornata ogni 1000 ms
            PercentageRefreshTimer = new System.Timers.Timer(1000);

            // Impostiamo il timer come ricorsivo: al termine dei 1000 ms riparte da zero e ricomincia
            PercentageRefreshTimer.AutoReset = true;

            // Allo scadere del timer, si lancia la funzione di aggiornamento della percentuale (PercentageRefresh)
            PercentageRefreshTimer.Elapsed += (obj, e) =>
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { PercentageRefresh(); }));
            };

            // Inizializzazione della lista di applicazioni (inizialmente vuota)
            Applications = new ObservableCollection<AppItem>();

            // Associazione della lista delle applicazioni all'elemento WPF Applist
            Applist.ItemsSource = Applications;

            // Abilitazione dell'accesso alla lista da parte di più thread
            BindingOperations.EnableCollectionSynchronization(Applications, Applications);
        }

      
        // Funzione che inizia la raccolta delle informazioni dal server
        public void StartServerDataExchange()
        {
            uint attempt = 2;

            while (attempt != 0)
            {
                try
                {
                    Listener = new SocketManagement(this);

                    // Thread secondario che si pone in attesa di informazioni dal server
                    ListenerThread = new Thread(Listener.SocketThreadListen);

                    Console.WriteLine("Main thread: Call Start, to start ThreadFcn.");

                    // Mettiamo il thread in background
                    ListenerThread.IsBackground = true;
                    ListenerThread.Start();

                    // Avvio del timer di refresh della percentuale
                    PercentageRefreshTimer.Start();

                    Console.WriteLine("Main thread: Call Join(), to wait until ThreadFcn ends.");

                    attempt = 0;
                }
                catch(OutOfMemoryException)
                {
                    ExceptionHandler.MemoryError(attempt, this.MyTab.MainWndw);
                }

            }


        }

       
        // Ferma l'ascolto dal server e disalloca le risorse precedentemente allocate per la comunicazione.
        public void ServerTabClose()
        {
            // Chiudo il socket in attesa
            Listener.Stop(); //mette il booleano a false per far terminare il while, dopo il while il thread finisce
            ListenerThread.Join(); //aspetta che il thread si ferma
            try
            {
                // Disabilita il socket sia in ingresso che uscita (Both)
                Connection.Client.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
                MessageBox.Show("Errore di connessione.", "Attenzione!", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (ObjectDisposedException)
            {
                // Indica che il socket è già stato chiuso
                MessageBox.Show("Tentativo di chiudere un socket già chiuso");
            }
        }

       
        // Metodo richiamato all'atto della modifica del cambio di focus o allo scadere del timer
        // Aggiorna le percentuali di tempo di focus delle diverse applicazioni
        public void PercentageRefresh()
        {
            // Accediamo in mutua esclusione alla lista delle applicazioni
            lock (Applications)
            {
                TimeSpan lastUpdate = DateTime.Now - LastPercentageUpdate;
                TimeSpan totalExecutionTime = DateTime.Now - MyTabBirth;

                foreach (AppItem a in Applications)
                {
                    if (a.HasFocus)
                        a.ExecutionTime += lastUpdate;
                    try
                    {
                        a.Percentage = (int)(a.ExecutionTime.TotalMilliseconds / totalExecutionTime.TotalMilliseconds * 100);
                    }
                    catch (DivideByZeroException)
                    {
                        a.Percentage = 0;
                    }
                }

                LastPercentageUpdate = DateTime.Now;
            }
        }
    }
}
