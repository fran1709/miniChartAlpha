using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Antlr4.Runtime;
using miniChartAlpha.Logica.TypeManager;

namespace miniChartAlpha.Logica
{

    public class CSharpContextual : MiniCSharpParserBaseVisitor<Object>
    {
        public CSTablaSimbolos laCsTablaSimbolos;

        // Tercera etapa 
        private Type pointType = null;
        private string asmFileName = "test.exe";
        private AssemblyName myAsmName = new AssemblyName();

        private AppDomain currentDom = Thread.GetDomain();
        private AssemblyBuilder myAsmBldr;

        private ModuleBuilder myModuleBldr;

        private TypeBuilder myTypeBldr;
        private ConstructorInfo objCtor = null;

        private MethodInfo writeMI, writeMS;

        private MethodBuilder pointMainBldr, currentMethodBldr;

        private List<MethodBuilder> metodosGlobales;

        private bool isArgument = false;

        public CSharpContextual()
        {
            laCsTablaSimbolos = new CSTablaSimbolos();

            // Tercera etapa
            metodosGlobales = new List<MethodBuilder>();

            myAsmName.Name = "TestASM";
            myAsmBldr = currentDom.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);
            myModuleBldr = myAsmBldr.DefineDynamicModule(asmFileName);

            //inicializar writeline para string

            writeMI = typeof(Console).GetMethod(
                "WriteLine",
                new Type[] { typeof(int) });
            writeMS = typeof(Console).GetMethod(
                "WriteLine",
                new Type[] { typeof(string) });
        }

        public bool isMultiple(string type)
        {
            switch (type)
            {
                case "==": return true;
                case "!=": return true;
                default: return false;
            }
        }

        private bool hasReturn = false;

        private String ShowErrorPosition(IToken t)
        {
            return " Fila: " + t.Line + " , Columna: " + (t.Column + 1);
        }

        // program : (using)* CLASS ident LEFTBRACK (varDecl | classDecl | methodDecl)* RIGHTBRACK EOF
        public override object VisitProgramAST(MiniCSharpParser.ProgramASTContext context)
        {
            //Visita a los using
            for (int i = 0; i < context.@using().Length; i++)
            {
                Visit(context.@using(i));
            }

            //Agregar la clase PRINCIPAL a la tabla 
            IToken id = (IToken)Visit(context.ident());
            laCsTablaSimbolos.openScope();
            if (Clase.IsClase(id.Text))
            {
                Clase clase = new Clase(id, context);
                laCsTablaSimbolos.insertar(clase);
            }
            
            //Se define la clase principal
            myTypeBldr = myModuleBldr.DefineType(id.Text, TypeAttributes.Public);
            
            //Creación del constructor de la clase???
            Type objType = Type.GetType("System.Object");
            objCtor = objType.GetConstructor(new Type[0]);
            Type[] ctorParams = new Type[0];
            ConstructorBuilder pointCtor = myTypeBldr.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                ctorParams);
            ILGenerator ctorIL = pointCtor.GetILGenerator();
            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, objCtor);
            ctorIL.Emit(OpCodes.Ret);

            Metodo addMethod = new Metodo(null, (int)Metodo.TipoMetodo.Void, context);
            addMethod.MethodNombre = "add";
            addMethod.cantidadParam = 2;
            addMethod.parametros.AddLast(new Arreglo(null, (int)Metodo.TipoMetodo.Multiple, context));
            addMethod.parametros.AddLast(new TipoBasico(null, (int)Metodo.TipoMetodo.Multiple, context));
            laCsTablaSimbolos.insertar(addMethod);

            Metodo delMethod = new Metodo(null, (int)Metodo.TipoMetodo.Void, context);
            delMethod.MethodNombre = "del";
            delMethod.cantidadParam = 2;
            delMethod.parametros.AddLast(new Arreglo(null, (int)Metodo.TipoMetodo.Multiple, context));
            delMethod.parametros.AddLast(new TipoBasico(null, (int)TipoBasico.TiposBasicos.Int, context));
            laCsTablaSimbolos.insertar(delMethod);

            Metodo lenMethod = new Metodo(null, (int)TipoBasico.TiposBasicos.Int, context);
            lenMethod.MethodNombre = "len";
            lenMethod.cantidadParam = 1;
            lenMethod.parametros.AddLast(new Arreglo(null, (int)Metodo.TipoMetodo.Multiple, context));
            laCsTablaSimbolos.insertar(lenMethod);
            
            currentMethodBldr = myTypeBldr.DefineMethod("len",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(int),
                new[] { typeof(Array) });

            ILGenerator lenIL = currentMethodBldr.GetILGenerator();
            lenIL.Emit(OpCodes.Ldarg_0); // Carga el arreglo
            lenIL.Emit(OpCodes.Ldlen); //Carga el len del arreglo
            lenIL.Emit(OpCodes.Ret); // Retorna el len
            
            //Se agrega el método recién creado a la lista de mpetodos globales para no perder su referencia cuando se creen más métodos
            metodosGlobales.Add(currentMethodBldr);
            
            //Vista a la declaración de clases
            for (int i = 0; i < context.classDecl().Length; i++)
            {
                Visit(context.classDecl(i));
            }

            //Vista a la declaración de variables
            for (int i = 0; i < context.varDecl().Length; i++)
            {
                Visit(context.varDecl(i));
            }

            //Declaración de constantes?

            //Vista a la declaración de métodos
            for (int i = 0; i < context.methodDecl().Length; i++)
            {
                Visit(context.methodDecl(i));
            }

            pointType = myTypeBldr.CreateType(); //crea la clase para ser luego instanciada
            myAsmBldr.SetEntryPoint(pointMainBldr);
            myAsmBldr.Save(asmFileName);
            
            laCsTablaSimbolos.CloseScope();
            laCsTablaSimbolos.consola.Show();
            return null;
        }

        // using : USING ident SEMICOLON
        public override object VisitUsignAST(MiniCSharpParser.UsignASTContext context)
        {
            Visit(context.ident());
            return null;
        }

        // varDecl : type ident (COMMA ident)* SEMICOLON   
        public override object VisitVarDeclaAST(MiniCSharpParser.VarDeclaASTContext context)
        {
            LinkedList<Object> variables = new LinkedList<object>();
            Tipo variable;
            //Se recorren todas las variables (cuando se declaran de un mismo tipo separadas por coma)
            for (int i = 0; i < context.ident().Length; i++)
            {
                int idType = (int)Visit(context.type()); //Se verifica el tipo de datos de la variable
                IToken id = (IToken)Visit(context.ident(i));
                Tipo varType = laCsTablaSimbolos.buscarObjetoTipo<Tipo>(id.Text);
                if (varType == null || varType.nivel != laCsTablaSimbolos.nivelActual)
                {
                    if (TipoBasico.isTipoBasico(context.type().GetText()))
                    {
                        variable = new TipoBasico(id, idType, context);
                        variables.AddLast(variable);
                        laCsTablaSimbolos.insertar(variable);

                    }

                    else if (Arreglo.isTipoArreglo(context.type().GetText()))
                    {
                        variable = new Arreglo(id, idType, context);
                        variables.AddLast(variable);
                        laCsTablaSimbolos.insertar(variable);
                    }

                    else if (TipoClase.IsTipoClase(context.type().GetText()))
                    {
                        Clase searched = laCsTablaSimbolos.buscarObjetoTipo<Clase>(context.type().GetText());
                        if (searched != null)
                        {
                            variable = new TipoClase(id, context.type().GetText(), context);
                            variables.AddLast(variable);
                            laCsTablaSimbolos.insertar(variable);
                        }
                        else
                        {
                            laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos: Tipo: \"" +
                                                                            context.type().GetText() +
                                                                            "\" no es un tipo válido." +
                                                                            ShowErrorPosition(context.type().Start) +
                                                                            "\n";
                        }

                    }

                }
                else
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de declaracion: El identificador: \"" +
                                                                    id.Text + "\" ya ha sido declarado." +
                                                                    ShowErrorPosition(id) + "\n";

                }

            }

            return variables;
        }

        // classDecl : CLASS ident LEFTBRACK (varDecl)* RIGHTBRACK  
        public override object VisitClassDeclaAST(MiniCSharpParser.ClassDeclaASTContext context)
        {
            IToken id = context.ident().Start;
            Clase claseBuscada = laCsTablaSimbolos.buscarObjetoTipo<Clase>(id.Text);

            if (Clase.IsClase(id.Text) && claseBuscada == null)
            {
                Clase clase = new Clase(id, context);
                laCsTablaSimbolos.openScope();
                //Se recorren las declaraciones de las variables de la clase
                for (int i = 0; i < context.varDecl().Length; i++)
                {
                    LinkedList<object> classVars = (LinkedList<object>)Visit(context.varDecl(i));
                    foreach (Tipo variable in classVars)
                    {
                        if (variable is TipoBasico)
                        {
                            clase.addVariable(variable);
                        }
                        else
                        {
                            laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                "Error de declaracion: El tipo de variable \"" + variable.tipo +
                                "\" no es permitido en la clase." + ShowErrorPosition(variable.tok) + "\n";

                        }
                    }
                }

                laCsTablaSimbolos.CloseScope();
                laCsTablaSimbolos.insertar(clase);
            }
            else
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de declaracion: La clase: \"" + id.Text +
                                                                "\" ya ha sido declarada." + ShowErrorPosition(id) +
                                                                "\n";
            }

            return null;
        }

        // methodDecl : (type | VOID) ident LEFTPAREN formPars? RIGHTPAREN block
        public override object VisitMethDeclaAST(MiniCSharpParser.MethDeclaASTContext context)
        {
            hasReturn = false;
            IToken id = (IToken)Visit(context.ident());
            Metodo metodoBuscado = laCsTablaSimbolos.buscarObjetoTipo<Metodo>(id.Text);
            if (metodoBuscado == null)
            {
                int idType = (int)Metodo.TipoMetodo.Void;
                if (context.VOID() == null)
                {
                    idType = (int)Visit(context.type());
                }

                Metodo metodo = new Metodo(id, idType, context);


                //Visita a los parametros del método
                if (context.formPars() != null)
                {
                    metodo.parametros = (LinkedList<object>)Visit(context.formPars());
                    metodo.cantidadParam = metodo.parametros.Count;
                }

                laCsTablaSimbolos.insertar(metodo);
                //Visita al bloque del método
                Visit(context.block());
                if (context.type() != null && !hasReturn)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de retorno: El método \"" + id.Text +
                                                                    "\" debe tener una expresión de retorno." +
                                                                    ShowErrorPosition(id) + "\n";
                }
            }
            else
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de declaracion: El metodo: \"" + id.Text +
                                                                "\" ya ha sido declarado." + ShowErrorPosition(id) +
                                                                "\n";
            }

            return null;
        }

        // formPars : type ident (COMMA type ident)*  
        public override object VisitFormParsAST(MiniCSharpParser.FormParsASTContext context)
        {
            LinkedList<Object> parametros = new LinkedList<object>();

            for (int i = 0; i < context.type().Length; i++)
            {
                int idType = (int)Visit(context.type(i));
                IToken id = (IToken)Visit(context.ident(i));

                if (TipoBasico.isTipoBasico(context.type(i).GetText()))
                {
                    TipoBasico tb = new TipoBasico(id, idType, context);
                    laCsTablaSimbolos.insertar(tb);
                    parametros.AddLast(tb);
                }
                else if (TipoClase.IsTipoClase(context.type(i).GetText()))
                {
                    TipoClase tc = new TipoClase(id, context.type(i).GetText(), context);
                    laCsTablaSimbolos.insertar(tc);
                    parametros.AddLast(tc);
                }
                else if (Arreglo.isTipoArreglo(context.type(i).GetText()))
                {
                    Arreglo arr = new Arreglo(id, idType, context);
                    laCsTablaSimbolos.insertar(arr);
                    parametros.AddLast(arr);
                }
                else
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos: Tipo: \"" +
                                                                    context.type(i).GetText() +
                                                                    "\" no es un tipo válido." +
                                                                    ShowErrorPosition(context.type(i).Start) + "\n";
                }
            }

            return parametros;
        }

        //  type : ident (LEFTSBRACK RIGHTSBRACK)? 
        public override object VisitTypeAST(MiniCSharpParser.TypeASTContext context)
        {
            TipoBasico.TiposBasicos result = TipoBasico.TiposBasicos.Error;
            if (context.ident().GetText().Equals("int") || context.ident().GetText().Equals("int[]"))
            {
                result = TipoBasico.TiposBasicos.Int;
            }
            else if (context.ident().GetText().Equals("double"))
            {
                result = TipoBasico.TiposBasicos.Double;
            }
            else if (context.ident().GetText().Equals("char") || context.ident().GetText().Equals("char[]"))
            {
                result = TipoBasico.TiposBasicos.Char;
            }
            else if (context.ident().GetText().Equals("string"))
            {
                result = TipoBasico.TiposBasicos.String;
            }
            else if (context.ident().GetText().Equals("bool"))
            {
                result = TipoBasico.TiposBasicos.Boolean;
            }
            else if (Clase.IsClase(context.ident().GetText()))
            {
                result = TipoBasico.TiposBasicos.Error;
            }
            else
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos: Tipo: \"" + context.ident().GetText() +
                                                                "\" no es un tipo válido." +
                                                                ShowErrorPosition(context.ident().Start) + "\n";
            }

            return result;
        }

        // statement : designator (ASSIGN expr | LEFTPAREN actPars? RIGHTPAREN | PLUSPLUS | MINUSMINUS) SEMICOLON 
        public override object VisitAssignStatementAST(MiniCSharpParser.AssignStatementASTContext context)
        {
            var idType = Visit(context.designator());
            Metodo metodo = laCsTablaSimbolos.buscarObjetoTipo<Metodo>(context.designator().GetText());

            if (context.expr() != null)
            {
                if (idType == null && laCsTablaSimbolos.buscarObjetoTipo<Tipo>(context.designator().GetText()) == null)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" +
                                                                    context.designator().GetText() +
                                                                    "\" no declarado en asignación." +
                                                                    ShowErrorPosition(context.designator().Start) +
                                                                    "\n";
                }

                var exprType = Visit(context.expr());
                if (exprType is int && exprType != idType ||
                    exprType is TipoBasico.TiposBasicos && !exprType.Equals(idType) ||
                    exprType is string type3 && !type3.Equals(idType))
                {
                    if (exprType is int && (int)exprType == 7)
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text +=
                            "Error de tipos: no se puede asignar un metodo \"" + Metodo.showTipo((int)exprType) +
                            "\" a una variable." + ShowErrorPosition(context.ASSIGN().Symbol) + "\n";
                    }
                    else if (idType is int)
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos: \"" +
                                                                        (TipoBasico.TiposBasicos)idType + "\" y \"" +
                                                                        exprType + "\" no son compatibles." +
                                                                        ShowErrorPosition(context.ASSIGN().Symbol) +
                                                                        "\n";
                    }
                    else
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos: \"" + idType + "\" y \"" +
                                                                        exprType + "\" no son compatibles." +
                                                                        ShowErrorPosition(context.ASSIGN().Symbol) +
                                                                        "\n";
                }

                var arrayDsg = laCsTablaSimbolos.buscarObjetoTipo<Arreglo>(context.designator().GetText());
                var arrayExpr = laCsTablaSimbolos.buscarObjetoTipo<Arreglo>(context.expr().GetText());
                if (arrayDsg != null && arrayExpr != null)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error: Asignación directa de arreglos \"" +
                                                                    context.designator().GetText() + "\" y \"" +
                                                                    context.expr().GetText() + "\" no permitida." +
                                                                    ShowErrorPosition(context.ASSIGN().Symbol) + "\n";
                }
            }

            if (context.actPars() != null)
            {
                if (metodo == null)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" +
                                                                    context.designator().GetText() +
                                                                    "\" no declarado en asignación." +
                                                                    ShowErrorPosition(context.designator().Start) +
                                                                    "\n";
                }
                else
                {
                    LinkedList<Object> pars = (LinkedList<Object>)Visit(context.actPars());
                    if (pars.Count != metodo.parametros.Count)
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de parámetros: El método \"" +
                                                                        context.designator().GetText() + "\" espera " +
                                                                        metodo.parametros.Count() +
                                                                        " parámetros, pero se encontraron " +
                                                                        pars.Count + " parámetros." +
                                                                        ShowErrorPosition(context.designator().Start) +
                                                                        "\n";
                    }
                    else
                    {
                        var declParams = metodo.parametros.First;
                        var actParams = pars.First;
                        for (int i = 0; i < pars.Count; i++)
                        {
                            Tipo declPar = (Tipo)declParams.Value;
                            var actPar = actParams.Value;
                            if (declPar is Arreglo arreglo && arreglo.tipoDato == (int)Metodo.TipoMetodo.Multiple ||
                                declPar is TipoBasico basico && basico.tipoDato == (int)Metodo.TipoMetodo.Multiple)
                            {
                                if (!Metodo.checkParsType((int)TipoBasico.TiposBasicos.Int, (int)actPar) &&
                                    !Metodo.checkParsType((int)TipoBasico.TiposBasicos.Char, (int)actPar))
                                {
                                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                        "Error de tipos: El tipo del parámetro en la posición " + (i + 1) +
                                        " no coincide con el tipo declarado." +
                                        ShowErrorPosition(context.designator().Start) + "\n";
                                }
                            }
                            else
                            {
                                if (!Metodo.checkParsType(declPar, actPar))
                                {
                                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                        "Error de tipos: El tipo del parámetro en la posición " + (i + 1) +
                                        " no coincide con el tipo declarado." +
                                        ShowErrorPosition(context.designator().Start) + "\n";
                                }
                            }

                            declParams = declParams.Next;
                            actParams = actParams.Next;
                        }
                    }
                }
            }

            return null;
        }

        // statement : IF LEFTPAREN condition RIGHTPAREN statement (ELSE statement)?  
        public override object VisitIfStatementAST(MiniCSharpParser.IfStatementASTContext context)
        {
            TipoBasico.TiposBasicos tipoCondicion = (TipoBasico.TiposBasicos)Visit(context.condition());
            if (tipoCondicion.Equals(TipoBasico.TiposBasicos.Error))
            {
                // La expresión es nula, se reporta el error
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error: La condición del if es nula." +
                                                                ShowErrorPosition(context.condition().Start) + "\n";
                return null;
            }

            Visit(context.statement(0));
            if (context.statement().Length > 1)
            {
                Visit(context.statement(1));
            }

            return tipoCondicion;
        }

        // statement : FOR LEFTPAREN expr SEMICOLON condition? SEMICOLON statement? RIGHTPAREN statement
        public override object VisitForStatementAST(MiniCSharpParser.ForStatementASTContext context)
        {
            var tipoExpr = Visit(context.expr());

            // Verificar que el tipo del expr sea numérico
            if (tipoExpr == null || !(tipoExpr is int))
            {
                // Si la condition existe, visita su subárbol 
                if (context.condition() != null)
                {
                    Visit(context.condition());
                }

                // Si statement existe, visita su subárbol y obtiene su tipo
                if (context.statement() != null)
                {
                    Visit(context.statement(0));
                }

                if (context.statement().Length > 1)
                {
                    Visit(context.statement(1));
                }

            }
            else
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text +=
                    "Error: La expresión del for debe ser de tipo numérico.\n";
                return null;
            }

            return null;
        }

        // statement : WHILE LEFTPAREN condition RIGHTPAREN statement
        public override object VisitWhileConditionStatementAST(
            MiniCSharpParser.WhileConditionStatementASTContext context)
        {
            Visit(context.condition());
            Visit(context.statement());
            return null;
        }

        // statement : RETURN expr? SEMICOLON
        public override object VisitReturnStatementAST(MiniCSharpParser.ReturnStatementASTContext context)
        {
            if (context.Parent is MiniCSharpParser.BlockASTContext blockContext)
            {
                if (blockContext.Parent is MiniCSharpParser.MethDeclaASTContext methodDeclContext)
                {
                    hasReturn = true;
                    string methodName = methodDeclContext.ident().GetText();
                    Metodo returnType = laCsTablaSimbolos.buscarObjetoTipo<Metodo>(methodName);
                    if (context.expr() != null)
                    {
                        var exprType = Visit(context.expr());
                        if (returnType.tipoDato != (int)Metodo.TipoMetodo.Void &&
                            returnType.tipoDato != (int)Metodo.TipoMetodo.Error)
                        {
                            if (returnType.tipoDato != (int)exprType)
                            {
                                laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                    "Error de tipos: El tipo de retorno del método \"" + methodName +
                                    "\" no coincide con el tipo de la expresión de retorno." +
                                    ShowErrorPosition(context.RETURN().Symbol) + "\n";
                            }
                        }
                        else
                        {
                            laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de retorno: El método \"" +
                                                                            methodName + "\" es de tipo \"" +
                                                                            Metodo.showTipo(returnType.tipoDato) +
                                                                            "\" no debe tener una expresión de retorno." +
                                                                            ShowErrorPosition(methodDeclContext
                                                                                .RIGHTPAREN().Symbol) + "\n";

                        }
                    }
                }
                else
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                        "Error: Las instrucciones de retorno no están permitidas fuera de los métodos." +
                        ShowErrorPosition(context.RETURN().Symbol) + "\n";
                }
            }
            else
            {
                if (context.expr() != null)
                {
                    Visit(context.expr());
                }
            }

            return null;
        }

        //  statement : READ LEFTPAREN designator RIGHTPAREN SEMICOLON    
        public override object VisitWhileNumberStatementAST(MiniCSharpParser.WhileNumberStatementASTContext context)
        {
            var type = Visit(context.designator());
            if (type == null || (TipoBasico.TiposBasicos)type == TipoBasico.TiposBasicos.Error)
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" +
                                                                context.designator().GetText() +
                                                                "\" no declarado en el metodo \"" +
                                                                context.READ().Symbol.Text + "\"." +
                                                                ShowErrorPosition(context.designator().Start) + "\n";
            }

            return null;
        }

        // statement : WRITE LEFTPAREN expr (COMMA (INT | DOUBLE))? RIGHTPAREN SEMICOLON
        public override object VisitWriteNumberStatementAST(MiniCSharpParser.WriteNumberStatementASTContext context)
        {
            Object exprType = Visit(context.expr());
            if (exprType == null || (TipoBasico.TiposBasicos)exprType == TipoBasico.TiposBasicos.Error)
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" +
                                                                context.expr().GetText() +
                                                                "\" no declarado en el metodo \"" +
                                                                context.WRITE().Symbol.Text + "\"." +
                                                                ShowErrorPosition(context.expr().Start) + "\n";

            }

            return null;
        }

        // statement : block
        public override object VisitBlockStatementAST(MiniCSharpParser.BlockStatementASTContext context)
        {
            Visit(context.block());
            return null;
        }

        // block : LEFTBRACK (varDecl | statement)* RIGHTBRACK
        public override object VisitBlockAST(MiniCSharpParser.BlockASTContext context)
        {
            laCsTablaSimbolos.openScope();
            for (int i = 0; i < context.varDecl().Length; i++)
            {
                Visit(context.varDecl(i));
            }

            for (int i = 0; i < context.statement().Length; i++)
            {
                Visit(context.statement(i));
            }

            laCsTablaSimbolos.CloseScope();

            return null;
        }

        // actPars : expr (COMMA expr)*
        public override object VisitActParsAST(MiniCSharpParser.ActParsASTContext context)
        {
            LinkedList<Object> param = new LinkedList<object>();
            for (int i = 0; i < context.expr().Length; i++)
            {
                Object exprType = Visit(context.expr(i));
                if (exprType == null ||
                    exprType is TipoBasico.TiposBasicos type && type == TipoBasico.TiposBasicos.Error)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, expresion invalida \"" +
                                                                    context.expr(i).GetText() + "\" en parametros." +
                                                                    ShowErrorPosition(context.expr(i).Start) + "\n";
                }

                param.AddLast(exprType);
            }

            return param;
        }

        // condition : condTerm (OR condTerm)*
        public override object VisitConditionAST(MiniCSharpParser.ConditionASTContext context)
        {
            TipoBasico.TiposBasicos type = TipoBasico.TiposBasicos.Error;
            for (int i = 0; i < context.condTerm().Length; i++)
            {
                type = (TipoBasico.TiposBasicos)Visit(context.condTerm(i));
                if (type.Equals(TipoBasico.TiposBasicos.Error))
                {
                    break;
                }
            }

            return type;
        }

        // condTerm : condFact (AND condFact)*
        public override object VisitCondTermAST(MiniCSharpParser.CondTermASTContext context)
        {
            TipoBasico.TiposBasicos type = TipoBasico.TiposBasicos.Error;
            for (int i = 0; i < context.condFact().Length; i++)
            {
                type = (TipoBasico.TiposBasicos)Visit(context.condFact(i));
                if (type.Equals(TipoBasico.TiposBasicos.Error))
                {
                    break;
                }
            }

            return type;
        }

        // condFact : expr RELOP expr
        public override object VisitCondFactAST(MiniCSharpParser.CondFactASTContext context)
        {
            var type1 = (int)Visit(context.expr(0));
            var type2 = (int)Visit(context.expr(1));
            if (!isMultiple(context.relop().GetText()) && !type1.Equals(type2))
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos " + TipoBasico.showTipo(type1) + " y " +
                                                                TipoBasico.showTipo(type2) +
                                                                " no son compatibles. Solo se aceptan de tipo INT" +
                                                                ShowErrorPosition(context.expr(0).Start) + "\n";
                return TipoBasico.TiposBasicos.Error;
            }

            if (isMultiple(context.relop().GetText()) && !type1.Equals(type2))
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos " + TipoBasico.showTipo(type1) + " y " +
                                                                TipoBasico.showTipo(type2) + " no son compatibles." +
                                                                ShowErrorPosition(context.expr(0).Start) + "\n";
                return TipoBasico.TiposBasicos.Error;

            }

            return TipoBasico.TiposBasicos.Boolean;
        }

        // cast : LEFTPAREN type RIGHTPAREN  
        public override object VisitCastAST(MiniCSharpParser.CastASTContext context)
        {
            return Visit(context.type());
        }

        // expr : MINUS? cast? term (addop term)* 
        public override object VisitExprAST(MiniCSharpParser.ExprASTContext context)
        {
            var type = Visit(context.term(0));
            for (int i = 1; i < context.term().Length; i++)
            {
                var type2 = Visit(context.term(i));
                if (!type.Equals(type2))
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos " + type + " y " + type2 +
                                                                    " no son compatibles. Solo se aceptan de tipo int" +
                                                                    ShowErrorPosition(context.term(0).Start) + "\n";
                    if (!isMultiple(context.addop(i - 1).GetText()))
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de operador " +
                                                                        context.addop(i - 1).GetText() +
                                                                        " no es permtido en este caso. " +
                                                                        ShowErrorPosition(context.addop(0).Start) +
                                                                        "\n";
                    }
                }

                Visit(context.addop(i - 1));
            }

            if (context.cast() != null)
            {
                if (context.Parent is MiniCSharpParser.AssignStatementASTContext assignContext)
                {
                    string variableName = assignContext.designator().GetText();
                    var casteo = Visit(context.cast());
                    var castedVar = laCsTablaSimbolos.buscarObjetoTipo<TipoBasico>(context.term(0).GetText());
                    if (type.Equals(TipoBasico.TiposBasicos.Int) && casteo.Equals(TipoBasico.TiposBasicos.Double))
                    {
                        if (castedVar != null && castedVar.tok.Text.Equals(variableName))
                        {
                            laCsTablaSimbolos.CastVariable(context.term(0).GetText(), 1);
                            return type;
                        }

                        return casteo;
                    }

                    if (type.Equals(TipoBasico.TiposBasicos.Double) && casteo.Equals(TipoBasico.TiposBasicos.Int))
                    {
                        if (castedVar != null && castedVar.tok.Text.Equals(variableName))
                        {
                            laCsTablaSimbolos.CastVariable(context.term(0).GetText(), 0);
                            return type;
                        }

                        return casteo;
                    }

                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                        "Error de casteo, no es posible castear una variable tipo " + TipoBasico.showTipo((int)type) +
                        " al tipo " + TipoBasico.showTipo((int)casteo) + ". " +
                        ShowErrorPosition(context.cast().Start) + "\n";
                }
            }

            return type;
        }

        // term : factor (mulop factor)* 
        public override object VisitTermAST(MiniCSharpParser.TermASTContext context)
        {
            // tipo de factor
            var type = Visit(context.factor(0));
            for (int i = 1; i < context.factor().Length; i++)
            {
                var type2 = Visit(context.factor(i));
                if (!type.Equals(type2))
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de tipos " + type + " y " + type2 +
                                                                    " no son compatibles. Solo se aceptan de tipo INT" +
                                                                    ShowErrorPosition(context.factor(0).Start) + "\n";
                    if (!isMultiple(context.mulop(i - 1).GetText()))
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de operando " +
                                                                        context.mulop(i - 1).GetText() +
                                                                        " no es permtido en este caso. " +
                                                                        ShowErrorPosition(context.mulop(0).Start) +
                                                                        "\n";
                    }
                }

                Visit(context.mulop(i - 1));
            }

            return type;
        }

        // factor : designator (LEFTPAREN (actPars)? RIGHTPAREN)?
        public override object VisitDesignFactorAST(MiniCSharpParser.DesignFactorASTContext context)
        {
            var type = Visit(context.designator());
            Metodo metodo = laCsTablaSimbolos.buscarObjetoTipo<Metodo>(context.designator().GetText());
            if (context.actPars() != null)
            {
                if (metodo == null)
                {
                    laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" +
                                                                    context.designator().GetText() +
                                                                    "\" no declarado en asignación." +
                                                                    ShowErrorPosition(context.designator().Start) +
                                                                    "\n";
                }
                else
                {
                    LinkedList<Object> pars = (LinkedList<Object>)Visit(context.actPars());
                    if (pars.Count != metodo.parametros.Count)
                    {
                        laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de parámetros: El método \"" +
                                                                        context.designator().GetText() + "\" espera " +
                                                                        metodo.parametros.Count() +
                                                                        " parámetros, pero se encontraron " +
                                                                        pars.Count + " parámetros." +
                                                                        ShowErrorPosition(context.designator().Start) +
                                                                        "\n";
                    }
                    else
                    {
                        var declParams = metodo.parametros.First;
                        var actParams = pars.First;
                        for (int i = 0; i < pars.Count; i++)
                        {
                            Tipo declPar = (Tipo)declParams.Value;
                            var actPar = actParams.Value;
                            if (declPar is Arreglo arreglo && arreglo.tipoDato == (int)Metodo.TipoMetodo.Multiple ||
                                declPar is TipoBasico basico && basico.tipoDato == (int)Metodo.TipoMetodo.Multiple)
                            {
                                if (!Metodo.checkParsType((int)TipoBasico.TiposBasicos.Int, (int)actPar) &&
                                    !Metodo.checkParsType((int)TipoBasico.TiposBasicos.Char, (int)actPar))
                                {
                                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                        "Error de tipos: El tipo del parámetro en la posición " + (i + 1) +
                                        " no coincide con el tipo declarado." +
                                        ShowErrorPosition(context.designator().Start) + "\n";
                                }
                            }
                            else
                            {
                                if (!Metodo.checkParsType(declPar, actPar))
                                {
                                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                                        "Error de tipos: El tipo del parámetro en la posición " + (i + 1) +
                                        " no coincide con el tipo declarado." +
                                        ShowErrorPosition(context.designator().Start) + "\n";
                                }
                            }

                            declParams = declParams.Next;
                            actParams = actParams.Next;
                        }
                    }
                }
            }

            return type;
        }

        // factor : CHARCONST
        public override object VisitCharconstFactorAST(MiniCSharpParser.CharconstFactorASTContext context)
        {
            return TipoBasico.TiposBasicos.Char;
        }

        // factor : STRINGCONST
        public override object VisitStrconstFactorAST(MiniCSharpParser.StrconstFactorASTContext context)
        {
            return TipoBasico.TiposBasicos.String;
        }

        // factor : INT 
        public override object VisitIntFactorAST(MiniCSharpParser.IntFactorASTContext context)
        {
            return TipoBasico.TiposBasicos.Int;
        }

        // factor : DOUBLE
        public override object VisitDoubFactorAST(MiniCSharpParser.DoubFactorASTContext context)
        {
            return TipoBasico.TiposBasicos.Double;
        }

        // factor : BOOL
        public override object VisitBoolFactorAST(MiniCSharpParser.BoolFactorASTContext context)
        {
            return TipoBasico.TiposBasicos.Boolean;
        }

        // factor : NEW ident
        public override object VisitNewIdentFactorAST(MiniCSharpParser.NewIdentFactorASTContext context)
        {
            IToken token = (IToken)Visit(context.ident());
            string type = token.Text;
            if (!TipoClase.IsTipoClase(type) && !Arreglo.isTipoArreglo(type))
            {
                laCsTablaSimbolos.consola.SalidaConsola.Text +=
                    "Error de tipos, se esperaba una clase o un arreglo de tipo válido, se encontró " + token.Text +
                    "\n" + ShowErrorPosition(context.ident().Start) + "\n";
                return null;
            }

            switch (type)
            {
                case "int[]":
                    return TipoBasico.TiposBasicos.Int;
                case "char[]":
                    return TipoBasico.TiposBasicos.Char;
                default:
                {
                    Clase newClase = laCsTablaSimbolos.buscarObjetoTipo<Clase>(type);
                    if (newClase != null)
                    {
                        return newClase.tok.Text;
                    }

                    break;
                }
            }

            return null;
        }

        // factor : LEFTPAREN expr RIGHTPAREN 
        public override object VisitExprInparentFactorAST(MiniCSharpParser.ExprInparentFactorASTContext context)
        {
            return Visit(context.expr());
            ;
        }

        // designator : ident (DOT ident | LEFTSBRACK expr RIGHTSBRACK)* 
        public override object VisitDesignatorAST(MiniCSharpParser.DesignatorASTContext context)
        {
            Tipo varType = null;

            for (int i = 0; i < context.ident().Length; i++)
            {
                var idToken = (IToken)Visit(context.ident(i));
                string id = idToken.Text;

                if (i == 0)
                {
                    if (id.EndsWith("[]"))
                    {
                        id = id.Substring(0, id.Length - 2);
                    }
                    else if (id.EndsWith("]"))
                    {
                        int openingBracketIndex = id.LastIndexOf('[');
                        if (openingBracketIndex != -1)
                        {
                            id = id.Substring(0, openingBracketIndex);
                        }
                    }

                    varType = laCsTablaSimbolos.buscarObjetoTipo<Tipo>(id);
                }
                else
                {
                    if (varType is TipoClase claseType)
                    {
                        Clase clase = laCsTablaSimbolos.buscarObjetoTipo<Clase>(claseType.tipoDato);
                        if (id != null)
                        {
                            varType = clase.buscarAtributo<TipoBasico>(id);
                            if (varType == null)
                            {
                                laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, atributo \"" + id +
                                    "\" no encontrado en la clase \"" + clase.tok.Text + "\"." +
                                    ShowErrorPosition(idToken) + "\n";
                                return TipoBasico.TiposBasicos.Error;
                            }
                        }

                    }
                }

            }

            if (varType == null)
            {
                return TipoBasico.TiposBasicos.Error;
            }

            if (varType is TipoBasico basicoType)
            {
                return (TipoBasico.TiposBasicos)basicoType.tipoDato;
            }

            if (varType is TipoClase claseTypo)
            {
                return claseTypo.tipoDato;
            }

            if (varType is Metodo metodoType)
            {
                return metodoType.tipoDato;
            }

            if (varType is Arreglo arregloType)
            {
                var indexIdent = context.IDENTIFIER(0);
                if (indexIdent != null)
                {
                    var indexType = laCsTablaSimbolos.buscarObjetoTipo<TipoBasico>(indexIdent.GetText());
                    if (indexType != null && indexType.tipoDato == (int)TipoBasico.TiposBasicos.Int)
                    {
                        return (TipoBasico.TiposBasicos)arregloType.tipoDato;
                    }

                    laCsTablaSimbolos.consola.SalidaConsola.Text +=
                        "Error: el índice del arreglo debe ser de tipo int" + ShowErrorPosition(indexIdent.Symbol) +
                        "\n";
                    return TipoBasico.TiposBasicos.Error;
                }

                return (TipoBasico.TiposBasicos)arregloType.tipoDato;
            }

            //laCsTablaSimbolos.consola.SalidaConsola.Text += "Error de alcances, identificador \"" + context.ident(0).GetText() + "\" no declarado en asignación." + ShowErrorPosition(context.ident(0).Start) + "\n";
            return TipoBasico.TiposBasicos.Error;
        }

        // ident : INT_ID
        public override object VisitIntIdIdentAST(MiniCSharpParser.IntIdIdentASTContext context)
        {
            return TipoBasico.TiposBasicos.Int;
        }

        // ident : CHAR_ID
        public override object VisitCharIdIdentAST(MiniCSharpParser.CharIdIdentASTContext context)
        {
            return TipoBasico.TiposBasicos.Char;
        }

        // ident : DOUBLE_ID
        public override object VisitDoubIdIdentAST(MiniCSharpParser.DoubIdIdentASTContext context)
        {
            return TipoBasico.TiposBasicos.Double;
        }

        // ident : BOOL_ID
        public override object VisitBoolIdIdentAST(MiniCSharpParser.BoolIdIdentASTContext context)
        {
            return TipoBasico.TiposBasicos.Boolean;
        }

        // ident : STRING_ID
        public override object VisitStrIdIdentAST(MiniCSharpParser.StrIdIdentASTContext context)
        {
            return TipoBasico.TiposBasicos.String;
        }

        // ident : IDENTIFIER
        public override object VisitIdentifierIdentAST(MiniCSharpParser.IdentifierIdentASTContext context)
        {
            return context.IDENTIFIER().Symbol;
        }

        // ident : LIST
        public override object VisitListIdentAST(MiniCSharpParser.ListIdentASTContext context)
        {
            return context.LIST().Symbol;
        }

        public override object VisitRelop(MiniCSharpParser.RelopContext context)
        {
            return context.GetChild(0);
        }

        public override object VisitAddop(MiniCSharpParser.AddopContext context)
        {
            return context.GetChild(0);
        }

        public override object VisitMulop(MiniCSharpParser.MulopContext context)
        {
            return context.GetChild(0);
        }
    }
}