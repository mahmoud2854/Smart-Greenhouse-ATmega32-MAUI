/*==========================================================
 * Smart Greenhouse — UART Driver for ATmega32
 * File: hal_uart.c
 * Note: ATmega32 uses UCSRA/B/C (no '0' suffix)
 *       UCSRC shares address with UBRRH — URSEL bit required
 *==========================================================*/
#include "hal_uart.h"
#include <avr/io.h>

#define BAUD      9600UL
#define UBRR_VAL  ((F_CPU / (16UL * BAUD)) - 1)   /* = 51 for 8MHz */

void UART_Init(void)
{
    /* Set baud rate (UBRRH bit7=0 to select UBRRH, not UCSRC) */
    UBRRH = (uint8_t)(UBRR_VAL >> 8) & 0x7F;
    UBRRL = (uint8_t)(UBRR_VAL);

    /* Enable transmitter and receiver */
    UCSRB = (1 << TXEN) | (1 << RXEN);

    /* Frame format: 8 data bits, 1 stop bit, no parity
     * URSEL=1 required to write to UCSRC (shared address with UBRRH) */
    UCSRC = (1 << URSEL) | (1 << UCSZ1) | (1 << UCSZ0);
}

void UART_Transmit(uint8_t data)
{
    /* Wait for empty transmit buffer */
    while (!(UCSRA & (1 << UDRE)));
    UDR = data;
}

void UART_SendString(const char *str)
{
    while (*str) {
        UART_Transmit((uint8_t)*str++);
    }
}

void UART_SendInt(int16_t val)
{
    char buf[7];
    int8_t i = 0;

    if (val < 0) {
        UART_Transmit('-');
        val = -val;
    }

    if (val == 0) {
        UART_Transmit('0');
        return;
    }

    while (val > 0 && i < 6) {
        buf[i++] = '0' + (val % 10);
        val /= 10;
    }

    /* Send digits in reverse order */
    while (i > 0) {
        UART_Transmit((uint8_t)buf[--i]);
    }
}

uint8_t UART_Available(void)
{
    return (UCSRA & (1 << RXC)) ? 1 : 0;
}

uint8_t UART_Receive(void)
{
    while (!(UCSRA & (1 << RXC)));
    return UDR;
}
