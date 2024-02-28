/**
 ******************************************************************************
 * @file    stm32l1xx_it.h
 * @brief   This file contains the headers of the interrupt handlers.
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
#ifndef _STM32L1xx_IT_H_
#define _STM32L1xx_IT_H_

#ifdef __cplusplus
extern "C" {
#endif

void NMI_Handler(void);
void HardFault_Handler(void);
void MemManage_Handler(void);
void BusFault_Handler(void);
void UsageFault_Handler(void);
void SVC_Handler(void);
void DebugMon_Handler(void);
void PendSV_Handler(void);
void SysTick_Handler(void);

#ifdef __cplusplus
}
#endif

#endif // _STM32L1xx_IT_H_
