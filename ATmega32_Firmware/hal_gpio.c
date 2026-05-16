/*==========================================================
 * Smart Greenhouse — GPIO Driver for ATmega32
 * File: hal_gpio.c
 *==========================================================*/
#include "hal_gpio.h"
#include "greenhouse_logic.h"
#include <avr/io.h>

void GPIO_Init(void)
{
    /* PB0, PB1, PB2 as outputs (Pump, Fan, LED) */
    DDRB |= (1 << PUMP_PIN) | (1 << FAN_PIN) | (1 << LED_PIN);

    /* Initialize actuators to OFF state depending on relay polarity */
#if RELAY_ACTIVE_LOW
    PORTB |= (1 << PUMP_PIN) | (1 << FAN_PIN);  /* HIGH keeps Active LOW relay OFF */
#else
    PORTB &= ~((1 << PUMP_PIN) | (1 << FAN_PIN)); /* LOW keeps Active HIGH relay OFF */
#endif
    /* LED is active HIGH (direct drive) */
    PORTB &= ~(1 << LED_PIN);

    /* PA1 as input for LDR (no internal pull-up, external voltage divider) */
    DDRA  &= ~(1 << 1);
    PORTA &= ~(1 << 1);
}

void GPIO_SetHigh(uint8_t pin)
{
    PORTB |= (1 << pin);
}

void GPIO_SetLow(uint8_t pin)
{
    PORTB &= ~(1 << pin);
}

void GPIO_Toggle(uint8_t pin)
{
    PORTB ^= (1 << pin);
}

uint8_t GPIO_ReadOutput(uint8_t pin)
{
    return (PORTB & (1 << pin)) ? 1 : 0;
}

uint8_t GPIO_ReadLDR(void)
{
    /* PA1: HIGH = dark (LDR resistance high), LOW = light */
    return (PINA & (1 << 1)) ? 1 : 0;
}
