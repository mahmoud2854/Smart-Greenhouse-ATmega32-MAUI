/*==========================================================
 * Smart Greenhouse — GPIO Driver for ATmega32
 * File: hal_gpio.h
 * Controls actuators (PB0-PB2) and reads LDR (PA1)
 *==========================================================*/
#ifndef HAL_GPIO_H
#define HAL_GPIO_H

#include <stdint.h>

/* Actuator pin definitions (Port B) */
#define PUMP_PIN    0   /* PB0 — Relay for water pump */
#define FAN_PIN     1   /* PB1 — Relay for cooling fan */
#define LED_PIN     2   /* PB2 — Transistor for night LED */

/* Initialize GPIO: PB0-PB2 as outputs, PA1 as input */
void GPIO_Init(void);

/* Set actuator ON (pin HIGH) */
void GPIO_SetHigh(uint8_t pin);

/* Set actuator OFF (pin LOW) */
void GPIO_SetLow(uint8_t pin);

/* Toggle actuator */
void GPIO_Toggle(uint8_t pin);

/* Read actuator state: 1=ON, 0=OFF */
uint8_t GPIO_ReadOutput(uint8_t pin);

/* Read LDR on PA1: 1=Dark (HIGH), 0=Light (LOW) */
uint8_t GPIO_ReadLDR(void);

#endif /* HAL_GPIO_H */
