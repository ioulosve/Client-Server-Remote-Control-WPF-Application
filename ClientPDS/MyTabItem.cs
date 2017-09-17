using System.Windows.Controls;

namespace Client
{
    
    //Classe che estende la classe TabItem di WPF.
    //TabItem ha le proprietà Content e Header che noi associeremo a TabManager e TabServerIP in NewTab 
    public class MyTabItem : TabItem
    {
      
        //Riferimento alla MainWindow
        public MainWindow MainWndw { get; private set; }
        
             // Costruttore della classe MyTabItem
        public MyTabItem (MainWindow w) { MainWndw = w; }
 

        // Proprietà che incapsula la stringa che indica l'host remoto
        public string TabServerIP { get; set; }

      
        // Proprietà che incapsula la stringa che indica il nome dell'app in foreground (focus)
        public string AppInFocus { get; set; }

   
        // Proprietà che incapsula un riferimento alla Logica del tab
        public MyTabManagement TabManager { get; set; }

       
    }
}
