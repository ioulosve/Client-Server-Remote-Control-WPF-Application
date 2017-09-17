using System;


namespace Client
{
   
    // Classe che mantiene le informazioni sulle app in foreground (focus)
    public class AppInFocus
    { 
        // Nome dell'applicazione
        public String Name { get; set; }

        // Costruttore della classe AppInFocus
        public AppInFocus (string name)
        {
            Name = name;
            
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            AppInFocus p = obj as AppInFocus;

            if ((object)p == null)
                return false;

            if (Name == p.Name)
                return true;

            //Caso di default
            return false;
        }
    }
}
