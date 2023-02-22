import socket
from threading import Thread

class SocketServer:
	PORT = 51399
	DELIMITER = "\n"

	def startServer(self, callback):
		# Create the socket and start the server in a thread
		self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		self.__thread = Thread(target=lambda: self.__startServer(callback))
		self.__thread.daemon = True
		self.__thread.start()
		
	def __startServer(self, callback):
		# Start the server and wait for the client to connect
		self.socket.bind(("", self.PORT))
		self.socket.listen(1)
		self.client, address = self.socket.accept()
		callback()
		
	def startReceive(self, callback):
		# Start receiving messages in a thread
		self.callback = callback
		self.receiving = True
		self.__thread = Thread(target=self.__receive)
		self.__thread.daemon = True
		self.__thread.start()
		
	def __receive(self):
		# Receive messages and return them in the callback
		remaining = ""
		while self.receiving:
			# Receive parts of the message until it is complete
			message = remaining
			while not self.DELIMITER in message:
				messageBytes = self.client.recv(1024)
				if(messageBytes):
					message += messageBytes.decode()
				else:
					# Client disconnected
					self.receiving = False
					self.stopServer()
			message = message.split(self.DELIMITER)
			remaining = message[1]
			if self.receiving: self.callback(message[0])
				
	def stopReceive(self):
		# Stop receiving messages
		self.receiving = False
			
	def send(self, message):
		# Send a message to the client
		self.client.send((message + self.DELIMITER).encode())

	def sendBroadcast(self, message):
		# Send a broadcast message to the network
		sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
		sock.sendto(message.encode(), ('<broadcast>', self.PORT))
		sock.close()
		
	def stopServer(self):
		# Stop the server
		self.client.close()
		self.socket.close()



if __name__ == "__main__": # Test: Simple chat
	def main():
		while wait: pass
		print("Connected!")
		server.startReceive(lambda message: print(message))
		running = True
		while running:
			message = input()
			if message == "exit":
				running = False
				server.stopReceive()
				server.stopServer()
			else:
				try:
					server.send(message)
				except Exception as e:
					print("Client disconnected")
					running = False

	def startMain():
		global wait
		wait = False

	wait = True
	server = SocketServer()
	print("Sending broadcast...")
	server.sendBroadcast("IP")
	print("Waiting for client...")
	server.startServer(startMain)
	main()
