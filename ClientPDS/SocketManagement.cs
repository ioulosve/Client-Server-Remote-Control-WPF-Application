using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Client
{

    // Classe che incapsula lo stream di lettura dei dati inviati dal server
    public class SocketManagement
    {
        // Importazione delle librerie Windows utili ad interpretare l'icona inviata dal server
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        extern static bool DestroyIcon(IntPtr handle);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        extern static IntPtr CreateIconFromResourceEx(IntPtr buffer, uint size, int isIcon, uint dwVer, int cx, int cy, uint flags);

        private volatile bool stop = false;
        private NetworkStream Stream; //un riferimento allo stream creato da NewConnection 
        private MyTabManagement TabManager; //riferimento al TabManager che sta usando questo socket.


        // Costruttore della classe SocketManagement
        public SocketManagement(MyTabManagement s)
        {
            TabManager = s;
            Stream = s.Stream;
        }

       
        // Funzione utilizzata per interrompere il ciclo di lettura del thread sullo stream
        public void Stop()
        {
            stop = true;
        }

     
        // Funzione eseguita da un thread in background: riceve dati dal socket
        public void SocketThreadListen()
        {
            int n = 0;

            try
            {
                Byte[] readBuffer = new Byte[1024];

                while (!stop)
                {
                    Console.WriteLine("In attesa di ricevere dati dal server...");

                    // Ricezione del tipo di modifica effettuata
                    n = Stream.Read(readBuffer, 0, sizeof(ushort));

                    if (!readSuccessful(n, sizeof(ushort)))
                        return;

                    // Conversione del buffer nell'ordine dei byte dell'host (Precedentemente era in ordine di rete)
                    ushort conv_mod = BitConverter.ToUInt16(readBuffer, 0);
                    int ModificationType = IPAddress.NetworkToHostOrder((short)conv_mod);
                    Console.WriteLine("Tipo della modifica: {0}", ModificationType);

                    // Ricezione del PID del processo. E' una DWORD che ha dimensioni pari ad uint
                    n = Stream.Read(readBuffer, 0, sizeof(uint));

                    if (!readSuccessful(n, sizeof(uint)))
                        return;

                    uint PID = BitConverter.ToUInt32(readBuffer, 0);

                    Console.WriteLine("PID: {0}", PID);

                    // Switch sul tipo di modifica
                    switch (ModificationType)
                    {
                        // CASO 0: Aggiunta di una nuova applicazione
                        case 0:

                            // Lettura della lunghezza del nome dell'applicazione
                            n = Stream.Read(readBuffer, 0, sizeof(int));

                            if (!readSuccessful(n, sizeof(uint)))
                                return;

                            // Conversione della lunghezza del nome in ordine dell'host
                            int conv_length = BitConverter.ToInt32(readBuffer, 0);
                            Console.WriteLine("Lunghezza convertita: {0}", conv_length);
                            int NameLength = IPAddress.NetworkToHostOrder(conv_length);
                            Console.WriteLine("Lunghezza nome: {0}", NameLength);

                            Byte[] NameBuffer = new Byte[NameLength];

                            String AppName = String.Empty;

                            // Lettura del nome dell'applicazione
                            n = Stream.Read(NameBuffer, 0, NameLength);

                            if (!readSuccessful(n, NameLength))
                                return;

                            try
                            {
                                // Conversione in stringa
                                AppName = System.Text.UnicodeEncoding.Unicode.GetString(NameBuffer);
                                AppName = AppName.Replace("\0", String.Empty);
                            }
                            catch (ArgumentException)
                            {
                                AppName = "Nessun nome";
                            }

                            Console.WriteLine("Nome dell'applicazione: {0}", AppName);

                            // Lettura della lunghezza dell'icona

                            n = Stream.Read(readBuffer, 0, sizeof(int));

                            if (!readSuccessful(n, sizeof(uint)))
                                return;

                            AppItem app = new AppItem(TabManager.MyTab.MainWndw.DefaultIcon);
                            app.PID = PID;
                            app.Name = AppName;

                            int conv_icon = BitConverter.ToInt32(readBuffer, 0);
                            int IconLength = IPAddress.HostToNetworkOrder(conv_icon);
                            Console.WriteLine("Lunghezza dell'icona: {0}", IconLength);

                            // Se la dimensione è valida la si sostituisce a quella di default
                            if (IconLength != 0 && IconLength < 1048576)
                            {
                                Console.WriteLine("Icona valida trovata");

                                // Lettura dell'icona dallo stream in blocchi da 1024 byte
                                Byte[] BufferIcon = new Byte[IconLength];

                                int TotalRead = 0;
                                int ToRead = 1024;

                                while (TotalRead != IconLength)
                                {
                                    if (ToRead > IconLength - TotalRead)
                                        ToRead = IconLength - TotalRead;

                                    n = Stream.Read(BufferIcon, TotalRead, ToRead);

                                    if (n == 0)
                                    {
                                        Console.WriteLine("Connessione persa durante la lettura dell'icona");
                                        return;
                                    }

                                    TotalRead += n;

                                }

                                if (!readSuccessful(TotalRead, IconLength))
                                    return;

                                //in C# è necessario creare un blocco unsafe per usare i puntatori nativi C e l'aritmetica dei puntatori
                                unsafe
                                {
                                    //fixed consente di assegnare ad un puntatore C l'indirizzo di una variabile gestita.
                                    //poiché le variabili gestite possono essere spostate dal garbage collector, questo 
                                    //impedisce che ciò avvenga.
                                    fixed (byte* buffer = &BufferIcon[0])
                                    {
                                        IntPtr Hicon = CreateIconFromResourceEx((IntPtr)buffer, (uint)IconLength, 1, 0x00030000, 48, 48, 0);

                                        if (Hicon != null)
                                        {
                                            BitmapFrame bitmap = BitmapFrame.Create(Imaging.CreateBitmapSourceFromHIcon(Hicon, new Int32Rect(0, 0, 48, 48), BitmapSizeOptions.FromEmptyOptions()));
                                            if (bitmap.CanFreeze)
                                            {
                                                bitmap.Freeze();
                                                app.Icon = bitmap;
                                            }

                                            DestroyIcon(Hicon);
                                        }
                                    }
                                }
                            }


                            // Aggiunta di una nuova applicazione e notifica del cambiamento nella lista
                            TabManager.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                           {
                               lock (TabManager.Applications)
                               {
                                   //aggiungo l'app alla ObsCollection Applications<AppItem> collegata alla ListView di TabManager
                                   TabManager.Applications.Add(app);
                               }
                           }));

                            break;

                        // Caso 1: rimozione di un'applicazione
                        case 1:
                            Console.WriteLine("Modifica: Rimozione");

                            // Rimozione dell'applicazione dalla lista
                            Monitor.Enter(TabManager.Applications);
                            foreach (AppItem appItem in TabManager.Applications)
                            {
                                if (appItem.PID == PID)
                                {
                                    Console.WriteLine("Rimozione applicazione: {0}", appItem.Name);
                                    Monitor.Exit(TabManager.Applications);
                                    this.TabManager.Dispatcher.Invoke(DispatcherPriority.Send,
                                        new Action(() => { lock (TabManager.Applications) { this.TabManager.Applications.Remove(appItem); } }));
                                    Monitor.Enter(TabManager.Applications);
                                    break;
                                }
                            }
                            Monitor.Exit(TabManager.Applications);
                            break;

                        // Caso 3: cambio di focus
                        case 2:
                            Console.WriteLine("Modifica: Change Focus");

                            // Pulizia della selezione precedente
                            this.TabManager.MyTab.MainWndw.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => { this.TabManager.Applist.SelectedItem = null; }));
                           
                            // Applicazione che perde il focus
                            this.TabManager.MyTab.MainWndw.Dispatcher.Invoke(DispatcherPriority.Send,
                                        new Action(() =>
                                        {
                                            // Aggiornamento lista app in foreground
                                            int index = this.TabManager.MyTab.MainWndw.AppsInFocus.IndexOf(new AppInFocus(TabManager.MyTab.AppInFocus));
                                            if (index != -1)
                                            {
                                                //if (--this.TabManager.TabManager.MainWndw.AppsInFocus[index].Count <= 0)
                                                    this.TabManager.MyTab.MainWndw.AppsInFocus.RemoveAt(index);
                                            }
                                        }));

                            // Ricerca delle applicazioni coinvolte nel cambiamento
                            Monitor.Enter(TabManager.Applications);
                            foreach (AppItem appItem in TabManager.Applications)
                            {
                                // Applicazione che guadagna il focus
                                if (appItem.PID == PID)
                                {
                                    Console.WriteLine("Pid: {0} - applicazione: {1}", PID, appItem.Name);
                                    Monitor.Exit(TabManager.Applications);
                                    this.TabManager.MyTab.MainWndw.Dispatcher.Invoke(DispatcherPriority.Send,
                                        new Action(() =>
                                        {
                                            lock (TabManager.Applications)
                                            {
                                                // Evidenziazione elemento nella tab
                                                appItem.HasFocus = true;
                                                this.TabManager.Applist.SelectedItem = appItem;
                                                this.TabManager.MyTab.AppInFocus = appItem.Name;
                                                // Aggiornamento lista delle app in foreground
                                                AppInFocus newapp = new AppInFocus(appItem.Name);
                                                this.TabManager.MyTab.MainWndw.AppsInFocus.Add(newapp);                                                 
                                                this.TabManager.MyTab.MainWndw.AppsInFocusBox.SelectedItem = newapp;
                                              
                                            }
                                        }));
                                    Monitor.Enter(TabManager.Applications);
                                }
                                else if (appItem.HasFocus)
                                    appItem.HasFocus = false;
                            }
                            Monitor.Exit(TabManager.Applications);
                            // Aggiornamento delle percentuali
                            TabManager.Dispatcher.Invoke(DispatcherPriority.Send,
                                             new Action(() => { TabManager.PercentageRefresh(); }));
                            break;

                        case 3:
                            break;
                        default:
                            Console.WriteLine("Modifica sconosciuta");
                            break;
                    }
                }
                Console.WriteLine("Thread - terminata ricezione dati dal server");
            }
            catch (NullReferenceException)
            {
                ExceptionHandler.ReceiveConnectionError(TabManager);
            }
            catch (IOException)
            { 
                ExceptionHandler.ReceiveConnectionError(TabManager);
            }
            catch (ObjectDisposedException)
            {
                ExceptionHandler.ReceiveConnectionError(TabManager);
            }
            catch (ArgumentOutOfRangeException)
            {
                ExceptionHandler.ReceiveConnectionError(TabManager);
            }
            catch (OutOfMemoryException)
            {
                ExceptionHandler.MemoryError(TabManager.MyTab.MainWndw);
            }
        }

        // Metodo per verificare la corretta lettura dal server.
        private bool readSuccessful(int byteread, int bytetoread)
        {
            if (byteread == 0)
            {
                Console.WriteLine("Connessione interrotta durante la lettura");
                return false;
            }

            else if (byteread != bytetoread)
            {
                Console.WriteLine("La lettura non ha avuto successo");
                MessageBox.Show("Server" + TabManager.MyTab.Header as String + ": Connessione interrotta");
                TabManager.MyTab.MainWndw.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
               {
                   TabManager.MyTab.MainWndw.CloseTab(TabManager.MyTab);
               }));

                return false;
            }
            return true;
        }
    }// Class closing bracket

}// Namespace closing bracket
