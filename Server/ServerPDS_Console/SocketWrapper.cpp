#pragma once
#define MAXLENGTH 2048
#include "SocketWrapper.hpp"
#include <iostream>

/* Costruttore della classe che si occupa:
*  1. Inizializzare la libreria Winsock
*  2. definire il Socket del Server (tipo di indirizzi, tipo di protocollo)
*  3. binding del socket con la struttura dati che contiene indirizzi e porta (per poter permettere al S.O. di poter inoltrare correttamente al Server i messaggi) 
*  4. setting del socket in modalità di ascolto per attendere eventuali connessioni.
*/

SocketWrapper::SocketWrapper(int port) {

	/* Impostazione dei socket come non validi per default */
	serverSocket = INVALID_SOCKET;
	clientSocket = INVALID_SOCKET;

	/* Inizializzazione libreria Winsock con metodo WSAStartup, con parametri:
	*  - wVersionRequested: Una WORD che indica la versione che bisogna inizializzare. Il BYTE di minor importanza indica il numero 
	*  - di versione maggiore mentre il BYTE più significativo indica il numero di versione minore. Usiamo Winsock 2.2.
	*  - lpWSAData: Un puntatore alla struttura WSADATA che riceve i dettagli della libreria Winsock.
	*/

	iResult = WSAStartup(MAKEWORD(2, 2), &wsaData);
	if (iResult != 0)
		throw socket_exception("Inizializzazione librerie Winsock fallita!");

	/* Per inviare e ricevere dati abbiamo bisogno di creare un socket. Dopo che il S.O. ne ha creato uno per noi ci ritorna un intero che 
	*  lo identifica. Per contenere l'intero viene utilizzato il tipo di dato SOCKET. Per farlo dobbiamo chiamare la funzione di nome 
	*  socket definita con i seguenti parametri:
	*  - __in  int af: indica il tipo di indirizzi che utilizza (con AF_INET si intende indirizzi IPv4).
	*  - __in  int type: il tipo di protocollo di trasporto da utilizzare (con SOCK_STREAM si specifica di voler usare protocolli che simulano il flusso dati di TCP).
	*  - __in  int protocol: indica strettamente il tipo di protocollo da usare (se TCP o UDP).
	*/

	serverSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if (serverSocket == INVALID_SOCKET)
		throw socket_exception("Costruzione del socket fallita!");

	/* Preparazione della struttura per fare il binding:
	*  L'applicazione server deve rimanere in ascolto per nuove connessioni da un client, per far questo, bisogna dire al S.O. quali sono 
	*  i pacchetti destinati a noi (applicazione server). Quindi dobbiamo settare una porta per la nostra applicazione, per far in modo che 
	*  i messaggi dei client che specificano la nostra porta vengono inoltrati a noi. Questo lavoro è fatto dalla funzione di Bind.
	*/

	ZeroMemory(&sockAddr, sizeof(sockAddr));		// Riempie di "zeri" una certa struttura dati passata come parametro, in questo caso sockAddr
	
	sockAddr.sin_family = AF_INET;					// Tipologia di famiglia che indirizza.
	sockAddr.sin_port = htons(port);				// Numero della porta scelta dal server (e che i client dovranno specificare per parlare con esso)
	sockAddr.sin_addr.s_addr = htonl(INADDR_ANY);	// Indirizzo locale (con INADDR_ANY non è necessario specificarne uno). Utile quando ci sono più interfacce su server ect..

	/* La funzione bind prende come parametri:
	*  - __in  SOCKET s: Il socket da bindare.
	*  - __in  const struct sockaddr *name: il puntatore a una struttura sockaddr che contiene le informazioni sulla porta e l'indirizzo locale.
	*  - __in  int namelen: La lunghezza in byte della struttura sockaddr
	*/
	if (bind(serverSocket, (struct sockaddr*) &sockAddr, sizeof(sockAddr)) != 0) {
		closesocket(serverSocket);
		throw socket_exception("Ascolto fallito da parte del Server");
	}

	/* Dopo aver effettuato il binding tra il socket e la struttura sockAddr che memorizza la porta e le altre informazioni, bisogna mettere
	* il socket in posizione d'ascolto con la funzione Listen che specifica:
	* il socket da mettere in ascolto.
	* lunghezza della coda di connessioni che possono essere messe in attesa (impostiamo a 0 perché non vogliamo una coda).
	*/

	if (listen(serverSocket, PENDINGQUEUE) == SOCKET_ERROR) {
		closesocket(serverSocket);
		throw socket_exception("Ascolto fallito");
	}

}

/*	Dopo che il Socket è stato inizializzato ed impostato in ascolto, è possibile instaurare una connessione con il client. Per poter iniziare
*	a gestire una connessione con un Client bisogna far utilizzo della funzione Accept che è integrata dentro il seguente metodo, che si occupa
*   della procedura d'instaurazione della comunicazione con un Client (se esso non c'è, si mette in attesa).
*/

char* SocketWrapper::waitingForConnection() {

	clientSocket = accept(serverSocket, (struct sockaddr*)&clientSockAddr, &clientAddrLen);

	
	if (clientSocket == INVALID_SOCKET) {
		closesocket(serverSocket);
		throw socket_exception("Accettazione del Client fallita!");
	}

	/* A questo punto la connessione è stata accettata dal ListenSocket, 
	   pertanto possiamo affermare che il client è connesso
	*  Impostiamo lo stato del socket come connesso, per indicare 
	   che c'è una connessione attiva su di esso
	*/

	setStatus(true);

	return inet_ntoa(clientSockAddr.sin_addr);
}

/* Funzione che imposta lo stato della connessione (se il socket del server è connesso o meno ad un client) */
void SocketWrapper::setStatus(bool status) {
	isConnected.store(status);
}

/* Funzione che restituisce lo stato del socket (Connesso, non connesso) */
bool SocketWrapper::getStatus(){
	return isConnected.load();
}

/* Funzione che chiude la connessione del socket (Non permette altre comunicazioni con quel client) */
void SocketWrapper::closeConnection() {
	if ((closesocket(clientSocket)) == SOCKET_ERROR) {
		throw socket_exception("Socket close failed");		//Se la chiusura del socket fallisce si lancia un'eccezione
	}
}

/* Dopo che si è instaurata una connessione tra Server e il Client (quindi dopo l'invocazione del costruttore e del metodo waitingForConnection() 
*  Il Server è in grado di inviare e ricevere i dati dal Client tramite le funzioni send e recv implementate nei seguenti metodi.
*/

void SocketWrapper::sendData(char* buffer, int len) {
	int nOfLeft = len;					// nOfLeft = numero di dati rimasti da scambiare (inizialmente pari alla lunghezza totale del buffer)


	while (nOfLeft > 0) {				// finché c'è qualche dato da scambiare

		// si verifica che la dimensione (lunghezza) dei dati non sia superiore all'upper bound. In quel caso si "spezza" l'invio in più invii.
		if (nOfLeft <= MAXLENGTH)
			/*	Il metodo send invia i dati al socket connesso (specificato). I parametri sono:
			*	- Socket che è ritornato dalla accept (che è quello connesso al socket del Server), a cui vanno inviati i dati. 
			*	- Puntatore al buffer da inviare.
			*	- Numero di bytes da inviare.
			*	- Specifica di modalità con cui devono essere inviati i dati.
			*/
			iResult = send(clientSocket, buffer, nOfLeft, 0);
		else
			iResult = send(clientSocket, buffer, MAXLENGTH, 0);

		if (iResult == SOCKET_ERROR)
			throw socket_exception("Invio fallito");

		/*	Se è andata a buon fine, la send ritorna il numero di byte inviati, pertanto bisogna aggiornare il numero di byte ancora da inviare, 
		*	rimuovendo quelli appena inviati. Bisogna anche spostare il puntatore al buffer, per fare in modo che la prossima volta invii nuovi
		*	dati, non quelli già inviati.
		*/
		nOfLeft -= iResult;
		buffer += iResult;
	}
}

int SocketWrapper::receiveData(char* buffer, int len) {

	/*	 In maniera analoga recv è la funzione che permette di ricevere dati da un socket connesso.
	*	 Parametri:
	*	 - ClientSocket: Socket tornato dall'accept (quello connesso al socket del Server)
	*    - buffer: Un puntatore al buffer che deve ricevere i dati in arrivo
	*    - len: Lunghezza del buffer di ricezione
	*    - 0: Flag
	*/
	int bytesToReceive = len;
	while (bytesToReceive > 0) {
		iResult = recv(clientSocket, buffer, bytesToReceive, 0);
		if (iResult == SOCKET_ERROR)
			throw socket_exception("Recv failed");
		// Se la receive avrà avuto successo, essa restituirà il numero di byte ricevuti, e lo stesso farà receivedata
		bytesToReceive -= iResult;
		buffer += iResult;
	}
	
	return iResult;
}
