/**
 ******************************************************************************
 * @file           : main.h
 * @brief          : Header for main.c file.
 *                   This file contains the common defines of the application.
 ******************************************************************************
 * This file is part of the PPKII software (https://github.com/cristianc1972/ppk2-net).
 * Copyright (c) 2024 Cristian Croci.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 ******************************************************************************
 */
#ifndef _MAIN_H_
#define _MAIN_H_

#ifdef __cplusplus
extern "C" {
#endif

#include "stm32l1xx_hal.h"

/* Exported functions prototypes ---------------------------------------------*/
void Error_Handler(void);


/* Private defines -----------------------------------------------------------*/
#define B1_Pin              GPIO_PIN_13
#define B1_GPIO_Port        GPIOC
#define USART_TX_Pin        GPIO_PIN_2
#define USART_TX_GPIO_Port  GPIOA
#define USART_RX_Pin        GPIO_PIN_3
#define USART_RX_GPIO_Port  GPIOA
#define LD2_Pin             GPIO_PIN_5
#define LD2_GPIO_Port       GPIOA
#define TMS_Pin             GPIO_PIN_13
#define TMS_                GPIO_Port GPIOA
#define TCK_Pin             GPIO_PIN_14
#define TCK_GPIO_Port       GPIOA
#define SWO_Pin             GPIO_PIN_3
#define SWO_GPIO_Port       GPIOB

#ifdef __cplusplus
}
#endif

#endif // _MAIN_H_
