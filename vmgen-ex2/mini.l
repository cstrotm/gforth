/* front-end scanner for vmgen example

  Copyright (C) 2001 Free Software Foundation, Inc.

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

/* %option yylineno (flex option, implied by flex -l) */

%{
#ifndef __GNUC__
#include <string.h>
char *strdup(const char *s);
#endif
%}

%%
[-();,+*&|<=!]	return yytext[0];
:=	return BECOMES;
func	return FUNC;
return	return RETURN;
end	return END;
var	return VAR;
if	return IF;
then	return THEN;
else	return ELSE;
while	return WHILE;
do	return DO;
print	return PRINT;
[0-9]+			{ yylval.num=strtol(yytext,NULL,10); return NUM; }
[a-zA-Z\_][a-zA-Z0-9\_]*	{ yylval.string=strdup(yytext); return IDENT; }
[ \t\n]	;
[/][/].*	;
.		yyerror("illegal character"); exit(1);
%%