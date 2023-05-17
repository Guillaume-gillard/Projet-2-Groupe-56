import RPi.GPIO as GPIO
import time
from math import pi
from Robot import Robot
from MetalMap import MetalMap
from SocketServer import SocketServer
from picamera import PiCamera
import io
from threading import Thread
import os


class Main:
    """
    Programme principal
    """

    # Pins
    POWER_LED = 16
    CONNECTION_LED = 27
    TEST_LED = 17
    BUTTON = 4
    
    def __init__(self):
        GPIO.setmode(GPIO.BCM)
        GPIO.setwarnings(False)
        GPIO.setup(self.CONNECTION_LED, GPIO.OUT)
        GPIO.setup(self.TEST_LED, GPIO.OUT)
        GPIO.setup(self.POWER_LED, GPIO.OUT)
        GPIO.setup(self.BUTTON, GPIO.IN, pull_up_down=GPIO.PUD_UP)
        GPIO.output(self.POWER_LED, GPIO.HIGH)
        GPIO.output(self.CONNECTION_LED, GPIO.LOW)
        GPIO.output(self.TEST_LED, GPIO.LOW)
        self.started = False
        self.camera = None
        self.launcher()


    def launcher(self):
        while True:
            time.sleep(0.1)
            if GPIO.input(self.BUTTON) == GPIO.LOW:
                t = time.time()
                reset = False
                while True:
                    if GPIO.input(self.BUTTON) == GPIO.HIGH:
                        break
                    if time.time() - t > 2 and time.time() - t < 3:
                        GPIO.output(self.TEST_LED, GPIO.HIGH)
                    if time.time() - t > 5:
                        GPIO.output(self.TEST_LED, GPIO.LOW)
                    if time.time() - t > 8 and not reset:
                        reset = True
                        GPIO.output(self.TEST_LED, GPIO.HIGH)
                        time.sleep(0.15)
                        GPIO.output(self.TEST_LED, GPIO.LOW)
                    time.sleep(0.1)

                if time.time() - t < 1:
                    if self.started:
                        self.onMessageReceive("", "shutdown")
                    else:
                        self.started = True
                        self.mainThread = Thread(target=self.start)
                        self.mainThread.setDaemon(True)
                        self.mainThread.start()
                    time.sleep(1)

                elif time.time() - t > 2 and time.time() - t < 5:
                    GPIO.output(self.TEST_LED, GPIO.LOW)
                    if self.started:
                        self.onMessageReceive("", "shutdown")
                        time.sleep(2)
                    if self.camera != None:
                        self.camera.stop_preview()
                        self.camera.close()
                    return

                elif time.time() - t > 5 and time.time() - t < 8:
                    break
        
        GPIO.output(self.TEST_LED, GPIO.HIGH)
        time.sleep(0.25)
        GPIO.output(self.TEST_LED, GPIO.LOW)
        if self.started:
            self.onMessageReceive("", "shutdown")
            time.sleep(2)
        os.system("sudo shutdown -h now")
        
      
    def start(self):
        """
        Fonction appellée au démarrage du programme
        -Envoie le broadcast pour donner l'adresse IP du serveur (= le robot) au client (= la télécommande)
        -Démarre le serveur
        """
        self.robot = Robot(self.robotOtherAction, self.robotBreakCondition)
        self.server = SocketServer(self.serverErrorCallback)
        self.metalMap = MetalMap(self, self.robot)
        self.instruction = "end"  # Dernière instruction recue de la télécommande
        self.mode = ""  # "controlled" si on est en mode télécommandé, "scan" si on est en mode scan
        self.moveArgs = self.robot.nothing() # Arguments (calculés à l'avance) à donner à self.robot.move() pour le prochain mouvement
        self.lastInstructionTime = 0 # Temps auquel la dernière instruction a été reçue
        self.connected = False
        self.mustSend = []

        print("Sending broadcast and starting server")
        self.server.sendBroadcast("IP")
        self.server.startServer(self.onClientConnected)
        self.cameraThread = Thread(target=self.sendCameraImages)
        self.cameraThread.daemon = True
        self.cameraThread.start()
        self.main()
        print("End of main")
  
      
    def onClientConnected(self):
        """
        Fonction donnée en callback à la fonction SocketServer.startServer() lors du démarrage du serveur
        Elle est donc appellée quand le client se connecte
        -Démarre la réception des messages du client
        """
        print("Client connected")
        GPIO.output(self.CONNECTION_LED, GPIO.HIGH)
        self.server.startReceive(self.onMessageReceive)
        self.connected = True
  
      
    def onMessageReceive(self, header: str, message: str):
        """
        Fonction donnée en callback à la fonction SocketServer.startReceive() lors du démarrage de la réception
        Elle est donc appellée quand un message est reçu
        Si la nouvelle instruction diffère de la dernière instruction recue :
        - Met à jour self.instruction
        - Arrête le mouvement des moteurs si on est en mode télécommandé
        - Arrête le mouvement des moteurs si on est en mode scan et que l'instruction est "end"
        """
        self.lastInstructionTime = time.time()
        if message != self.instruction:
            self.instruction = message
            if self.mode == "controlled":
                if message == "end" or message == "shutdown" or message.startswith("precision"):
                    self.robot.stop(self.robot.nothing())
                else:
                    self.moveArgs = self.controlledMoveArgs()
                    self.robot.stop(self.moveArgs)
            else:
                self.robot.stop(self.robot.nothing())
  
      
    def main(self):
        """
        Boucle infinie
        A chaque itération, vérifie la variable self.instruction
        Celle-ci contiendra les commandes envoyées par la télécommande
        Commandes :
         * end : ne rien faire (c'était la commande pour quitter le mode précédent)
         * scan [sizeX] [sizeY] [precision] [speed] : appeler self.scan(sizeX, sizeY, precision, speed)
         * controlled [precision] : appeler self.controlled(precision)
         * shutdown : quitter la boucle
        A la fin de la boucle, appeler self.stopRobot()
        """
        while self.started:
          self.mode = ""
          cmd = self.instruction.split()
          if cmd[0] == "end" :
            self.mode = ""
            self.robot.move(*self.robot.nothing())
          elif cmd[0] == "scan" : 
            self.robot.reset()
            self.metalMap.clearData()
            self.instruction = ""
            self.mode = "scan"
            self.scan(float(cmd[1]), float(cmd[2]), float(cmd[3]), float(cmd[4]))
          elif cmd[0] == "controlled" :
            self.robot.reset()
            self.metalMap.clearData()
            self.instruction = "nothing"
            self.mode = "controlled"
            self.controlled(float(cmd[1]))
          if cmd[0] == "shutdown" :
            self.robot.reset()
            self.stopRobot()
  
      
    def scan(self, sizeX: int, sizeY: int, precision: float, speed: float):
        """
        Scanne une zone de [sizeX] x [sizeY] (en cm) en faisant des allers-retours (on considère que la position actuelle du robot est le coin inférieur gauche)
        Si self.instruction == "end" ou == "shutdown" : arrête la fonction
        [precision] est :
        -La taille (en cm) d'une case de la grille qui sera renvoyée à la télécommande 
        -La distance entre chaque ligne parcourue par le robot
        -Donne self.robotOtherAction comme [otherAction] aux fonctions de mouvement de Robot
        """
        i = 0 #variable qui va dire si on tourne à gauche ou a droite. 
        count = 0 
        self.metalMap.cellSize = precision
        while count < sizeX / precision:
            time.sleep(0.2)
            if self.instruction == "end" or self.instruction == "shutdown":
                return
            self.robot.move(*self.robot.forward(speed * self.robot.WHEEL_DIAMETER * pi, sizeY, True))
            
            if count + 1 >= sizeX / precision:
                break

            time.sleep(0.2)
            if self.instruction == "end" or self.instruction == "shutdown":
                return 
            self.robot.move(*self.robot.turnRight(speed*(-1)**i * 360 / (self.robot.DISTANCE_BETWEEN_WHEELS / self.robot.WHEEL_DIAMETER), 90, True))
            
            time.sleep(0.2)
            if self.instruction == "end" or self.instruction == "shutdown":
                return
            self.robot.move(*self.robot.forward(speed * self.robot.WHEEL_DIAMETER * pi, precision, True))

            time.sleep(0.2)
            if self.instruction == "end" or self.instruction == "shutdown":
                return
            self.robot.move(*self.robot.turnRight(speed*(-1)**i * 360 / (self.robot.DISTANCE_BETWEEN_WHEELS / self.robot.WHEEL_DIAMETER), 90, True))
            
            i += 1
            count += 1
        self.metalMap.sendMap(True)
        self.instruction = "end"
  
      
    def controlled(self, precision):
        """
        Démarre le mode télécommandé :
        -Attend la première instruction, puis :
        -Boucle jusqu'à ce que self.instruction == "end" ou == "shutdown"
         -A chaque fois, fait tourner les moteurs en fonction de l'instruction et indéfiniment (ils seront arrêtés à la prochaine instruction en utilisant Motor.stopMovement())
         -Instructions :
          * forward [speed]
          * backward [speed]
          * left [speed]
          * right [speed]
          * combine [speed1] [speed2]
          * nothing
         -Donne self.robotOtherAction comme [otherAction] aux fonctions de mouvement de Robot
        """
        self.metalMap.cellSize = precision
        while self.instruction != "end" and self.instruction != "shutdown":
            if time.time() - self.lastInstructionTime > 1:
                self.robot.move(*self.robot.nothing())
            elif self.instruction.startswith("precision"):
                self.metalMap.changePrecision(float(self.instruction.split()[1]))
            else:
                self.robot.move(*self.moveArgs)


    def controlledMoveArgs(self):
        """
        Retourne les arguments à donner à self.robot.move() pour exécuter self.instruction
        """ 
        input = self.instruction.split()
        cmd = input[0]
        if len(input) > 1:
            speed = float(input[1])
        if cmd == "nothing":
            return self.robot.nothing()
        elif cmd == "combine" :
            return self.robot.turnWhileMoving(float(input[1]), float(input[2]))
        elif cmd == "forward" :
            return self.robot.forward(speed)
        elif cmd == "backward" :
            return self.robot.backward(speed)
        elif cmd == "left" :
            return self.robot.turnLeft(speed)
        elif cmd == "right" :
            return self.robot.turnRight(speed)
        

    def serverErrorCallback(self):
        if self.started:
            self.onMessageReceive("", "shutdown")


    def robotOtherAction(self):
        """
        Fonction à donner en tant que [otherAction] aux fonctions de mouvement de la classe Robot
        """
        self.metalMap.addData()


    def robotBreakCondition(self):
        return self.mode == "controlled" and time.time() - self.lastInstructionTime > 1
        
        
    def stopRobot(self):
        """
        -Arrête la connection avec la télécommande
        -Fin du programme (jusqu'à ce que launcher() le relance)
        """
        print("Stopping server")
        GPIO.output(self.CONNECTION_LED, GPIO.LOW)
        self.server.stopReceive()
        time.sleep(1)
        self.server.stopServer()
        self.started = False

    
    def sendCameraImages(self):
        while not self.connected and self.started: pass
        resolution = (800, 600)
        self.server.send("Res", str(resolution[0]) + ";" + str(resolution[1]))
        if self.camera == None and self.started:
            self.camera = PiCamera()
            self.camera.resolution = (resolution[0], resolution[1])
            time.sleep(2)
        while self.started:
            stream = io.BytesIO()
            self.camera.capture(stream, "jpeg", use_video_port=True)
            success = self.server.send("Img", stream.getvalue())
            if not success: break
            while len(self.mustSend) > 0:
                self.server.send(*self.mustSend[0])
                del self.mustSend[0]
        print("End of camera")


Main()
print("See you later alligator")