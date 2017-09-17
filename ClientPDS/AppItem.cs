using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Client
{
    
    // Classe che incapsula le informazioni relative alle singole applicazioni che compaiono nella lista.
    public class AppItem : INotifyPropertyChanged
    {
        
        // Numero che mantiene la percentuale di focus avuto dall'applicazione corrente
        private int _percentage = 0;

        // Proprietà che incapsula il nome dell'applicazione
        public String Name { get; set; }
      
        // Proprietà che incapsula l'icona dell'applicazione
        public ImageSource Icon { get; set; }

        // Proprietà che incapsula il PID dell'applicazione
        public uint PID { get; set; } = 0;

        // Proprietà che incapsula il tempo di esecuzione dell'applicazione
        public TimeSpan ExecutionTime { get; set; } = new TimeSpan(0);


        // Proprietà che incapsula la percentuale di focus dell'applicazione e ne notifica eventuali variazioni all'interfaccia
        public int Percentage
        {
            get { return _percentage; }
            set
            {
                if (value != _percentage)
                {
                    _percentage = value;
                    NotifyPropertyUpdate();
                }
            }
        }

        // Indica se l'applicazione ha il focus o meno
        private bool _hasFocus = false;
        
        // Proprietà che incapsula lo stato dell'applicazione
        public bool HasFocus
        {
            get { return _hasFocus; }
            set
            {
                if (value != _hasFocus)
                {
                    _hasFocus = value;
                }
            }
        }
  
        // Delegato che traccia l'aggiornamento di una proprietà
        public event PropertyChangedEventHandler PropertyChanged;

        // Costruttore della classe AppItem
        public AppItem(ImageSource defaultIcon)
        {
            Name = "Applicazione di default";
            Icon = defaultIcon;
        }

        // Metodo invocato all'atto della variazione della percentuale
        private void NotifyPropertyUpdate ([CallerMemberName] String property = "")
        {
            // Se c'è stato un aggiornamento di una proprietà
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
    }
}
