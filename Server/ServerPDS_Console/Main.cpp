#include "ListManager.hpp"
#include "utilities.h"
#include <stdlib.h>

#define PORT 2000

using namespace std;

int main() {

	bool continua = true;
	try {
		
		SocketWrapper socket(PORT);

		serverLoop(socket,continua);
		
		if (socket.getStatus() == true) {
			socket.closeConnection();
			socket.setStatus(false);
			continua = false;	
		}

	}
	catch (socket_exception& e) {
		WSACleanup();
		return -1;
	}
	catch (std::system_error) {
		WSACleanup();
		return -1;
	}

	WSACleanup();
	system("PAUSE");
	return 0;

}