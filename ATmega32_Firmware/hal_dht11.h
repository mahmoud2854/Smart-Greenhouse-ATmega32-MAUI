/*==========================================================
 * Smart Greenhouse — DHT11 Driver for ATmega32
 * File: hal_dht11.h
 * Bit-banging protocol on PA2
 *==========================================================*/
#ifndef HAL_DHT11_H
#define HAL_DHT11_H

#include <stdint.h>

/* DHT11 data structure */
typedef struct {
    uint8_t temperature;   /* Integer part only (0-50°C) */
    uint8_t humidity;      /* Integer part only (20-90%) */
    uint8_t valid;         /* 1 = checksum OK, 0 = error */
} DHT11_Data;

/* Read temperature and humidity from DHT11 on PA2
 * Returns: DHT11_Data with valid flag
 * Note: This is a blocking call (~25ms) */
DHT11_Data DHT11_Read(void);

#endif /* HAL_DHT11_H */
