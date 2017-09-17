#pragma once
#include "SocketWrapper.hpp"
#include "ListManager.hpp"


//viene eseguito dal MainThread per gestire la connessione col client
void serverLoop(SocketWrapper& socket, bool& continua);

//viene eseguito dal thread secondario per intercettare i comandi da inviare all'applicazione in focus
void fromClientToForegroundApp(SocketWrapper* s);

//inizializza le struct INPUT relative ai tasti modificatori
void inputStructInitialize(INPUT& CtrlDown, INPUT& ShiftDown, INPUT& AltDown, INPUT& CtrlUp, INPUT& ShiftUp, INPUT& AltUp,
							INPUT& KeyDown, INPUT& KeyUp);


char * createUpdateBuffer(int& length, Update c);
char * createNameBuffer(int& length, Update c);
char * createIconBuffer(int& length, Update c);