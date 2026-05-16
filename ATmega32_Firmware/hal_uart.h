/*==========================================================
 * Smart Greenhouse — UART Driver for ATmega32
 * File: hal_uart.h
 * 9600 Baud @ 8MHz for HC-05 Bluetooth
 *==========================================================*/
#ifndef HAL_UART_H
#define HAL_UART_H

#include <stdint.h>

/* Initialize UART: 9600 baud, 8N1 */
void UART_Init(void);

/* Transmit a single byte */
void UART_Transmit(uint8_t data);

/* Send a null-terminated string */
void UART_SendString(const char *str);

/* Send an integer as ASCII string */
void UART_SendInt(int16_t val);

/* Check if data is available in receive buffer */
uint8_t UART_Available(void);

/* Receive a single byte (blocking) */
uint8_t UART_Receive(void);

#endif /* HAL_UART_H */
