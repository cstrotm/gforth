\ float wordset test suite

\ Copyright (C) 2002 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation; either version 2
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program; if not, write to the Free Software
\ Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111, USA.

decimal

\ d>f d>d

{  4503599627370497. 2dup d>f f>d d= -> true }
{ -4503599627370497. 2dup d>f f>d d= -> true }
{  9007199254740991. 2dup d>f f>d d= -> true }
{ -9007199254740991. 2dup d>f f>d d= -> true }

\ fround
{ 0.5e fround 0e f= -> true }
{ 1.5e fround 2e f= -> true }
{ 2.5e fround 2e f= -> true }
{ -0.5e fround -0e f= -> true }
{ -1.5e fround -2e f= -> true }
{ -2.5e fround -2e f= -> true }
{ 4503599627370495. d>f 0.5e f+ fround 4503599627370496. d>f f= -> true }
{ 4503599627370494. d>f 0.5e f+ fround 4503599627370494. d>f f= -> true }
{ 4503599627370497. d>f fdup fround f= -> true }
{ 9007199254740991. d>f fdup fround f= -> true }
{ -4503599627370495. d>f -0.5e f+ fround -4503599627370496. d>f f= -> true }
{ -4503599627370495. d>f  0.5e f+ fround -4503599627370494. d>f f= -> true }
{ -4503599627370497. d>f fdup fround f= -> true }
{ -9007199254740991. d>f fdup fround f= -> true }
{ 1.49999e fround 1e f= -> true }