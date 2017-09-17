#pragma once
#include <string>
#include <exception>
#include <Windows.h>

#define typeLen sizeof(u_short)
#define pidLen sizeof(DWORD)
#define add 0
#define del 1
#define newFocus 2
#define keepAlive 3

/* Struct che contiene le inforamzioni su un'applicazione */
struct ApplicationNames {
	std::wstring Name;		//Nome dell'applicazione
	std::wstring Exec_name;
};
	/* la classe che rappresenta una modifica alla lista */
	class Update {
	
	public:
		u_short type;
		DWORD pID;
		ApplicationNames app;

		Update(u_short t, DWORD id);         // Costruttore di modifica change_focus o remove
		void Update::setApplicationNames(ApplicationNames a);	// Costruttore modifica add
		bool Update::isAdd();
	};