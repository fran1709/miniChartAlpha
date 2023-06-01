parser grammar MiniCSharpParser;

options {
  tokenVocab = MiniCSharpScanner;
}

program : (using)* CLASS ident LEFTBRACK (varDecl | classDecl | methodDecl)* RIGHTBRACK EOF                    #programAST;

using : USING ident SEMICOLON                                                                                  #usignAST;                                                                                 

//se agrega a cada declaración de variable un entero que pueda servir para generación de código
varDecl locals [int indexVar=0] : type ident (COMMA ident)* SEMICOLON                                                                  #varDeclaAST;

classDecl : CLASS ident LEFTBRACK (varDecl)* RIGHTBRACK                                                        #classDeclaAST;

methodDecl : (type | VOID) ident LEFTPAREN formPars? RIGHTPAREN block                                          #methDeclaAST;

formPars : type ident (COMMA type ident)*                                                                      #formParsAST;

type : ident (LEFTSBRACK RIGHTSBRACK)?                                                                         #typeAST;

statement : designator (ASSIGN expr | LEFTPAREN actPars? RIGHTPAREN | PLUSPLUS | MINUSMINUS) SEMICOLON         #assignStatementAST
          | IF LEFTPAREN condition RIGHTPAREN statement (ELSE statement)?                                      #ifStatementAST
          | FOR LEFTPAREN expr SEMICOLON condition? SEMICOLON statement? RIGHTPAREN statement                  #forStatementAST
          | WHILE LEFTPAREN condition RIGHTPAREN statement                                                     #whileConditionStatementAST
          | BREAK SEMICOLON                                                                                    #breakStatementAST
          | RETURN expr? SEMICOLON                                                                             #returnStatementAST
          | READ LEFTPAREN designator RIGHTPAREN SEMICOLON                                                     #whileNumberStatementAST
          | WRITE LEFTPAREN expr (COMMA (INT | DOUBLE))? RIGHTPAREN SEMICOLON                                  #writeNumberStatementAST
          | block                                                                                              #blockStatementAST
          | SEMICOLON                                                                                          #semicolonStatementAST;

block : LEFTBRACK (varDecl | statement)* RIGHTBRACK                                                            #blockAST;
 
actPars : expr (COMMA expr)*                                                                                   #actParsAST; 

condition : condTerm (OR condTerm)*                                                                            #conditionAST;

condTerm : condFact (AND condFact)*                                                                            #condTermAST;

condFact : expr relop expr                                                                                     #condFactAST;

cast : LEFTPAREN type RIGHTPAREN                                                                               #castAST;

expr : MINUS? cast? term (addop term)*                                                                         #exprAST;

term : factor (mulop factor)*                                                                                  #termAST;

factor : designator (LEFTPAREN (actPars)? RIGHTPAREN)?                                                         #designFactorAST
       | CHARCONST                                                                                             #charconstFactorAST
       | STRINGCONST                                                                                           #strconstFactorAST
       | INT                                                                                                   #intFactorAST
       | DOUBLE                                                                                                #doubFactorAST
       | BOOL                                                                                                  #boolFactorAST
       | NEW ident                                                                                             #newIdentFactorAST
       | LEFTPAREN expr RIGHTPAREN                                                                             #exprInparentFactorAST;

// SE AGREGA LA REGLA PARA ident CON LA IDEA DE QUE CADA NUEVO NODO TENGA UN PUNTERO A SU DECLARACIÓN 
// ParserRuleContext es la clase más arriba en la gerarquía del AST de ANTRLR4
ident locals [ParserRuleContext declPointer = null] : INT_ID                                                                                                 #intIdIdentAST
        | CHAR_ID                                                                                              #charIdIdentAST
        | DOUBLE_ID                                                                                            #doubIdIdentAST
        | BOOL_ID                                                                                              #boolIdIdentAST
        | STRING_ID                                                                                            #strIdIdentAST
        | IDENTIFIER                                                                                           #identifierIdentAST
        | LIST                                                                                                 #listIdentAST
        ;    

designator : ident (DOT ident | LEFTSBRACK (MINUS)? (IDENTIFIER|INT) RIGHTSBRACK)*                                      #designatorAST;

relop : EQUALS                                                                                                  
        | NOTEQUALS                                                                                            
        | GREATERTHAN                                                                                          
        | GREATOREQUALS                                                                                        
        | LESSTHAN                                                                                             
        | LESSOREQUALS                                                                                         #relopAST; 

addop : PLUS                                                                                                   
        | MINUS                                                                                                #addopAST;

mulop : MULT                                                                                                    
        | DIV                                                                                                  
        | MOD                                                                                                  #mulopAST;

    