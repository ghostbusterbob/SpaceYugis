#include <BleKeyboard.h>

BleKeyboard bleKeyboard;

const int buttonPin_start = 2;

int buttonState_start = HIGH;

void setup() {
  Serial.begin(115200);
  Serial.println("Starting BLE work!");
  bleKeyboard.begin();
  
  pinMode(buttonPin_start, INPUT);
}

void loop() {
}
