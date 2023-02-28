import RPi.GPIO as GPIO # Module pour interagir avec le Raspberry
from time import sleep # Importer sleep pour les temps d'attente entre les pas

# Définition des pins
M1STEP = 5 # => Le pin 'Step' du driver du moteur 1 est connecté au GPIO 5 du Raspberry
M1DIR = 6
M1ENBL = 26

M2STEP = 17
M2DIR = 27
M2ENBL = 22

# Configurations du module
GPIO.setmode(GPIO.BCM)
GPIO.setwarnings(False)

# Configuration des pins en mode 'output'
GPIO.setup(M1STEP, GPIO.OUT)
GPIO.setup(M1DIR, GPIO.OUT)
GPIO.setup(M1ENBL, GPIO.OUT)
GPIO.setup(M2STEP, GPIO.OUT)
GPIO.setup(M2DIR, GPIO.OUT)
GPIO.setup(M2ENBL, GPIO.OUT)

# Désactiver les moteurs (car on ne les fait pas encore tourner)
GPIO.output(M1ENBL, GPIO.HIGH)
GPIO.output(M2ENBL, GPIO.HIGH)

# Fonction pour faire tourner les 2 moteurs de [step] pas avec un temps d'attente [wait] entre chaque pas
def spin(steps, wait):
    for i in range(steps):
        GPIO.output(M1STEP, GPIO.HIGH)
        GPIO.output(M2STEP, GPIO.HIGH)
        sleep(wait / 2)
        GPIO.output(M1STEP, GPIO.LOW)
        GPIO.output(M2STEP, GPIO.LOW)
        sleep(wait / 2)

# Calcul du temps d'attente en fonction de la vitesse voulue
speed = 1
stepsPerRotation = 200
rotations = 1
wait = 1 / (speed * stepsPerRotation)

# Boucle infinie : fait tourner les moteurs de [rotations] tours avec une vitesse de [speed] tours/s dans un sens, puis dans l'autre à chaque fois qu'on appuie sur enter
while True:
    input("") # Attendre qu'on appuie sur enter

    # Activer les moteurs (on va les faire tourner) et attendre 0.2s
    GPIO.output(M1ENBL, GPIO.LOW)
    GPIO.output(M2ENBL, GPIO.LOW)
    sleep(0.2)

    # Configurer le sens de rotation
    GPIO.output(M1DIR, GPIO.LOW)
    GPIO.output(M2DIR, GPIO.LOW)

    # Faire tourner les moteurs
    spin(stepsPerRotation * rotations, wait)
    
    # Désactiver les moteurs après 0.2s
    sleep(0.2)
    GPIO.output(M1ENBL, GPIO.HIGH)
    GPIO.output(M2ENBL, GPIO.HIGH)

    input("") # Attendre qu'on appuie sur enter

    # Activer les moteurs (on va les faire tourner) et attendre 0.2s
    GPIO.output(M1ENBL, GPIO.LOW)
    GPIO.output(M2ENBL, GPIO.LOW)
    sleep(0.2)

    # Configurer le sens de rotation (l'autre sens qu'avant)
    GPIO.output(M1DIR, GPIO.HIGH)
    GPIO.output(M2DIR, GPIO.HIGH)

    # Faire tourner les moteurs
    spin(stepsPerRotation * rotations, wait)

    # Désactiver les moteurs après 0.2s
    sleep(0.2)
    GPIO.output(M1ENBL, GPIO.HIGH)
    GPIO.output(M2ENBL, GPIO.HIGH)