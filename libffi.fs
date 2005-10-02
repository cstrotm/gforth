\ libffi.fs	shared library support package 		14aug05py

\ Copyright (C) 1995,1996,1997,1998,2000,2003,2005 Free Software Foundation, Inc.

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

\ common stuff, same as fflib.fs

Variable libs 0 libs !
\ links between libraries
Variable thisproc
Variable thislib

Variable revdec  revdec off
\ turn revdec on to compile bigFORTH libraries
Variable revarg  revarg off
\ turn revarg on to compile declarations with reverse arguments
Variable legacy  legacy off
\ turn legacy on to compile bigFORTH legacy libraries

Vocabulary c-decl
Vocabulary cb-decl

: @lib ( lib -- )
    \G obtains library handle
    cell+ dup 2 cells + count open-lib
    dup 0= abort" Library not found" swap ! ;

: @proc ( lib addr -- )
    \G obtains symbol address
    cell+ tuck cell+ @ count rot cell+ @
    lib-sym  dup 0= abort" Proc not found!" swap ! ;

: proc, ( lib -- )
\G allocates and initializes proc stub
\G stub format:
\G    linked list in library
\G    address of proc
\G    ptr to OS name of symbol as counted string
\G    threaded code for invocation
    here dup thisproc !
    swap 2 cells + dup @ A, !
    0 , 0 A, ;

Defer legacy-proc  ' noop IS legacy-proc

: proc:  ( lib "name" -- )
\G Creates a named proc stub
    Create proc, 0 also c-decl
    legacy @ IF  legacy-proc  THEN
DOES> ( x1 .. xn -- r )
    3 cells + >r ;

: library ( "name" "file" -- )
\G loads library "file" and creates a proc defining word "name"
\G library format:
\G    linked list of libraries
\G    library handle
\G    linked list of library's procs
\G    OS name of library as counted string
    Create  here libs @ A, dup libs !
    0 , 0 A, parse-name string, @lib
DOES> ( -- )  dup thislib ! proc: ;

: init-shared-libs ( -- )
    defers 'cold  libs
    0  libs  BEGIN  @ dup  WHILE  dup  REPEAT  drop
    BEGIN  dup  WHILE  >r
	r@ @lib
	r@ 2 cells +  BEGIN  @ dup  WHILE  r@ over @proc  REPEAT
	drop rdrop
    REPEAT  drop ;

' init-shared-libs IS 'cold

: symbol, ( "c-symbol" -- )
    here thisproc @ 2 cells + ! parse-name s,
    thislib @ thisproc @ @proc ;

\ stuff for libffi

\ libffi uses a parameter array for the input

$20 Value maxargs

Create retbuf 2 cells allot
Create argbuf maxargs 2* cells allot
Create argptr maxargs 0 [DO]  argbuf [I] 2* cells + A, [LOOP]

\ "forward" when revarg is on

\ : >c+  ( char buf -- buf' )  tuck   c!    cell+ cell+ ;
: >i+  ( n buf -- buf' )     tuck    !    cell+ cell+ ;
: >p+  ( addr buf -- buf' )  tuck    !    cell+ cell+ ;
: >d+  ( d buf -- buf' )     dup >r ffi-2! r> cell+ cell+ ;
: >sf+ ( r buf -- buf' )     dup   sf!    cell+ cell+ ;
: >df+ ( r buf -- buf' )     dup   df!    cell+ cell+ ;

\ "backward" when revarg is off

\ : >c-  ( char buf -- buf' )  tuck   c!    2 cells - ;
: >i-  ( n buf -- buf' )     2 cells - tuck    ! ;
: >p-  ( addr buf -- buf' )  2 cells - tuck    ! ;
: >d-  ( d buf -- buf' )     2 cells - dup >r ffi-2! r> ;
: >sf- ( r buf -- buf' )     2 cells - dup   sf! ;
: >df- ( r buf -- buf' )     2 cells - dup   df! ;

\ return value

\ : c>   ( -- c )  retbuf c@ ;
: i>x   ( -- n )  retbuf @ ;
: p>x   ( -- addr ) retbuf @ ;
: d>x   ( -- d )  retbuf ffi-2@ ;
: sf>x  ( -- r )  retbuf sf@ ;
: df>x  ( -- r )  retbuf df@ ;

wordlist constant cifs

Variable cifbuf $40 allot \ maximum: 64 parameters
cifbuf cell+ cifbuf !
Variable args args off

: argtype ( bkxt fwxt type "name" -- )
    Create , , , DOES>  1 args +! ;

: arg@ ( arg -- type pushxt )
    dup @ swap cell+
    revarg @ IF  cell+  THEN  @    ;

: arg, ( xt -- )
    dup ['] noop = IF  drop  EXIT  THEN  compile, ;

: start, ( n -- )  cifbuf cell+ cifbuf !
    revarg @ IF  drop 0  ELSE  2* cells  THEN  argbuf +
    postpone Literal ;

: ffi-call, ( -- lit-cif )
    postpone drop postpone argptr postpone retbuf
    thisproc @ cell+ postpone literal postpone @
    0 postpone literal here cell -
    postpone ffi-call ;

: cif, ( n -- )
    cifbuf @ c! 1 cifbuf +! ;

: cif@ ( -- addr u )
    cifbuf cell+ cifbuf @ over - ;

: make-cif ( rtype -- addr ) cif,
    cif@ cifs search-wordlist
    IF  execute  EXIT  THEN
    get-current >r cifs set-current
    cif@ nextname Create  here >r
    cif@ 1- bounds ?DO  I c@ ffi-type ,  LOOP
    r> cif@ 1- tuck + c@ ffi-type here dup >r 0 ffi-size allot
    ffi-prep-cif throw
    r> r> set-current ;

: decl, ( 0 arg1 .. argn call rtype start -- )
    start, { retxt rtype }
    revdec @ IF  0 >r
	BEGIN  dup  WHILE  >r  REPEAT
	BEGIN  r> dup  WHILE  arg@ arg,  REPEAT
	ffi-call, retxt compile,  postpone  EXIT
	BEGIN  dup  WHILE  cif,  REPEAT drop
    ELSE  0 >r
	BEGIN  dup  WHILE  arg@ arg, >r REPEAT drop
	ffi-call, retxt compile,  postpone  EXIT
	BEGIN  r> dup  WHILE  cif,  REPEAT  drop
    THEN  rtype make-cif swap ! here thisproc @ 2 cells + ! ;

: rettype ( endxt n "name" -- )
    Create 2,
  DOES>  2@ args @ decl, symbol, previous revarg off args off ;

also c-decl definitions

: <rev>  revarg on ;

' >i+  ' >i-    6 argtype int
' >p+  ' >p-  &12 argtype ptr
' >d+  ' >d-    8 argtype llong
' >sf+ ' >sf-   9 argtype sf
' >df+ ' >df- &10 argtype df

' noop   0 rettype (void)
' i>x    6 rettype (int)
' p>x  &12 rettype (ptr)
' d>x    8 rettype (llong)
' sf>x   9 rettype (sf)
' df>x &10 rettype (fp)

previous definitions

\ legacy support for old library interfaces
\ interface to old vararg stuff not implemented yet

also c-decl

:noname ( n 0 -- 0 int1 .. intn )
    legacy @ 0< revarg !
    swap 0 ?DO  int  LOOP  (int)
; IS legacy-proc

: (int) ( n -- )
    >r ' execute r> 0 ?DO  int  LOOP  (int) ;
: (void) ( n -- )
    >r ' execute r> 0 ?DO  int  LOOP  (void) ;
: (float) ( n -- )
    >r ' execute r> 0 ?DO  df   LOOP  (fp) ;

previous

\ callback stuff

0 [IF]
Variable callbacks
\G link between callbacks

: callback ( -- )
    Create  0 ] postpone >r also cb-decl
  DOES>
    Create here >r 0 , callbacks @ A, r@ callbacks !
    swap postpone Literal postpone call , postpone EXIT
    r> dup cell+ cell+ alloc-callback swap !
  DOES> @ ;

: callback; ( 0 xt1 .. xtn -- )
    BEGIN  over  WHILE  compile,  REPEAT
    postpone r> postpone execute compile, drop
    postpone EXIT postpone [ previous ; immediate

: va-ret ( xt xt -- )
    Create A, A, immediate
  DOES> 2@ compile, ;

: init-callbacks ( -- )
    defers 'cold  callbacks cell -
    BEGIN  cell+ @ dup  WHILE  dup cell+ cell+ alloc-callback over !
    REPEAT  drop ;

' init-callbacks IS 'cold

also cb-decl definitions

\ arguments

' va-arg-int      Alias int
' va-arg-float    Alias sf
' va-arg-double   Alias df
' va-arg-longlong Alias llong
' va-arg-ptr      Alias ptr

' va-return-void     ' va-start-void     va-ret (void)
' va-return-int      ' va-start-int      va-ret (int)
' va-return-float    ' va-start-float    va-ret (sf)
' va-return-double   ' va-start-double   va-ret (fp)
' va-return-longlong ' va-start-longlong va-ret (llong)
' va-return-ptr      ' va-start-ptr      va-ret (ptr)

previous definitions

[THEN]
    
\ testing stuff

[ifdef] testing

library libc libc.so.6
                
libc sleep int (int) sleep
libc open  int int ptr (int) open
libc lseek int llong int (llong) lseek64
libc read  int ptr int (int) read
libc close int (int) close

library libm libm.so.6

libm fmodf sf sf (sf) fmodf
libm fmod  df df (fp) fmod

\ example for a windows callback
    
callback wincall (int) int int int int callback;

:noname ( a b c d -- e )  2drop 2drop 0 ; wincall do_timer

\ test a callback

callback 2:1 (int) int int callback;

: cb-test ( a b -- c )
    cr ." Testing callback"
    cr ." arguments: " .s
    cr ." result " + .s cr ;
' cb-test 2:1 c_plus

: test  c_plus av-start-int >r >r av-int-r av-int-r av-call-int ;

\ 3 4 test

\ bigFORTH legacy library test

library libX11 libX11.so.6

legacy on

1 libX11 XOpenDisplay XOpenDisplay    ( name -- dpy )
5 libX11 XInternAtoms XInternAtoms    ( atoms flag count names dpy -- status )

legacy off

[then]    