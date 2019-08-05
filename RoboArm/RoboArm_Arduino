#include <Servo.h>

const int DOF = 6;
Servo servos[DOF];
String readIn = "";

int bounds[][2] = {
  {60, 125},  //0
  {0, 180},   //1
  {0, 180},   //2
  {0, 165},   //3
  {0, 180},   //4
  {0, 180},   //5
};

int startingAngles[] = {
  bounds[0][0],
  107,
  45,
  150,
  120,
  90
};

void setup()
{
  Serial.begin(57600);

  servos[0].attach(3);
  servos[1].attach(5);
  servos[2].attach(6);
  servos[3].attach(9);
  servos[4].attach(10);
  servos[5].attach(11);

  pinMode(LED_BUILTIN, OUTPUT);

  for(int i=0; i<DOF; i++){
    servos[i].write(startingAngles[i]);
  }
}

void loop() {
  
  while(Serial.available()) {
    char c = Serial.read();  //gets one byte from serial buffer
    readIn += c; //makes the string readString
  }

//readIn = "_2,100;";

  int head = readIn.indexOf("_");
  int tail = readIn.indexOf(";", head);
  if (head >= 0 && tail >= 0) {
    digitalWrite(LED_BUILTIN, HIGH);
    String raw = readIn.substring(head, tail);
    
    String rawIndex = raw.substring(1, 2);
    int index = rawIndex.toInt();

    raw = raw.substring(3);
    double pos = raw.toDouble();
    pos = confine(pos, index);
    
    servos[index].write(pos);
    
    readIn = readIn.substring(tail+1);
  }else{
    digitalWrite(LED_BUILTIN, LOW);
  }
}

double confine(double d, int i){
  int range[2];
  range[0] = bounds[i][0];
  range[1] = bounds[i][1];

  if(d < range[0]){
    d = (double) range[0];
  }else if(d > range[1]){
    d = (double) range[1];
  }

  return d;
}

//
//    while(true){
//      for (int i = 0; i < pos; i++) {
//        digitalWrite(LED_BUILTIN, HIGH);   // turn the LED on (HIGH is the voltage level)
//        delay(100);                       // wait for a second
//        digitalWrite(LED_BUILTIN, LOW);    // turn the LED off by making the voltage LOW
//        delay(100);
//      }
//      delay(700);
//    }
