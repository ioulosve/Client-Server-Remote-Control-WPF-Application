#include "ListManager.hpp"
#include "Utilities.h"
#define MAXSTR 100
#define MAXEXT 10
#define pair std::pair<DWORD, ApplicationNames>


/*
Funzione che viene richiamata per ogni finestra rilevata da EnumWindow() (vedi dopo)
Se la finestra non è visibile o se il suo nome è vuoto, ritorna subito;
altrimenti crea una struttura ApplicationNames con i parametri della finestra
e la inserisce nella lista passata come parametro. Ciò viene fatto per ogni finestra (applicazione) aperta
*/

BOOL CALLBACK MyWindowProc(__in HWND hwnd, __in LPARAM lparam) {
	
	/* Finestra non visibile */
	if (!IsWindowVisible(hwnd)) {
		return TRUE;
	}

	/* Ottenimento del pid del processo e verifica che esso sia stato già inserito nella lista delle applicazioni */

	DWORD procID;
	GetWindowThreadProcessId(hwnd, &procID);		// ottenimento del pid

	// se find() = end() significa che la pair con key "proc" (il pid) non è presente nella lista
	if (((std::map<DWORD, ApplicationNames>*) lparam)->find(procID) != ((std::map<DWORD, ApplicationNames>*)lparam)->end())
		return TRUE;

	/* Arrivati qui significa che il processo non è presente nella lista 
	* Si vuole ottenere l'handle del processo tramite il pID ottenuto, ottenendo i giusti permessi per poter ottenere le informazioni sul nome
	*/
	
	HANDLE process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, procID);
	if (process == NULL)
		return TRUE;
	
	TCHAR* file_name = new TCHAR[MAXSTR];
	DWORD maxstr = MAXSTR;
	/* la funzione QueryFullProcessImageName prende l'handle del process, e estrae il path del processo, salvandolo in file_name, riuscendoci grazie ai "diritti" definiti con OpenProcess */

	if (QueryFullProcessImageName(process, 0, file_name, &maxstr) == 0 || maxstr >= MAXSTR) {
		CloseHandle(process);
		delete[] file_name;
		return TRUE;
	}

	/* Dopo essere riusciti ad estrarre il path dell'applicazione, posso aggiungerla alla lista */
	ApplicationNames app;

	TCHAR* buff = new TCHAR[MAXSTR + 1];
	TCHAR* ext = new TCHAR[MAXEXT + 1];

	/* Prendiamo dal nome completo del file il nome dell'eseguibile
	*  gli altri due parametri sono a NULL e 0 perché non servono quelle informazioni
	*  vedendo la documentazione, in buff ci finisce il nome del file senza estensione, in ext ci finisce l'estensione
	*  se torna 0 ha avuto successo
	*/

	if (splitpath(file_name, NULL, 0, NULL, 0, buff, MAXSTR, ext, MAXEXT) != 0) {
		delete[] buff;
		delete[] ext;
		delete[] file_name;
		CloseHandle(process);
		return TRUE;
	}

	/* il campo Name dell'appplicazione lo costruisco come nome file + estensione */
	app.Name = buff;
	app.Name += ext;
	app.Exec_name = file_name;

	((std::map<DWORD, ApplicationNames>*)lparam)->insert(pair(procID, app));
	delete[] buff; delete[] ext; delete[] file_name;
	CloseHandle(process);

	return TRUE;
}

/*
Creazione della lista da zero, semplicemente richiamando la funzione EnumWindows()
Alla funzione viene passata la callback e la lista
*/

void ListManager::buildList(std::map<DWORD,ApplicationNames>& tempList) {

	tempList.clear();

	/* Per ogni applicazione in foreground eseguiamo la MyWindowsProc passando la lista delle app
	*  Enumera tutte le top-level windows sullo schermo passando l'handle ad ogni window, a turno, ad una application-defined callback function.
	*  EnumWindows continua finché l'ultima top-level window non viene enumerata o se la callback function ritorna FALSE (per questo restituisce true la func mywind).
	*/
	if (!EnumWindows(MyWindowProc, (LPARAM)&tempList))
		throw std::runtime_error("Fallimento nella enumerazione delle Windows");
}

/*
* Funzione principale della classe ListManager, eseguita dal thread che gestisce la lista.
* Fino a che il programma non viene terminato, viene richiesta una nuova lista di applicazioni ogni refreshTime millisecondi; 
* questa lista viene confrontata con quella del ListManager per determinare i programmi nuovi e quelli terminati, per
* poi sostituire la vecchia lista. I dati poi devono essere inviati al client.
*/

void ListManager::UpdateAppList() {
	
	std::map<DWORD, ApplicationNames> newList;
	DWORD newForeground = 0;
	int count = 0;

	/* il ciclo viene interrotto quando il client chiude la connessione */
	
	while (socket.getStatus() == true) {
		count++;

		buildList(newList);	//buildList chiama enumWindow e riempe effetivamente la lista delle applicazioni attive sul server


		//SCORRIAMO NEWLIST PER TROVARE APPLICAZIONI NUOVE
		for each(pair app in newList) {
			std::map<DWORD, ApplicationNames>::iterator i = applicationsList.find(app.first);

			//SE L'APP di newlist  NON ESISTEVA IN APPLIST ...
			if (i == applicationsList.end())
			{               //-> è nuova : creiamo un change di "applicazione aggiunta"
				

				DWORD pid = app.first;
				ApplicationNames names = app.second;
				Update	c(add, pid); //cambiamento di tipo add avente PID=pid
				c.setApplicationNames(names);
				updateList.push_back(c);

				//c'è una nuova app, ed è anche quella che ha il focus
				Update c1(newFocus, pid);
				focusedApplication = pid;
				updateList.push_back(c1);

				count = 0;
			}

		}
		//SCORRIAMO APPLIST PER TROVARE APPLICAZIONI CHIUSE (NON PIù ESISTENTI IN NEWLIST)
		for each(pair app in applicationsList) {
			std::map<DWORD, ApplicationNames>::iterator i = newList.find(app.first);

			//SE L'APP di applicationsList  NON ESISTE PIù in newList
			if (i == newList.end())
			{               //-> è stata chiusa: creiamo un change di rimozione
				DWORD pid = app.first;
				//-> è stata chiusa: creiamo un change di rimozione
				Update c(del, pid);
				updateList.push_back(c);
				count = 0;
			}

		}

		/* Memorizzo la nuova lista */
		applicationsList.swap(newList);

		/* vedo se è cambiata l'applicazione col focus
		*  la funzione GetForegroundWindow() prende (restituisce) l'HANDLE della window in foreground
		*  la funzione getwindowthreadprocessID mi salva dentro newForeground il pid del HANDLE della window in foreground
		*/

		GetWindowThreadProcessId(GetForegroundWindow(), &newForeground);

		//controlla se l'applicazione attualmente in focus è la stessa o è cambiata
		if (newForeground != focusedApplication) {	// focusedApplication: PID della window in foreground(see Application.hpp)
			focusedApplication = newForeground;
			//nel caso in cui l'app in focus è cambiata, segnati il cambiamento
			Update c(newFocus, focusedApplication);
			updateList.push_back(c);
			count = 0;
		}

		//se per 10 cicli consecutivi non avviene nessun cambiamento, invia che non c'è stato un cambiamento alla lista.
		
		
	if (count == 10) {
			count = 0;
			Update c(keepAlive, 0);
			updateList.push_back(c);
		}
		

	 sendToClient();

		/* il thread è messo in pausa per tot millisecondi */
		std::this_thread::sleep_for(std::chrono::microseconds(refreshTime));
	}

}

/* Invio della lista al client */

void ListManager::sendToClient() {
	
	if (updateList.empty()) {
		return;
	}
	

	char* send_buf = nullptr;
	int length = 0;
	bool noIcon = false;
	int nOfChange = updateList.size();

	try {
		for each(Update c in updateList) {

			/*per tutti i cambiamenti viene sempre inviato il changeType*/
			send_buf = createUpdateBuffer(length, c);	  //see Update.cpp
			socket.sendData(send_buf, length);
			length = 0;
			free(send_buf);
			

			/* Modifica ADD: solo se è un Modification di tipo add il createNameBuffer restituisce un valore diverso da nullptr 
			* La modifica di tipo add prevede molto più lavoro, in quanto bisogna inviare il nome dell'applicazione e l'icona
			*/
			send_buf = createNameBuffer(length, c);
			if (c.isAdd()) {
				u_long length_net = htonl(u_long(length));				// see SocketWrapper.cpp (traduzione in formato per la network)
				socket.sendData(((char*) &length_net), sizeof(int));		// invio dimensione (lunghezza) del nome dell'applicazione aggiunta
				socket.sendData(send_buf,length);						// invio del nome dell'applicazione
				length = 0;
				free(send_buf);

				/* invio dell'icona */
				send_buf = createIconBuffer(length, c);
				if (send_buf != nullptr) {
					u_long length_net = htonl(u_long(length));			// see SocketWrapper.cpp (traduzione in formato per la network)
					socket.sendData(((char*) &length_net), sizeof(int));	// invio dimensione dell'icona dell'applicazione aggiunta
					socket.sendData(send_buf, length);					// invio dell'icona
					length = 0;
					free(send_buf);
				}
				else {													// Non è stato allocato nessun buffer => no free in caso di eccezione in sendData.
					length = 0;
					noIcon = true;
					socket.sendData(((char*)&length), sizeof(int));
				}
			}

		}

		/* al termine dell'invio cancello la lista */
		updateList.clear();
	}
	catch (std::overflow_error& e) {
		std::wcerr << e.what() << std::endl;
		// la memcpy_s fallisce dentro createNameBuffer
		// forziamo la chiusura della connessione
		socket.closeConnection();
		updateList.clear();			// la lista è disponibile per altre connessioni
		socket.setStatus(false);	// termina il metodo UpdateAppList
		int a;
		
	}
	
	catch (socket_exception) {

	}
	catch (std::exception& e) {
		std::wcerr << e.what() << std::endl;

		/* si rilasciano eventuali risorse quando la send fallisce */
		if (!noIcon)
			free(send_buf);

		updateList.clear();			// lista disponibile per altre connessioni

		socket.setStatus(false);	// durante l'invio c'è stato un errore, il ciclo dentro UpdateAppList termina.	
	}
}

/* metodo gestito da un thread secondario (sganciato dal ThreadManager nella funzione ServerManagement)
*  si occupa di attendere i comandi del client, li decifra, e li invia all'applicazione in foreground come input
*/



