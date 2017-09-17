using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace Client
{

    /*Il client invia al server un buffer che contiene "lo stato" dei tre modificatori e il tasto premuto*/
    
    struct modificatore { 
        public char ctrl;
        public char alt;
        public char shift;
    }

  
    // Definizione della componente della classe MainWindow in grado di gestire la pressione dei tasti
    public partial class MainWindow : Window
    {
        
        modificatore mod = new modificatore();

        //Inizializza la gestione del controllo remoto
        private void SendCommands_Start()
        {

            //modifier = mod_code.none;
            mod.alt = mod.ctrl = mod.shift = 'n';

            //aggiunta dei metodi che dovrà eseguire la mainWindow quando viene premuto e rilasciato un tasto
            this.PreviewKeyDown += KeyPressed;
            this.PreviewKeyUp += KeyReleased;
        }


        // Funzione invocata dall'evento PreviewKeyDown (registrato da SendCommands_Start)
        // Cattura e registra i modificatori e il tasto premuto e li invia al server
        private void KeyPressed (object sender, KeyEventArgs e)
        {

            //estrae l'attuale app in focus dalla AppsInFocusBox
            AppInFocus appinfocus = AppsInFocusBox.SelectedItem as AppInFocus;
            
            //Controlla se l'app in focus è il client stesso (per evitare cicli infiniti)
            if (appinfocus.Name == System.AppDomain.CurrentDomain.FriendlyName || appinfocus == null)
            {
                e.Handled = true;
                return;
            }
           
            Key key;
            if (e.Key == Key.System)
                key = e.SystemKey;
            else
                key = e.Key;

            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    mod.shift = 'y'; 
                    e.Handled = true;
                    break;

                case Key.LeftCtrl:
                case Key.RightCtrl:
                    mod.ctrl = 'y';
                    e.Handled = true;
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    mod.alt = 'y';
                    e.Handled = true;
                    break;

                default:
                    break;
            } // Switch closing bracket
            
            // Nel caso in cui il tasto premuto non sia un modificatore
            if (e.Handled == false)
            {
                // Preparazione dei dati da inviare
               
                byte[] buffer = new byte[3 + sizeof(int)];          // Struttura che conterra  Modificatori + tasto
                buffer[0] = Convert.ToByte(mod.ctrl);
                buffer[1] = Convert.ToByte(mod.alt);
                buffer[2] = Convert.ToByte(mod.shift);

                int conv_key = KeyInterop.VirtualKeyFromKey(key);
                BitConverter.GetBytes(IPAddress.HostToNetworkOrder(conv_key)).CopyTo(buffer, 3);

                // Recupero l'applicazione che è in focus dall'elemento selezionato nella combobox
    
                // Se c'è almeno un app in focus, cerchiamo il tab o server a cui appartiene 
                foreach (MyTabItem tab in TabItemList)
                {
                    if (tab.AppInFocus == appinfocus.Name)
                    {
                        MyTabManagement s = tab.Content as MyTabManagement;
                        if (s != null)
                            try
                            {
                                //invia i tasti sul socket relativo a questa tab (server)
                                s.Stream.BeginWrite(buffer, 0, 3 + sizeof(int), new AsyncCallback(SendToServer), s);
                            }
                            catch (IOException)
                            {
                                ExceptionHandler.SendError(s);
                            }
                    }
                }

                e.Handled = true;
            }

        } // KeyPressed closing bracket

        //Invocato dall'evento PreviewKeyUp (registrato da SendCommands_Start)
        private void KeyReleased (object sender, KeyEventArgs e)
        {
            Key key;

            if (e.Key == Key.System)
                key = e.SystemKey;
            else
                key = e.Key;

            switch (key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    mod.shift = 'n';
                    e.Handled = true;
                    break;

                case Key.LeftCtrl:
                case Key.RightCtrl:
                    mod.ctrl = 'n';
                    e.Handled = true;
                    break;

                case Key.LeftAlt:
                case Key.RightAlt:
                    mod.alt = 'n';
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        } // KeyReleased closing bracket


   
        // Metodo che gestisce la terminazione dell'invio di dati al server (invocato da KeyPressed)
        private void SendToServer(IAsyncResult a)
        {
            MyTabManagement s = (MyTabManagement)a.AsyncState;

            try
            {
                s.Stream.EndWrite(a);
            }
            catch (IOException)
            {
                ExceptionHandler.SendError(s);
            }
        }

       
    }
}
