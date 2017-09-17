using System;
using System.Windows;
using System.Windows.Threading;

namespace Client
{
    static class ExceptionHandler
    {
        static public void ReceiveConnectionError(MyTabManagement Item)
        {
            MessageBoxResult res = MessageBox.Show("Il Server "+Item.MyTab.TabServerIP+" non risponde, vuoi riprovare?", "Attenzione!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.No) {
               
                Item.MyTab.MainWndw.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, new Action(() =>
                {
                    Item.MyTab.MainWndw.Error = true;
                    Item.MyTab.MainWndw.CloseTab(Item.MyTab);

                }));
            }
            if (res == MessageBoxResult.Yes) {
                Item.MyTab.MainWndw.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send, new Action(() =>
                {
                    //codice di connect
                    NewConnection C = new NewConnection();
                    C.Show();

                    Item.MyTab.MainWndw.Error = true;
                    //codice di disconnect
                    if (Item != null)
                       Item.MyTab.MainWndw.CloseTab(Item.MyTab);

                 
                }));
                
                
            }

            
        }

        static public void MemoryError(MainWindow main)
        {
            MessageBox.Show("Errore di memoria.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            main.Error = true;
            main.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => { Application.Current.Shutdown(); }));
        }

        static public void MemoryError(uint attempt, MainWindow main)
        {
            if (attempt > 1)
            {
                attempt--;
                System.GC.Collect();
            }
            else
            {
                MessageBox.Show("Errore irreversibile di memoria.", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                main.Error = true;
                main.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => { Application.Current.Shutdown(); }));
            }
        }

        static public void SendError(MyTabManagement Item)
        {
            MessageBoxResult res = MessageBox.Show("Errore durante l'invio del comando. Chiudere la connessione?", "Attenzione!", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
                Item.MyTab.MainWndw.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => { Item.MyTab.MainWndw.CloseTab(Item.MyTab); }));
        }

        static public void ConnectionError()
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window is MainWindow)
                {
                    MainWindow w = window as MainWindow;
                    ExceptionHandler.ReceiveConnectionError(w.TabItemList[w.TabItemList.Count - 1].TabManager);
                    break;
                }
            }
        }
    }
}