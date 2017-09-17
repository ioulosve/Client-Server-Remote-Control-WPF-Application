#pragma comment(lib,"Ws2_32.lib")
#include "Update.hpp"

//Costruttore di Update
Update::Update(u_short t, DWORD id) : type(t), pID(id) {}

//setter di ApplicationNames
void Update::setApplicationNames(ApplicationNames a)
{
	app = a;
}

//ritorna true se l'update è di tipo add
bool Update::isAdd()
{
	return type == add;
}
