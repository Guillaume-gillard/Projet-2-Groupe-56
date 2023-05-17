import time
from math import cos, sin, pi

# Module pour interagir avec le GPIO (uniquement disponible sur le Raspberry)
import RPi.GPIO as GPIO

class Robot:
    """Gère le mouvement des deux moteurs pas à pas et le calcul de la position du robot"""

    # Pins
    M1STEP = 6
    M1DIR = 5
    M1ENABLE = 3
    M2STEP = 22
    M2DIR = 26
    M2ENABLE = 2

    # Valeurs
    M1FORWARD = GPIO.LOW
    M1BACKWARD = GPIO.HIGH
    M2FORWARD = GPIO.HIGH
    M2BACKWARD = GPIO.LOW
    STEPS_PER_ROTATION = 400

    # Un step fait tourner la roue de 1/200 de tour

    # Dimensions
    WHEEL_DIAMETER = 6.2 * 365 / 360 # Diamètre des roues (cm) + Correction expérimentale...
    DISTANCE_BETWEEN_WHEELS = 23.5 # Distance entre le centre des deux roues (cm)
    SENSOR_POSITION = (0, 6.5) # Position (x, y) du capteur de métaux (cm) par rapport au point entre les 2 roues

    # Autres
    STEPS_PER_ACTION = 40 # Nombre de pas entre chaque exécution de [otherAction] pour les fonctions de mouvement
    MAX_INSTANT_ACCELERATION = 200
    ACCELERATION_RATE = 4000


    class MotorMove:
        """Classe utilisée par la fonction move()"""
        
        totalCount = 0

        def __init__(self, pin, steps, startSpeed, maxSpeed, accelerationRate):
            self.pin = pin
            self.steps = steps
            self.startSpeed = startSpeed
            self.maxSpeed = abs(maxSpeed)
            self.accelerationRate = accelerationRate
            self.speed = abs(startSpeed)
            self.count = 0
            self.actionCount = 0
            self.isHigh = False
            self.waited = 0
            self.decelerating = False

        def wait(self, time):
            if self.speed != 0:
                self.waited += time
                if self.waited >= 1 / (2 * self.speed):
                    self.waited = 0
                    self.isHigh = not self.isHigh
                    GPIO.output(self.pin, GPIO.HIGH if self.isHigh else GPIO.LOW)
                    if self.decelerating:
                        self.decelerate(1 / (2 * self.speed))
                    else:
                        self.__accelerate(1 / (2 * self.speed))
                    if self.isHigh:
                        self.count += 1
                        self.actionCount += 1
                        self.__class__.totalCount += 1
                        if self.steps > 0 and self.count >= self.steps:
                            self.speed = 0

        def nextStep(self):
            if self.speed == 0:
                return 0
            else:
                return 1 / (2 * self.speed) - self.waited
            
        def getSpeed(self):
            return self.speed if self.startSpeed > 0 else -self.speed
            
        def __accelerate(self, passedTime):
            if self.speed != 0 and self.speed < self.maxSpeed:
                self.speed += self.accelerationRate * passedTime
                if self.speed > self.maxSpeed:
                    self.speed = self.maxSpeed

        def mustDecelerate(self, canJump):
            if self.steps == -1: 
                return False
            else:
                return (self.speed*self.speed - canJump*canJump) / (2 * self.accelerationRate) >= self.steps - self.count

        def startDecelerate(self):
            self.decelerating = True

        def decelerate(self, time):
            self.speed -= self.accelerationRate * time

        def canJumpTo(self, speed, canJump):
            return abs(speed - self.getSpeed()) <= canJump


    def __init__(self, otherAction: callable, breakCondition: callable):
        self.position = [0, 0] # Position (x, y) du point entre les 2 roues (cm)
        self.orientation = 0 # Orientation du robot (degrés)
        self.stopMovement = False
        self.stopped = True
        self.otherAction = otherAction
        self.breakCondition = breakCondition
        self.m1PreviousSpeed = 0
        self.m2PreviousSpeed = 0
        self.m1NextSpeed = 0
        self.m2NextSpeed = 0
        self.canJump1 = self.MAX_INSTANT_ACCELERATION
        self.canJump2 = self.MAX_INSTANT_ACCELERATION
        GPIO.setup(self.M1STEP, GPIO.OUT)
        GPIO.setup(self.M1DIR, GPIO.OUT)
        GPIO.setup(self.M1ENABLE, GPIO.OUT)
        GPIO.setup(self.M2STEP, GPIO.OUT)
        GPIO.setup(self.M2DIR, GPIO.OUT)
        GPIO.setup(self.M2ENABLE, GPIO.OUT)
        GPIO.output(self.M1ENABLE, GPIO.HIGH)
        GPIO.output(self.M2ENABLE, GPIO.HIGH)


    def getSensorPosition(self):
        """
        Renvoie la position actuelle du détecteur de métaux
        """
        # En fonction de self.position, self.orientation et self.SENSOR_POSITION
        return (
            self.position[0] + self.SENSOR_POSITION[0] * cos(self.orientation) - self.SENSOR_POSITION[1] * sin(self.orientation),
            self.position[1] + self.SENSOR_POSITION[0] * sin(self.orientation) + self.SENSOR_POSITION[1] * cos(self.orientation)
        )


    def move(self, m1Steps: int, m2Steps: int, m1Speed: float, m2Speed: float, positionIncrement: tuple, decelerate: bool = False):
        """
        -Fait tourner les deux moteurs pas à pas
         * de [m1Steps] et [m2Steps] respectivement (ou indéfiniment si ils valent -1)
         * à une vitesse de [m1Speed] et [m2Speed] (pas/s)
         * vers l'avant si la vitesse est positive, vers l'arrière sinon
        -Appelle [otherAction] à chaque [self.STEPS_PER_ACTION] pas
        -Arrête le mouvement si self.stopMovement == True (dans ce cas, le remettre à False)
        -Met à jour self.position et self.orientation grâce à [positionIncrement] :
        [positionIncrement] est un tuple contenant :
         * Si la trajectoire est circulaire : (r, da, m) où r est le rayon du cercle et da est l'angle (en degrés dans le sens trigonométrique) parcouru par le robot à chaque pas du moteur m (1 ou 2) (le cercle est à droite si r >= 0 et à gauche sinon)
         * Si la trajectoire est rectiligne : (None, dx, None) où dx est la distance parcourue par le robot à chaque pas du moteur 1
        """
        # Pendant que les moteurs tournent, l'exécution du programme sera bloquée dans cette fonction.
        # On ne pourra donc pas faire autre chose ou réagir à des évènements extérieurs.
        # C'est à ca que sert le paramètre [otherAction]
        # Nous y mettrons les actions à faire périodiquement (ex: enregistrer les données du capteur)
        GPIO.output(self.M1DIR, self.M1FORWARD if m1Speed > 0 else self.M1BACKWARD)
        GPIO.output(self.M2DIR, self.M2FORWARD if m2Speed > 0 else self.M2BACKWARD)

        if m1Speed == 0 and m2Speed == 0:
            count = 0
            while not self.stopMovement:
                if not self.stopped:
                    time.sleep(0.05)
                    count += 0.05
                    if count >= 0.2:
                        GPIO.output(self.M1ENABLE, GPIO.HIGH)
                        GPIO.output(self.M2ENABLE, GPIO.HIGH)
                        self.stopped = True
                        self.m1PreviousSpeed = 0
                        self.m2PreviousSpeed = 0
                        self.canJump1 = self.MAX_INSTANT_ACCELERATION
                        self.canJump2 = self.MAX_INSTANT_ACCELERATION

        else:
            if self.stopped:
                self.stopped = False
                GPIO.output(self.M1ENABLE, GPIO.LOW)
                GPIO.output(self.M2ENABLE, GPIO.LOW)
                time.sleep(0.1)
            
            if abs(m1Speed - self.m1PreviousSpeed) <= self.canJump1 and abs(m2Speed - self.m2PreviousSpeed) <= self.canJump2:
                jump1 = m1Speed - self.m1PreviousSpeed
                jump2 = m2Speed - self.m2PreviousSpeed
            else:
                if abs(m1Speed - self.m1PreviousSpeed) <= self.canJump1:
                    jump2 = self.canJump2 * (1 if m2Speed - self.m2PreviousSpeed > 0 else -1)
                    jump1 = jump2 * m1Speed / m2Speed
                elif abs(m2Speed - self.m2PreviousSpeed) <= self.canJump2:
                    jump1 = self.canJump1 * (1 if m1Speed - self.m1PreviousSpeed > 0 else -1)
                    jump2 = jump1 * m2Speed / m1Speed
                else:
                    jump1 = max(self.canJump1, self.canJump2 * m1Speed / m2Speed) * (1 if m1Speed - self.m1PreviousSpeed > 0 else -1)
                    jump2 = jump1 * m2Speed / m1Speed

            if abs(m1Speed) > abs(m2Speed):
                fastest = self.MotorMove(self.M1STEP, m1Steps, self.m1PreviousSpeed + jump1, m1Speed, self.ACCELERATION_RATE)
                slowest = self.MotorMove(self.M2STEP, m2Steps, self.m2PreviousSpeed + jump2, m2Speed, self.ACCELERATION_RATE * abs(m2Speed / m1Speed))
                m1, m2 = fastest, slowest
            else:
                fastest = self.MotorMove(self.M2STEP, m2Steps, self.m2PreviousSpeed + jump2, m2Speed, self.ACCELERATION_RATE)
                slowest = self.MotorMove(self.M1STEP, m1Steps, self.m1PreviousSpeed + jump1, m1Speed, self.ACCELERATION_RATE * abs(m1Speed / m2Speed))
                m1, m2 = slowest, fastest

            while True:
                wait1, wait2 = fastest.nextStep(), slowest.nextStep()
                if wait1 == 0 and wait2 == 0:
                    break
                elif wait1 == 0:
                    wait = wait2
                elif wait2 == 0:
                    wait = wait1
                else:
                    wait = min(wait1, wait2)

                time.sleep(wait)
                fastest.wait(wait)
                slowest.wait(wait)

                if not self.stopMovement and self.breakCondition():
                    self.stopMovement = True

                if not fastest.decelerating and (self.stopMovement or (decelerate and fastest.mustDecelerate(self.MAX_INSTANT_ACCELERATION))):
                    fastest.startDecelerate()
                    slowest.startDecelerate()

                if self.stopMovement:
                    if m1.canJumpTo(self.m1NextSpeed, self.MAX_INSTANT_ACCELERATION) and m2.canJumpTo(self.m2NextSpeed, self.MAX_INSTANT_ACCELERATION):
                        self.canJump1 = self.MAX_INSTANT_ACCELERATION
                        self.canJump2 = self.MAX_INSTANT_ACCELERATION
                        self.m1PreviousSpeed = m1.getSpeed()
                        self.m2PreviousSpeed = m2.getSpeed()
                        break
                    if fastest.canJumpTo(0, self.MAX_INSTANT_ACCELERATION / 2):
                        if m1.getSpeed() * self.m1NextSpeed > 0:
                            self.canJump1 = self.MAX_INSTANT_ACCELERATION + m1.speed
                        else:
                            self.canJump1 = self.MAX_INSTANT_ACCELERATION - m1.speed
                        if m2.getSpeed() * self.m2NextSpeed > 0:
                            self.canJump2 = self.MAX_INSTANT_ACCELERATION + m2.speed
                        else:
                            self.canJump2 = self.MAX_INSTANT_ACCELERATION - m2.speed
                        self.m1PreviousSpeed = 0
                        self.m2PreviousSpeed = 0
                        break

                if self.MotorMove.totalCount >= self.STEPS_PER_ACTION:
                    self.MotorMove.totalCount = 0
                    self.__incrementPosition(positionIncrement[0], positionIncrement[1] * (m1.actionCount if positionIncrement[2] == 1 else m2.actionCount))
                    m1.actionCount = 0
                    m2.actionCount = 0
                    self.otherAction()

            self.__incrementPosition(positionIncrement[0], positionIncrement[1] * (m1.actionCount if positionIncrement[2] == 1 else m2.actionCount))
            self.m1NextSpeed = 0
            self.m2NextSpeed = 0
            if self.stopMovement:
                self.stopMovement = False
            else:
                self.m1PreviousSpeed = 0
                self.m2PreviousSpeed = 0
                self.canJump1 = self.MAX_INSTANT_ACCELERATION
                self.canJump2 = self.MAX_INSTANT_ACCELERATION
    

    def __incrementPosition(self, r, da):
        if r == None:
            self.position[0] -= da * sin(self.orientation)
            self.position[1] += da * cos(self.orientation)
        else:
            da *= pi / 180
            self.position[0] += r * (cos(self.orientation) - cos(self.orientation + da))
            self.position[1] += r * (sin(self.orientation) - sin(self.orientation + da))
            self.orientation += da
            if self.orientation > 2 * pi:
                self.orientation -= 2 * pi


    def nothing(self):
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Ne rien faire (et arrêter les moteurs quand il faut)
        """
        return (0, 0, 0, 0, (None, 0, None))


    def forward(self, speed: float, distance: float = -1, decelerate: bool = False) -> tuple:
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Faire avancer le robot en avant de [distance] (cm) à la vitesse [speed] (cm/s)
        -Appelle [otherAction] périodiquement
        -L'argument [distance] est facultatif : s'il n'est pas donné, le robot avance indéfiniment
        """
        roue200eme = (self.WHEEL_DIAMETER*pi) / self.STEPS_PER_ROTATION
        m1Speed = speed/roue200eme # Je calcule le nombre de pas à faire par seconde pour aller à la vitesse demandée
        m2Speed = m1Speed # On ne tourne pas donc les roues vont à la même vitesse
        distance_steps = distance//roue200eme
        positionIncrement = (None, roue200eme if speed > 0 else -roue200eme, 1)
        if distance == -1:
            return (-1, -1, m1Speed, m2Speed, positionIncrement, decelerate)
        elif distance > 0:
            return (distance_steps, distance_steps, m1Speed, m2Speed, positionIncrement, decelerate)
        else:
            raise ValueError
        
        # Cette fonction n'interagit pas directement avec les moteurs, elle appelle juste la fonction _move avec les bons arguments.
        # C'est la fonction _move qui gérera tout le reste.


    def backward(self, speed: float, distance: float = -1, decelerate: bool = False) -> tuple:
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Faire avancer le robot en arrière de [distance] (cm) à la vitesse [speed] (cm/s)
        -Appelle [otherAction] périodiquement
        -L'argument [distance] est facultatif : si il n'est pas donné, le robot avance indéfiniment
        """
        # Cette fonction n'interagit pas directement avec les moteurs, elle appelle juste la fonction _move avec les bons arguments.
        # C'est la fonction _move qui gérera tout le reste.
        return self.forward(-speed, distance, decelerate)


    def turnRight(self, speed: float, angle: float = -1, decelerate: bool = False) -> tuple:
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Faire tourner le robot à droite de [angle] (degrés) à la vitesse [speed] (degrés/s)
        -Appelle [otherAction] périodiquement
        -L'argument [angle] est facultatif : si il n'est pas donné, le robot tourne indéfiniment
        """
        # Cette fonction n'interagit pas directement avec les moteurs, elle appelle juste la fonction _move avec les bons arguments.
        # C'est la fonction _move qui gérera tout le reste.
        nangles = 360 / (self.DISTANCE_BETWEEN_WHEELS / self.WHEEL_DIAMETER * self.STEPS_PER_ROTATION)
        nsteps  = angle / nangles
        m1Speed = speed / nangles
        positionIncrement = (0, nangles if speed < 0 else -nangles, 1 if speed > 0 else 2)
        if angle >= 0:
            return (nsteps, nsteps, m1Speed, -m1Speed, positionIncrement, decelerate)
        else:
            return (-1, -1, m1Speed, -m1Speed, positionIncrement, decelerate)
    
  

    def turnLeft(self, speed: float, angle: float = -1, decelerate: bool = False) -> tuple:
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Faire tourner le robot à gauche de [angle] (degrés) à la vitesse [speed] (degrés/s)
        -Appelle [otherAction] périodiquement
        -L'argument [angle] est facultatif : si il n'est pas donné, le robot tourne indéfiniment
        """
        # Cette fonction n'interagit pas directement avec les moteurs, elle appelle juste la fonction _move avec les bons arguments.
        # C'est la fonction _move qui gérera tout le reste.
        return self.turnRight(-speed, angle, decelerate)


    def turnWhileMoving(self, wheel1Speed: float, wheel2Speed: float, wheel1Distance: float = -1, wheel2Distance: float = -1, decelerate: bool = False) -> tuple:
        """
        Renvoie les arguments à donner à Robot.move() pour :
        Faire tourner les deux roues du robot à des vitesses [wheel1Speed] et [wheel2Speed] (cm/s) respectivement
        -Sens : vers l'avant si la vitesse est positive, vers l'arrière sinon
        -Les roues s'arrêtent après [wheel1Distance] et [wheel2Distance] cm respectivement
        -Appelle [otherAction] périodiquement
        -Les arguments [wheel1Distance] et [wheel2Distance] sont facultatifs : si ils ne sont pas donnés, les roues tournent indéfiniment
        """
        # Cette fonction n'interagit pas directement avec les moteurs, elle appelle juste la fonction _move avec les bons arguments.
        # C'est la fonction _move qui gérera tout le reste.
        tour200eme = (self.WHEEL_DIAMETER*pi) / self.STEPS_PER_ROTATION
        r = (self.DISTANCE_BETWEEN_WHEELS/2) * (wheel1Speed + wheel2Speed) / (wheel1Speed - wheel2Speed)
        if wheel1Speed == 0:
            da = 180 * self.WHEEL_DIAMETER * wheel2Speed / (self.STEPS_PER_ROTATION * abs(r*wheel2Speed))
        else:
            da = -180 * self.WHEEL_DIAMETER * (wheel1Speed+wheel2Speed) / (2 * r * self.STEPS_PER_ROTATION * abs(wheel1Speed))
        positionIncrement = (r, da, 2 if wheel1Speed == 0 else 1)
        return (wheel1Distance/tour200eme, wheel2Distance/tour200eme, wheel1Speed/tour200eme, wheel2Speed/tour200eme, positionIncrement, decelerate)

    
    def stop(self, nextMove):
        """
        Fonction permettant d'arrêter le mouvement des moteurs
        - [nextMove] est une prévision du prochain mouvement qui sera demandé au robot pour commencer à décelérer
        """
        self.m1NextSpeed = nextMove[2]
        self.m2NextSpeed = nextMove[3]
        self.stopMovement = True


    def reset(self):
        """
        Supprime toutes les données pour recommencer une nouvelle carte
        """
        self.position = [0, 0]
        self.orientation = 0
        self.stopMovement = False
        self.stopped = True
        GPIO.output(self.M1ENABLE, GPIO.HIGH)
        GPIO.output(self.M2ENABLE, GPIO.HIGH)