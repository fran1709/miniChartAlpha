lexer grammar MiniCSharpScanner;

//Identificadores
LIST            : (INT_ID | STRING_ID | CHAR_ID | BOOL_ID | DOUBLE_ID | IDENTIFIER) LEFTSBRACK (IDENTIFIER | INT)? RIGHTSBRACK;
INT_ID          : 'int';
STRING_ID       : 'string';
CHAR_ID         : 'char';
BOOL_ID         : 'bool';
DOUBLE_ID       : 'double';

//Palabras reservadas
CLASS           : 'class';
USING           : 'using';
VOID            : 'void';
IF              : 'if';
ELSE            : 'else';
WHILE           : 'while';
FOR             : 'for';
BREAK           : 'break';
RETURN          : 'return';
READ            : 'read';
WRITE           : 'write';
NEW             : 'new';

/*Constantes*/
BOOL            : 'true' | 'false';

//Constantes númericas
INT            : DIGIT+;
DOUBLE         : [0-9]+ ('.' [0-9]*)? (('e'|'E') ('+'|'-')? [0-9]+)? ;

//Constantes carácter
STRINGCONST         : '"' (~["\\\r\n] | '\\' .)* '"' ;
CHARCONST       : '\'' (EscapeSequence | ~('\''|'\\')) '\'' ;
EscapeSequence  : '\\' [\\'"nrtbf] ;

/*Operadores*/
ASSIGN          : '=';
AND             : '&&';
OR              : '||';
//Addop
PLUS            : '+';
MINUS           : '-';
PLUSPLUS        : '++';
MINUSMINUS      : '--';

//Mulop
MULT            : '*';
DIV             : '/';
MOD             : '%';

//Relop
EQUALS          : '==';
NOTEQUALS       : '!=';
LESSTHAN        : '<';
GREATERTHAN     : '>';
LESSOREQUALS    : '<=';
GREATOREQUALS  : '>=';

//Simbolos
DOT             : '.';
SEMICOLON       : ';';
COMMA           : ',';
LEFTPAREN       : '(';
RIGHTPAREN      : ')';
LEFTBRACK       : '{';
RIGHTBRACK      : '}';
LEFTSBRACK      : '[';
RIGHTSBRACK     : ']';

//Clases léxicas
fragment LETTER : [a-zA-Z_];
fragment DIGIT  : [0-9];
IDENTIFIER      : LETTER (LETTER | DIGIT)*;

//Skip
COMMENT         : '//' ~[\r\n]* -> skip;
BLOCKCOMMENT    : '/*' ~[\r\n]* '*/'-> skip;
WS              : [ \t\n\r]+ -> skip;