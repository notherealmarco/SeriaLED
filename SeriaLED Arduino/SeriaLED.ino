String reading = "";
bool completed = false;
bool fade = true;
int red, green, blue, white;
void setup() {
    TCCR1B = _BV(COM0A1) | _BV(COM0B1) | _BV(WGM00);
    TCCR0B = _BV(COM0A1) | _BV(COM0B1) | _BV(WGM00);
    TCCR2B = _BV(COM0A1) | _BV(COM0B1) | _BV(WGM00);
    pinMode(3, OUTPUT);
    pinMode(5, OUTPUT);
    pinMode(6, OUTPUT);
    pinMode(9, OUTPUT);
    Serial.begin(9600);
    startFading();
    Serial.println("r");
    Serial.println("x");
}

void loop() {
  serialScan();
}

void serialScan() {
  if (Serial.available() > 0) {
        char data = Serial.read();
        if (data == '\n') completed = true; else reading = reading + data;
        return;
    }
  if (completed == false) return;
  if (reading.equals("x"))
  {
    fade = true;
    reading = "";
    completed = false;
    startFading();
    return;
  }
  if (reading.equals("z"))
  {
    fade = false;
    reading = "";
    completed = false;
    return;
  }
  char charBuf[reading.length() + 1];
  reading.toCharArray(charBuf, reading.length() + 1);
  reading = "";
  completed = false;
  inputToRGBW(charBuf);
  setColors();
}

void startFading() {
  while (fade) {
      serialScan();
      for(int i = 0; i < 256; i++){
        if (!fade) break;
        serialScan();
        analogWrite(3, 255-i);
        analogWrite(5, i);
        analogWrite(6, 0);
        analogWrite(9, 0);
        delay(1500);
      }
      for(int i = 0; i < 256; i++){
        if (!fade) break;
        serialScan();
        analogWrite(3, 0);
        analogWrite(5, 255-i);
        analogWrite(6, i);
        analogWrite(9, 0);
        delay(1500);
      }
      for(int i = 0; i < 256; i++){
        if (!fade) break;
        serialScan();
        analogWrite(3, i);
        analogWrite(5, 0);
        analogWrite(6, 255-i);
        analogWrite(9, 0);
        delay(1500);
      }
    }
    Serial.println("z");
    setColors();
}

void setColors() {
  analogWrite(3, red);
  analogWrite(5, green);
  analogWrite(6, blue);
  analogWrite(9, white);
}

void inputToRGBW(const char * input) {
  if (strlen(input) == 6) {
    red = fromhex (& input [0]);
    green = fromhex (& input [2]);
    blue = fromhex (& input [4]);
    white = 0;
  } else if (strlen(input) == 8) {
    red = fromhex (& input [0]);
    green = fromhex (& input [2]);
    blue = fromhex (& input [4]);
    white = fromhex (& input [6]);
  } else if (strlen(input) == 10)
    red = fromhex (& input [0]);
    green = fromhex (& input [2]);
    blue = fromhex (& input [4]);
    white = fromhex (& input [6]);
  } else if (strlen(input) == 9) {
    red = fromhex (& input [1]);
    green = fromhex (& input [3]);
    blue = fromhex (& input [5]);
    white = fromhex (& input [7]);
  } else {
    Serial.println("Wrong length of input");
  }
}
byte fromhex (const char * str)
{
  char c = str [0] - '0';
  if (c > 9)
    c -= 7;
  int result = c;
  c = str [1] - '0';
  if (c > 9)
    c -= 7;
  return (result << 4) | c;
}
