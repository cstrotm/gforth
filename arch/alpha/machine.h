/* DEC Alpha

  Copyright (C) 1995,1996,1997,1998,2000 Free Software Foundation, Inc.

  This file is part of Gforth.

  Gforth is free software; you can redistribute it and/or
  modify it under the terms of the GNU General Public License
  as published by the Free Software Foundation; either version 2
  of the License, or (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111, USA.
*/

/* Be careful: long long on Alpha are 64 bit :-(( */

#ifndef THREADING_SCHEME
#define THREADING_SCHEME 5
#endif

#if !defined(USE_TOS) && !defined(USE_NO_TOS)
#define USE_TOS
#endif

#define FLUSH_ICACHE(addr,size)		asm("call_pal 0x86") /* imb (instruction-memory barrier) */

#include "../generic/machine.h"

#ifdef FORCE_REG
/* $9-$14 are callee-saved, $1-$8 and $22-$25 are caller-saved */
#define IPREG asm("$10")
#define SPREG asm("$11")
#define RPREG asm("$12")
#define LPREG asm("$13")
#define TOSREG asm("$14")
/* #define CFAREG asm("$22") egcs-1.0.3 crashes with any caller-saved
   register decl */
#endif /* FORCE_REG */
