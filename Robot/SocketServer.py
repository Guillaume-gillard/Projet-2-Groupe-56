# Modules pour la communication avec la télécommande
import socket
from threading import Thread
import traceback

class SocketServer:
	"""Gère la communication en réseau local avec un client (= la télécommande)"""

	PORT = 51399  # Port utilisé pour la communication


	def __init__(self, errorCallback):
		self.errorCallback = errorCallback


	def startServer(self, callback: callable):
		"""
		-Crée le serveur
		-Attend que le client se connecte dans un thread (fonction __startServer)
		"""
		self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
		self.__thread = Thread(target=lambda: self.__startServer(callback))
		self.__thread.setDaemon(True)
		self.__thread.start()


	def __startServer(self, callback: callable):
		"""
		Fonction appellée par startServer dans un thread :
		-Attend que le client se connecte
		-Appelle [callback] quand le client est connecté
		"""
		try:
			self.socket.bind(("", self.PORT))
			self.socket.listen(1)
			self.client, address = self.socket.accept()
			callback()
		except:
			print(traceback.format_exc())
			self.errorCallback()
		print("End of startServer")


	def startReceive(self, callback: callable):
		"""
		Commence à recevoir les messages du client dans un thread (fonction __receive)
		[callback] est une fonction qui prend le header comme premier argument et le message comme deuxième argument
		"""
		self.callback = callback
		self.receiving = True
		self.__thread = Thread(target=self.__receive)
		self.__thread.setDaemon(True)
		self.__thread.start()


	def __receive(self):
		"""
		Fonction appellée par startReceive dans un thread :
		-Reçoit les messages du client
		-Appelle le callback donné à startReceive quand un message est reçu
		"""
		try:
			# Receive messages and return them in the callback
			remaining = bytearray()
			while self.receiving:
				# Receive parts of the message until it is complete
				message = remaining
				length = -1
				contentType = ""
				header = ""
				pointer = -1
				while length == -1 or pointer < length:
					if length == -1 and len(message) > 7:
						contentType = bytes([message[0]]).decode()
						header = message[1:4].decode()
						length = int.from_bytes(message[4:8], "little", signed=True)
						start = message[8:]
						message = bytearray(length + 1024)
						message[:len(start)] = start
						pointer = len(start)
						if pointer >= length: break

					messageBytes = self.client.recv(1024)
					if messageBytes:
						if pointer == -1:
							message += messageBytes
						else:
							message[pointer:pointer+len(messageBytes)] = messageBytes
							pointer += len(messageBytes)
					else:
						# Client disconnected
						self.receiving = False
						self.stopServer()
				remaining = message[length:pointer]
				message = message[:length]
				if self.receiving:
					if contentType == "b":
						self.callback(header, message)
					elif contentType == "s":
						self.callback(header, message.decode())

		except:
			print(traceback.format_exc())
			self.errorCallback()
		print("End of receive")


	def stopReceive(self):
		"""
		Arrête de recevoir les messages du client
		"""
		self.receiving = False


	def send(self, header: str, content):
		"""
		Envoie le message [message] au client
		[header] est une chaine de 3 caractères
		[content] est une chaine de caractères ou des bytes
		"""
		try:
			if isinstance(content, str):
				b = content.encode()
				self.client.send(("s" + header).encode() + len(b).to_bytes(4, "little", signed=True) + b)
			elif isinstance(content, bytes):
				self.client.send(("b" + header).encode() + len(content).to_bytes(4, "little", signed=True) + content)
			return True
		except:
			print(traceback.format_exc())
			self.errorCallback()
			return False

	def sendBroadcast(self, message: str):
		"""
		Envoie le message [message] de broadcast à tout le réseau
		-A utiliser au démarrage pour donner l'adresse IP du serveur au client
		"""
		sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		sock.setsockopt(socket.SOL_SOCKET, socket.SO_BROADCAST, 1)
		sock.sendto(message.encode(), ('<broadcast>', self.PORT))
		sock.close()


	def stopServer(self):
		"""
		Arrête le serveur
		"""
		try:
			self.client.shutdown(socket.SHUT_RDWR)
			self.client.close()
		except: pass
		try:
			self.socket.shutdown(socket.SHUT_RDWR)
			self.socket.close()
		except: pass