#pragma once
#include <winsock2.h>
#include <ws2tcpip.h>
#include <stdexcept>
#include <atomic>


#define PENDINGQUEUE 0

/* Classe che contiene tutte le informazioni necessarie per permettere la comunicazione client server tramite socket */

class SocketWrapper {
	WSADATA wsaData;						// per poter usare le Winsock bisogna inizializzare la libreria
	SOCKET serverSocket;					// socket (oggetto che rappresenta una connessione) in attesa di comandi
	SOCKET clientSocket;					// socket per la comunicazione con il client
	struct sockaddr_in	sockAddr, clientSockAddr;	// struttura dati che contiene informazioni sulla famiglia di indirizzi (se Ipv4 o Ipv6), indirizzo IP locale e Porta 
	socklen_t clientAddrLen = sizeof(clientSockAddr);
	std::atomic_int iResult;
	std::atomic_bool isConnected = false;	// stato del socket

public:
	SocketWrapper(int port);
	char* waitingForConnection(); //ritorna l'ip del client a cui si è connesso.
	bool getStatus();
	void setStatus(bool status);
	void closeConnection();
	void sendData(char* buffer, int len);
	int receiveData(char* buffer, int len);
};


class socket_exception : public std::runtime_error {
public:
	socket_exception(const char* message) : runtime_error(message) {};
};