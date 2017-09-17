#pragma once
#include "SocketWrapper.hpp"
#include "Update.hpp"
#include <Windows.h>
#include <thread>
#include <memory>
#include <mutex>
#include <deque>
#include <vector>
#include <map>
#include <iostream>
#include <psapi.h>
#include <system_error>



/* Classe che gestisce la lista delle applicazioni */

class ListManager {
private:

	unsigned long refreshTime;							//Tempo di refresh della lista
	std::map<DWORD, ApplicationNames> applicationsList;	//Lista delle applicazioni indicizzata per pid
	DWORD focusedApplication = 0;						//Pid dell'applicazione in foreground
	std::deque<Update> updateList;						//Puntatore alla lista delle modifiche
	SocketWrapper& socket;
	void sendToClient();

public:
	void buildList(std::map<DWORD, ApplicationNames>& list);
	void UpdateAppList();
	ListManager(SocketWrapper& s, unsigned long refreshTime = 100) : socket(s), refreshTime(refreshTime) {}
};


#ifdef UNICODE

#define splitpath _wsplitpath_s

#else

#define splitpath _splitpath_s

#endif