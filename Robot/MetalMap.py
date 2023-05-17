# Modules pour récupérer les données du détecteur de métaux
import Adafruit_GPIO.SPI 
import Adafruit_MCP3008
import numpy as np

# Module pour transformer des objets en chaine de caractères
import json


class MetalMap:
    """Gère la collecte des données du détecteur de métaux, 
    l'association de ces données avec la position du robot et 
    l'envoi des données à la télécommande"""

    SEND_FREQUENCY = 10 # Nombre de addData() entre chaque envoi automatique à la télécommande

    def __init__(self, main, robot):
        self.main = main
        self.robot = robot # Référence de l'objet Robot pour avoir accès à la position du robot (robot.getSensorPosition())
        self.data = {} # Dictionnaire associant les positions (tuple[float]) auxquelles ont a prélevé les données du détecteur et la valeur du métal détecté à ces positions
        self.matrix = -np.ones((3,3))
        self.originCoords = (0, 0) # Coordonnées de l'entrée de self.matrix représentant la case dont le centre est la position (0, 0) du capteur de métaux
        self.decal_x = 0
        self.decal_y = 0
        self.mcp3008 = Adafruit_MCP3008.MCP3008(spi=Adafruit_GPIO.SPI.SpiDev(0, 0)) # Objet pour récupérer les données du détecteur de métaux
        self.cellSize = 1 # Taille d'une case de la grille
        self.count = 0
        self.values = np.array([])
        self.x = 0
        self.y = 0

    # Permet d'ajouter des colonnes ou des lignes à la matrice
    def addColR(self, n):
        self.matrix = np.c_[self.matrix, -np.ones((np.shape(self.matrix)[0],n))]

    def addColL(self, n):
        self.matrix = np.c_[-np.ones((np.shape(self.matrix)[0],n)),self.matrix]

    def addRowU(self, n):
        self.matrix = np.r_[-np.ones((n, np.shape(self.matrix)[1])), self.matrix]

    def addRowD(self,n):
        self.matrix = np.r_[self.matrix, -np.ones((n, np.shape(self.matrix)[1]))]

    def addToMatrix(self, value, x, y):
        x += self.decal_x,
        y += self.decal_y
        if x >= np.shape(self.matrix)[0]:
            self.addRowD(x - np.shape(self.matrix)[0]+1)
        if y >= np.shape(self.matrix)[1]:
            self.addColR(y - np.shape(self.matrix)[1]+1)
        if x < 0:
            self.decal_x += abs(x)
            self.addRowU(-x)
            x = 0
        if y < 0:
            self.decal_y += abs(y)
            self.addColL(-y)
            y = 0
        self.matrix[x, y] = value

    def addData(self, lastSend=False):
        """
        -Récupère la valeur captée par le détecteur de métaux
        -Associe la position actuelle du détecteur de métaux avec cette valeur dans self.data
        -Ajoute la valeur au bon endroit dans self.matrix (et y ajouter une ligne ou une colonne au début ou à la fin et mettre à jour self.originCoords si besoin)
        -Une fois sur [self.SEND_FREQUENCY], appelle self.sendMap()
        """
        position = self.robot.getSensorPosition()
        value = 1 - self.mcp3008.read_adc(0) / 855
        self.data[position] = value

        self.count += 1
        xtmp = position[0] / self.cellSize + 0.5
        ytmp = position[1] / self.cellSize + 0.5
        x = int(xtmp if xtmp > 0 else xtmp - 1)
        y = int(ytmp if ytmp > 0 else ytmp - 1)

        if x == self.x and y == self.y:    # Si on est sur la même case, rajoute une donnée dans la liste
            self.values = np.append(self.values, value)

        else:
            if len(self.values) > 0:    # self.x et self.y sont de base à 0, 0 donc lorsque qu'on exécute le programme, il veut envoyer la moyenne d'une liste vide
                self.addToMatrix(np.mean(self.values), self.x, self.y)
            self.x = x
            self.y = y
            self.values = np.array([value])
        
        if self.count == self.SEND_FREQUENCY or lastSend:  # envoie les donnée
            self.addToMatrix(np.mean(self.values), self.x, self.y)
            self.count = 0
            self.sendMap()

    
    def changePrecision(self, newprecision):
        self.cellSize = newprecision

        self.decal_x = 0
        self.decal_y = 0
        self.x = 0
        self.y = 0
        self.matrix = -np.ones((3,3))
        self.values = np.array([])

        counts = {}
        for position in self.data:
            xtmp = position[0] / self.cellSize + 0.5
            ytmp = position[1] / self.cellSize + 0.5
            x = int(xtmp if xtmp > 0 else xtmp - 1)
            y = int(ytmp if ytmp > 0 else ytmp - 1)
            intpos = (x, y)
            if intpos in counts:
                counts[intpos] += 1
                self.addToMatrix(((counts[intpos] - 1) * self.matrix[x + self.decal_x, y + self.decal_y] + self.data[position]) / counts[intpos], x, y)
            else:
                counts[intpos] = 1
                self.addToMatrix(self.data[position], x, y)

        self.sendMap()
        self.count = 0


    def sendMap(self):
        """
        -Récupère la grille
        -Transforme la grille en une chaine de caractère
        -Envoie la grille à la télécommande
        """
        pos = self.robot.getSensorPosition()
        self.server.send("Map", "{};{};{};{};{};{}".format(pos[0], pos[1], self.robot.orientation, self.decal_x, self.decal_y, json.dumps(self.matrix.tolist())))

    
    def clearData(self):
        """
        Supprime toutes les données pour recommencer une nouvelle carte
        """
        self.data = {}
        self.decal_x = 0
        self.decal_y = 0
        self.x = 0
        self.y = 0
        self.matrix = -np.ones((3,3))
        self.values = np.array([])


    def sendMap(self):
        """
        -Récupère la grille
        -Transforme la grille en une chaine de caractère
        -Envoie la grille à la télécommande
        """
        pos = self.robot.getSensorPosition()
        self.main.mustSend.append(("Map", "{};{};{};{};{};{}".format(pos[0], pos[1], self.robot.orientation, self.originCoords[0], self.originCoords[1], json.dumps(self.matrix.tolist()))))