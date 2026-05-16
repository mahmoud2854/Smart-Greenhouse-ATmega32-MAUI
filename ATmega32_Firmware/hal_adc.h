/*==========================================================
 * Smart Greenhouse — ADC Driver for ATmega32
 * File: hal_adc.h
 * Reads analog sensor on PA0 (Soil Moisture)
 *==========================================================*/
#ifndef HAL_ADC_H
#define HAL_ADC_H

#include <stdint.h>

/* Initialize ADC: AVCC reference, prescaler /64 (125kHz @ 8MHz) */
void ADC_Init(void);

/* Read 10-bit value from ADC channel (0-7 for PA0-PA7) */
uint16_t ADC_Read(uint8_t channel);

/* Read soil moisture as percentage (0-100%) */
uint8_t ADC_ReadSoilPercent(void);

#endif /* HAL_ADC_H */
