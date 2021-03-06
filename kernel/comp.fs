\ compiler definitions						14sep97jaw

\ Copyright (C) 1995,1996,1997,1998,2000,2003,2004,2005,2006,2007,2008,2009,2010,2011,2012,2013 Free Software Foundation, Inc.

\ This file is part of Gforth.

\ Gforth is free software; you can redistribute it and/or
\ modify it under the terms of the GNU General Public License
\ as published by the Free Software Foundation, either version 3
\ of the License, or (at your option) any later version.

\ This program is distributed in the hope that it will be useful,
\ but WITHOUT ANY WARRANTY; without even the implied warranty of
\ MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
\ GNU General Public License for more details.

\ You should have received a copy of the GNU General Public License
\ along with this program. If not, see http://www.gnu.org/licenses/.

\ \ Revisions-Log

\	put in seperate file				14sep97jaw	

\ \ here allot , c, A,						17dec92py

[IFUNDEF] allot
[IFUNDEF] forthstart
: allot ( n -- ) \ core
    dup unused u> -8 and throw
    dp +! ;
[THEN]
[THEN]

\ we default to this version if we have nothing else 05May99jaw
[IFUNDEF] allot
: allot ( n -- ) \ core
    \G Reserve @i{n} address units of data space without
    \G initialization. @i{n} is a signed number, passing a negative
    \G @i{n} releases memory.  In ANS Forth you can only deallocate
    \G memory from the current contiguous region in this way.  In
    \G Gforth you can deallocate anything in this way but named words.
    \G The system does not check this restriction.
    here +
    dup 1- usable-dictionary-end forthstart within -8 and throw
    dp ! ;
[THEN]

: c,    ( c -- ) \ core c-comma
    \G Reserve data space for one char and store @i{c} in the space.
    here 1 chars allot c! ;

: ,     ( w -- ) \ core comma
    \G Reserve data space for one cell and store @i{w} in the space.
    here cell allot ! ;

: 2,	( w1 w2 -- ) \ gforth
    \G Reserve data space for two cells and store the double @i{w1
    \G w2} there, @i{w2} first (lower address).
    here 2 cells allot 2! ;

\ : aligned ( addr -- addr' ) \ core
\     [ cell 1- ] Literal + [ -1 cells ] Literal and ;

: >align ( addr a-addr -- ) \ gforth
    \G add enough spaces to reach a-addr
    swap ?DO  bl c,  LOOP ;

: align ( -- ) \ core
    \G If the data-space pointer is not aligned, reserve enough space to align it.
    here dup aligned >align ;

\ : faligned ( addr -- f-addr ) \ float f-aligned
\     [ 1 floats 1- ] Literal + [ -1 floats ] Literal and ; 

: falign ( -- ) \ float f-align
    \G If the data-space pointer is not float-aligned, reserve
    \G enough space to align it.
    here dup faligned >align ;

: maxalign ( -- ) \ gforth
    \G Align data-space pointer for all alignment requirements.
    here dup maxaligned >align ;

\ the code field is aligned if its body is maxaligned
' maxalign Alias cfalign ( -- ) \ gforth
\G Align data-space pointer for code field requirements (i.e., such
\G that the corresponding body is maxaligned).

' , alias A, ( addr -- ) \ gforth

' NOOP ALIAS const

\ \ Header							23feb93py

\ input-stream, nextname and noname are quite ugly (passing
\ information through global variables), but they are useful for dealing
\ with existing/independent defining words

: string, ( c-addr u -- ) \ gforth
    \G puts down string as cstring
    dup c, here swap chars dup allot move ;

: longstring, ( c-addr u -- ) \ gforth
    \G puts down string as longcstring
    dup , here swap chars dup allot move ;

: nlstring, ( c-addr u -- ) \ gforth
    \G puts down string as longcstring
    tuck here swap chars dup allot move , ;


[IFDEF] prelude-mask
variable next-prelude

: prelude, ( -- )
    next-prelude @ if
	align next-prelude @ ,
    then ;
[THEN]

: header, ( c-addr u -- ) \ gforth
    name-too-long?  vt,
    dup max-name-length @ max max-name-length !
    [ [IFDEF] prelude-mask ] prelude, [ [THEN] ]
    dup here + 3 cells + dup maxaligned >align
    nlstring,
    current @ 1 or A, 0 A, here last !  \ link field; before revealing, it contains the
    \ tagged reveal-into wordlist
    alias-mask lastflags cset
    next-prelude @ 0<> prelude-mask and lastflags cset
    next-prelude off
    cfalign ;

defer record-name ( -- ) ' noop is record-name
\ record next name in tags file
defer (header)
defer header ( -- ) \ gforth
' (header) IS header

: input-stream-header ( "name" -- )
    parse-name name-too-short? header, ;

: input-stream ( -- )  \ general
    \G switches back to getting the name from the input stream ;
    ['] input-stream-header IS (header) ;

' input-stream-header IS (header)

2variable nextname-string

: nextname-header ( -- )
    nextname-string 2@ header,
    nextname-string free-mem-var
    input-stream ;

\ the next name is given in the string

: nextname ( c-addr u -- ) \ gforth
    \g The next defined word will have the name @var{c-addr u}; the
    \g defining word will leave the input stream alone.
    name-too-long?
    nextname-string free-mem-var
    save-mem nextname-string 2!
    ['] nextname-header IS (header) ;

: noname, ( -- )
    0 last ! vt,  here 3 cells + dup maxaligned >align alias-mask , 0 , ;
: noname-header ( -- )
    noname, input-stream ;

: noname ( -- ) \ gforth
    \g The next defined word will be anonymous. The defining word will
    \g leave the input stream alone. The xt of the defined word will
    \g be given by @code{latestxt}.
    ['] noname-header IS (header) ;

: latestxt ( -- xt ) \ gforth
    \G @i{xt} is the execution token of the last word defined.
    \ The main purpose of this word is to get the xt of words defined using noname
    lastcfa @ ;

' latestxt alias lastxt \ gforth-obsolete
\G old name for @code{latestxt}.

: latest ( -- nt ) \ gforth
\G @var{nt} is the name token of the last word defined; it is 0 if the
\G last word has no name.
    last @ ;

\ \ literals							17dec92py

: Literal  ( compilation n -- ; run-time -- n ) \ core
    \G Compilation semantics: compile the run-time semantics.@*
    \G Run-time Semantics: push @i{n}.@*
    \G Interpretation semantics: undefined.
    postpone lit , ; immediate restrict

: 2Literal ( compilation w1 w2 -- ; run-time  -- w1 w2 ) \ double two-literal
    \G Compile appropriate code such that, at run-time, @i{w1 w2} are
    \G placed on the stack. Interpretation semantics are undefined.
    swap postpone Literal  postpone Literal ; immediate restrict

: ALiteral ( compilation addr -- ; run-time -- addr ) \ gforth
    postpone lit A, ; immediate restrict

Defer char@ ( addr u -- char addr' u' )
:noname  over c@ -rot 1 /string ; IS char@

: char   ( '<spaces>ccc' -- c ) \ core
    \G Skip leading spaces. Parse the string @i{ccc} and return @i{c}, the
    \G display code representing the first character of @i{ccc}.
    parse-name char@ 2drop ;

: [char] ( compilation '<spaces>ccc' -- ; run-time -- c ) \ core bracket-char
    \G Compilation: skip leading spaces. Parse the string
    \G @i{ccc}. Run-time: return @i{c}, the display code
    \G representing the first character of @i{ccc}.  Interpretation
    \G semantics for this word are undefined.
    char postpone Literal ; immediate restrict

\ \ threading							17mar93py

' noop Alias recurse
\g Call the current definition.
unlock tlastcfa @ lock AConstant lastcfa
\ this is the alias pointer in the recurse header, named lastcfa.
\ changing lastcfa now changes where recurse aliases to
\ it's always an alias of the current definition
\ it won't work in a flash/rom environment, therefore for Gforth EC
\ we stick to the traditional implementation

: cfa,     ( code-address -- )  \ gforth	cfa-comma
    here
    dup lastcfa !
    0 A, 0 ,
    code-address! ;

defer basic-block-end ( -- )

:noname ( -- )
    0 compile-prim1 ;
is basic-block-end

has? primcentric [IF]
    has? peephole [IF]
	\ dynamic only    
	: peephole-compile, ( xt -- )
	    \ compile xt, appending its code to the current dynamic superinstruction
	    here swap , compile-prim1 ;
    [ELSE]
	: peephole-compile, ( xt -- addr ) @ , ;
    [THEN]

    : compile, ( xt -- ) x#exec [ 1 , ] ;
[ELSE]
' , is compile,
[THEN]

\ \ ticks

: default-name>comp ( nt -- w xt ) \ gforth name-to-comp
    \G @i{w xt} is the compilation token for the word @i{nt}.
    (name>x) (x>comp)
    1 = if
        ['] execute
    else
        ['] compile,
    then ;

: [(')]  ( compilation "name" -- ; run-time -- nt ) \ gforth bracket-paren-tick
    (') postpone ALiteral ; immediate restrict

: [']  ( compilation. "name" -- ; run-time. -- xt ) \ core      bracket-tick
    \g @i{xt} represents @i{name}'s interpretation
    \g semantics. Perform @code{-14 throw} if the word has no
    \g interpretation semantics.
    ' postpone ALiteral ; immediate restrict

: COMP'    ( "name" -- w xt ) \ gforth  comp-tick
    \g Compilation token @i{w xt} represents @i{name}'s compilation semantics.
    (') name>comp ;

: [COMP']  ( compilation "name" -- ; run-time -- w xt ) \ gforth bracket-comp-tick
    \g Compilation token @i{w xt} represents @i{name}'s compilation semantics.
    COMP' swap POSTPONE Aliteral POSTPONE ALiteral ; immediate restrict

: postpone, ( w xt -- ) \ gforth	postpone-comma
    \g Compile the compilation semantics represented by the
    \g compilation token @i{w xt}.
    dup ['] execute =
    if
	drop compile,
    else
	swap POSTPONE aliteral compile,
    then ;

include ./recognizer.fs

\ \ Strings							22feb93py

: S, ( addr u -- )
    \ allot string as counted string
    here over char+ allot  place align ;

: mem, ( addr u -- )
    \ allot the memory block HERE (do alignment yourself)
    here over allot swap move ;

: ," ( "string"<"> -- )
    [char] " parse s, ;

\ \ Header states						23feb93py

\ problematic only for big endian machines

: cset ( bmask c-addr -- )
    tuck @ or swap ! ; 

: creset ( bmask c-addr -- )
    tuck @ swap invert and swap ! ; 

: ctoggle ( bmask c-addr -- )
    tuck @ xor swap ! ; 

: lastflags ( -- c-addr )
    \ the address of the flags byte in the last header
    \ aborts if the last defined word was headerless
    latest dup 0= abort" last word was headerless"
    >f+c ;

: immediate ( -- ) \ core
    \G Make the compilation semantics of a word be to @code{execute}
    \G the execution semantics.
    immediate-mask lastflags cset ;

: restrict ( -- ) \ gforth
    \G A synonym for @code{compile-only}
    restrict-mask lastflags cset ;

' restrict alias compile-only ( -- ) \ gforth
\G Remove the interpretation semantics of a word.

\ \ Create Variable User Constant                        	17mar93py

: Alias    ( xt "name" -- ) \ gforth
    Header reveal ['] on vtcopy
    alias-mask lastflags creset
    dup A, lastcfa ! ;

: s>int ( nt -- xt )  @ name>int ;
: s>comp ( nt -- xt1 xt2 )  @ name>comp ;

: Synonym ( "name" "oldname" -- ) \ Forth200x
    Header  ['] on vtcopy
    alias-mask lastflags creset
    parse-name find-name dup A, name>int lastcfa !
    ['] s>int set->int ['] s>comp set->comp reveal ;

: Create ( "name" -- ) \ core
    Header reveal dovar, ;

: buffer: ( u "name" -- ) \ core ext
    Create allot ;

: Variable ( "name" -- ) \ core
    Create 0 , ;

: AVariable ( "name" -- ) \ gforth
    Create 0 A, ;

: 2Variable ( "name" -- ) \ double two-variable
    Create 0 , 0 , ;

: uallot ( n -- ) \ gforth
    udp @ swap udp +! ;

: User ( "name" -- ) \ gforth
    Header reveal douser, cell uallot , ;

: AUser ( "name" -- ) \ gforth
    User ;

: (Constant)  Header reveal docon, ;

: (Value)  Header reveal dovalue, ;

: Constant ( w "name" -- ) \ core
    \G Define a constant @i{name} with value @i{w}.
    \G  
    \G @i{name} execution: @i{-- w}
    (Constant) , ;

: AConstant ( addr "name" -- ) \ gforth
    (Constant) A, ;

: Value ( w "name" -- ) \ core-ext
    (Value) , ;

: AValue ( w "name" -- ) \ core-ext
    (Value) A, ;

: u-to >body @ next-task + ! ;
comp: drop >body @ postpone useraddr , postpone ! ;
: u-compile, ( xt -- )  >body @ postpone useraddr , postpone @ ;

: UValue ( "name" -- )
    \G Define a per-thread value
    Create cell uallot , ['] u-to set-to
    ['] u-compile, set-compiler
  DOES> @ next-task + @ ;

: 2Constant ( w1 w2 "name" -- ) \ double two-constant
    Create ( w1 w2 "name" -- )
        2,
    DOES> ( -- w1 w2 )
        2@ ;

: (Field)  Header reveal dofield, ;

\ IS Defer What's Defers TO                            24feb93py

defer defer-default ( -- )
' abort is defer-default
\ default action for deferred words (overridden by a warning later)
    
: Defer ( "name" -- ) \ gforth
\G Define a deferred word @i{name}; its execution semantics can be
\G set with @code{defer!} or @code{is} (and they have to, before first
\G executing @i{name}.
    Header Reveal dodefer,
    ['] defer-default A, ;

: defer@ ( xt-deferred -- xt ) \ gforth defer-fetch
    \G @i{xt} represents the word currently associated with the deferred
    \G word @i{xt-deferred}.
    x#exec [ 7 , ] ;

: >body@ >body @ ;

: Defers ( compilation "name" -- ; run-time ... -- ... ) \ gforth
    \G Compiles the present contents of the deferred word @i{name}
    \G into the current definition.  I.e., this produces static
    \G binding as if @i{name} was not deferred.
    ' defer@ compile, ; immediate

: does>-like ( xt -- defstart )
    \ xt ( addr -- ) is !does or !;abi-code etc, addr is the address
    \ that should be stored right after the code address.
    >r ;-hook ?struc
    exit-like
    here [ has? peephole [IF] ] 5 [ [ELSE] ] 4 [ [THEN] ] cells +
    postpone aliteral r> compile, [compile] exit
    [ has? peephole [IF] ] finish-code [ [THEN] ]
    defstart ;

\ : !does    ( addr -- ) \ gforth	store-does
\     ['] spaces >namevt @ >vtcompile, @ set-compiler
\     latestxt does-code! ;

extra>-dummy (doextra-dummy)
: !extra   ( addr -- ) \ gforth store-extra
    vttemplate >vtcompile, @ ['] udp >namevt @ >vtcompile, @ =
    IF
	['] extra, set-compiler
    THEN
    latestxt extra-code! ;

: DOES>  ( compilation colon-sys1 -- colon-sys2 ; run-time nest-sys -- ) \ core        extra
    cfalign 0 , here !extra ] defstart :-hook ;
comp: drop  ['] !extra does>-like :-hook ;

\ comp: to define compile, action

Create vttemplate
0 A,                   \ link field
' peephole-compile, A, \ compile, field
' post, A,             \ post, field
0 A,                   \ extra field
' no-to A,             \ to field
' default-name>int A,  \ name>int field
' default-name>comp A, \ name>comp field
' >body@ A,            \ defer@

\ initialize to one known vt

: vtcopy ( xt -- ) \ gforth vtcopy
    vttemplate here >namevt !
    >namevt @ vttemplate vtsize move
    here >namevt vttemplate ! ;

: vtcopy,     ( xt -- )  \ gforth	vtcopy-comma
    dup vtcopy >code-address cfa, ;

: vt= ( vt1 vt2 -- flag )
    cell+ swap vtsize cell /string tuck compare 0= ;

: (vt,) ( -- )
    align  here vtsize allot vttemplate over vtsize move
    vtable-list @ over !  dup vtable-list !
    vttemplate @ !  vttemplate off ;

: vt, ( -- )  vttemplate @ 0= IF EXIT THEN
    vtable-list
    BEGIN  @ dup  WHILE
	    dup vttemplate vt= IF  vttemplate @ !  vttemplate off  EXIT  THEN
    REPEAT  drop (vt,) ;

: !namevt ( addr -- )  latestxt >namevt ! ;

: start-xt ( -- colonsys xt ) \ incomplete, will not be a full xt
    here >r docol: cfa, defstart ] :-hook r> ;
: start-xt-like ( colonsys xt -- colonsys )
    nip reveal does>-like drop start-xt drop ;

: set-compiler  ( xt -- ) vttemplate >vtcompile, ! ;
: set-postpone  ( xt -- ) vttemplate >vtpostpone ! ;
: set-to        ( xt -- ) vttemplate >vtto ! ;
: set-defer@    ( xt -- ) vttemplate >vtdefer@ ! ;
: set->int      ( xt -- ) vttemplate >vt>int ! ;
: set->comp     ( xt -- ) vttemplate >vt>comp ! ;
: set-does>     ( xt -- ) >body !extra ; \ more work than the aboves

: comp: ( -- colon-sys )
    start-xt  set-compiler ;
comp: ['] set-compiler start-xt-like ;  ( compilation colon-sys1 -- colon-sys2 ; run-time nest-sys -- ) \ gforth        compile-to

: post: ( -- colon-sys )
    start-xt  set-postpone ;
comp: ['] set-postpone     start-xt-like ;  ( compilation colon-sys1 -- colon-sys2 ; run-time nest-sys -- ) \ gforth        lit-to

\ defer and friends

: defer! ( xt xt-deferred -- ) \ gforth  defer-store
\G Changes the @code{defer}red word @var{xt-deferred} to execute @var{xt}.
    >body ! ;

: value! ( xt xt-deferred -- ) \ gforth  value-store
    >body ! ;
comp: drop >body postpone ALiteral postpone ! ;
    
: <IS> ( "name" xt -- ) \ gforth
    \g Changes the @code{defer}red word @var{name} to execute @var{xt}.
    record-name ' defer! ;

: [IS] ( compilation "name" -- ; run-time xt -- ) \ gforth bracket-is
    \g At run-time, changes the @code{defer}red word @var{name} to
    \g execute @var{xt}.
    record-name ' postpone ALiteral postpone defer! ; immediate restrict

: (int-to) ( val xt -- ) x#exec [ 4 , ] ;
: (comp-to) ( xt -- ) dup >namevt @ >vtto @ compile, ;

:noname ( value "name" -- ) (') (name>x) drop (int-to) ;
:noname ( value "name" -- ) (') (name>x) drop (comp-to) ; over over
interpret/compile: TO ( value "name" -- )
interpret/compile: IS ( value "name" -- )

\ \ : ;                                                  	24feb93py

defer :-hook ( sys1 -- sys2 )
defer free-old-local-names ( -- )
defer ;-hook ( sys2 -- sys1 )

0 Constant defstart

: (noname->comp) ( nt -- nt xt )  ['] compile, ;
: (:noname) ( -- colon-sys )
    \ common factor of : and :noname
    docol, defstart ] :-hook ;

: : ( "name" -- colon-sys ) \ core	colon
    free-old-local-names
    Header (:noname) ;

: :noname ( -- xt colon-sys ) \ core-ext	colon-no-name
    noname, here (:noname)
    ['] noop set->int  ['] (noname->comp) set->comp ;

: ; ( compilation colon-sys -- ; run-time nest-sys ) \ core	semicolon
    ;-hook ?struc [compile] exit
    [ has? peephole [IF] ] finish-code [ [THEN] ]
    reveal postpone [ ; immediate restrict

: recognizer: ( int-xt comp-xt post-xt "name" -- )
    \G create a new recognizer table
    >r >r >r :noname r> compile, postpone ;
    r> set-compiler r> set-postpone  Constant ;

\ new interpret/compile:

: interpret/compile? ( xt -- flag ) drop false ;

: i/c>int ( nt -- xt )  @ ;
: i/c>comp ( nt -- xt1 xt2 ) cell+ @ ['] execute ;

: interpret/compile: ( interp-xt comp-xt "name" -- ) \ gforth
    Header reveal ['] on vtcopy alias-mask lastflags cset
    ['] i/c>int set->int  ['] i/c>comp set->comp
    swap dup A, lastcfa ! A, ;

\ \ Search list handling: reveal words, recursive		23feb93py

: last?   ( -- false / nfa nfa )
    latest ?dup ;

Variable warnings ( -- addr ) \ gforth
G -1 warnings T !

: (reveal) ( nt wid -- )
    wordlist-id dup >r
    @ over >link ! 
    r> ! ;

\ make entry in wordlist-map
' (reveal) f83search reveal-method !

: check-shadow  ( addr count wid -- )
    \G prints a warning if the string is already present in the wordlist
    >r 2dup r> (search-wordlist) warnings @ and ?dup if
	<<#
	name>string 2over 2over str= 0=
	IF  2over holds s"  with " holds  THEN
	holds s" redefined " holds
	0. #> hold 1- c(warning") #>>
    then
    2drop ;

: reveal ( -- ) \ gforth
    last?
    if \ the last word has a header
	dup >link @ 1 and
	if \ it is still hidden
	    dup >link @ 1 xor		( nt wid )
	    2dup >r name>string r> check-shadow ( nt wid )
	    dup wordlist-map @ reveal-method perform
	else
	    drop
	then
    then ;

: rehash  ( wid -- )
    dup wordlist-map @ rehash-method perform ;

' reveal alias recursive ( compilation -- ; run-time -- ) \ gforth
\g Make the current definition visible, enabling it to call itself
\g recursively.
	immediate restrict
