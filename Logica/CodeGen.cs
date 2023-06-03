using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace miniChartAlpha.Logica
{
    public class CodeGen : MiniCSharpParserBaseVisitor<Object>
    {
        private Type pointType = null;
        private string asmFileName = "test.exe";
        private AssemblyName myAsmName = new AssemblyName();

        private AppDomain currentDom = Thread.GetDomain();
        private AssemblyBuilder myAsmBldr;

        private ModuleBuilder myModuleBldr;

        private TypeBuilder myTypeBldr;
        private ConstructorInfo objCtor=null;

        private MethodInfo writeMI, writeMS;

        private MethodBuilder pointMainBldr, currentMethodBldr;

        private List<MethodBuilder> metodosGlobales, metodosLocales;
        private List<Object> variablesGlobales, variablesLocales;

        public CodeGen()
        {
            metodosGlobales = new List<MethodBuilder>();
            
            myAsmName.Name = "TestASM";
            myAsmBldr = currentDom.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.RunAndSave);
            myModuleBldr = myAsmBldr.DefineDynamicModule(asmFileName);
            myTypeBldr = myModuleBldr.DefineType("TestClass");
            
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
            
            //inicializar writeline para string
            
            writeMI = typeof(Console).GetMethod(
                "WriteLine",
                new Type[] { typeof(int) });
            writeMS = typeof(Console).GetMethod(
                "WriteLine",
                new Type[] { typeof(string) });
            
        }
        public override object VisitProgramAST(MiniCSharpParser.ProgramASTContext context)
        {
            foreach (var child in context.children)
            {
                Visit(child);
            }
            pointType = myTypeBldr.CreateType(); //creo un tipo de la clase para luego ser instanciada
            myAsmBldr.SetEntryPoint(pointMainBldr); // setEntryPoint cargar el metodo de entrada a la clase
            myAsmBldr.Save(asmFileName); //

            return pointType;
        }

        public override object VisitUsignAST(MiniCSharpParser.UsignASTContext context)
        {
            Visit(context.ident());
            return null;
        }

        public override object VisitVarDeclaAST(MiniCSharpParser.VarDeclaASTContext context)
        {
            //Se recorren todas las variables (cuando se declaran de un mismo tipo separadas por coma)
            for (int i = 0; i < context.ident().Length; i++)
            {
                Visit(context.type());
                Visit(context.ident(i));
            }

            return null;
        }

        public override object VisitClassDeclaAST(MiniCSharpParser.ClassDeclaASTContext context)
        {
            foreach (var child in context.children)
            {
                Visit(child);
            }
            return null;
        }
        
        private Type verificarTipoRetorno(string tipo)
        {
            switch (tipo)
            {
                case "int":
                    return typeof(int);
                case "char":
                    return typeof(char);
                case "boolean":
                    return typeof(bool);
                case "double":
                    return typeof(double);
                default:
                    return typeof(void);
            }
        }

        public override object VisitMethDeclaAST(MiniCSharpParser.MethDeclaASTContext context)
        {
            Type typeMethod = null;
            if (context.type() != null)
                typeMethod = verificarTipoRetorno((string)Visit(context.type()));
            else if (context.VOID() != null)
                typeMethod = typeof(void);

            currentMethodBldr = myTypeBldr.DefineMethod(context.ident().GetText(),
                MethodAttributes.Public |
                MethodAttributes.Static,
                typeMethod,
                null); //los parámetros son null porque se tiene que visitar despues de declarar el método... se cambiará luego

            //se visitan los parámetros para definir el arreglo de tipos de cada uno de los parámetros formales... si es que hay (not null)
            Type[] parameters = null;
            if (context.formPars() != null)
                parameters = (Type[])Visit(context.formPars());

            //después de visitar los parámetros, se cambia el signatura que requiere la definición del método
            currentMethodBldr.SetParameters(parameters);

            //se visita el cuerpo del método para generar el código que llevará el "currentMethodBldr" de turno
            Visit(context.block());

            ILGenerator currentIL = currentMethodBldr.GetILGenerator();
            currentIL.Emit(OpCodes.Ret);

            //Se agrega el método recién creado a la lista de mpetodos globales para no perder su referencia cuando se creen más métodos
            metodosGlobales.Add(currentMethodBldr);
            if (context.ident().GetText().Equals("main"))
            {
                //el puntero al metodo principal se setea cuando es el Main quien se declara
                pointMainBldr = currentMethodBldr;
            }

            return null;
        }

        public override object VisitFormParsAST(MiniCSharpParser.FormParsASTContext context)
        {
            for (int i = 0; i < context.type().Length; i++)
            {
                Visit(context.type(i));
                Visit(context.ident(i));
            }

            return null;
        }

        public override object VisitTypeAST(MiniCSharpParser.TypeASTContext context)
        {
            return verificarTipoRetorno(context.ident().GetText());
        }

        public override object VisitAssignStatementAST(MiniCSharpParser.AssignStatementASTContext context)
        {
            Visit(context.designator());
            if (context.expr() != null)
            {
                Visit(context.expr());
            }

            if (context.actPars() != null)
            {
                Visit(context.actPars());
            }

            return null;
        }

        public override object VisitIfStatementAST(MiniCSharpParser.IfStatementASTContext context)
        {
            //visit condition
            Visit(context.condition());
            
            //definir etiqueta
            ILGenerator currentIL = currentMethodBldr.GetILGenerator();
            Label labelFalse = currentIL.DefineLabel();
            
            //saltar if false
            currentIL.Emit(OpCodes.Brfalse,labelFalse);
            
            //Visit statement TRUE
            Visit(context.statement(0));
            
            //marcar etiqueta
            currentIL.MarkLabel(labelFalse);
            
            //Visit statement FALSE
            Visit(context.statement(1));

            return null;
        }

        public override object VisitForStatementAST(MiniCSharpParser.ForStatementASTContext context)
        {
            Visit(context.expr());
            
            // Generar la etiqueta de inicio del bucle
            ILGenerator currentIL = currentMethodBldr.GetILGenerator();
            Label loopStartLabel = currentIL.DefineLabel();
            currentIL.MarkLabel(loopStartLabel);
            
            // Si la condition existe, visita su subárbol 
            if (context.condition() != null)
            {
                Visit(context.condition());
            }
            
            // Generar la etiqueta de salida del bucle
            Label loopExitLabel = currentIL.DefineLabel();
                
            // Salto condicional al finalizar el bucle si la expresión es falsa
            currentIL.Emit(OpCodes.Brfalse, loopExitLabel);
            
            // Si statement existe, visita su subárbol y obtiene su tipo
            if (context.statement() != null)
            {
                Visit(context.statement(0));
            }

            if (context.statement().Length > 1)
            {
                Visit(context.statement(1));
            }
                
            // Salto incondicional al inicio del bucle
            currentIL.Emit(OpCodes.Br, loopStartLabel);
    
            // Marcar la etiqueta de salida del bucle
            currentIL.MarkLabel(loopExitLabel);

            return null;
        }

        public override object VisitWhileConditionStatementAST(MiniCSharpParser.WhileConditionStatementASTContext context)
        {
            // Generar la etiqueta de inicio del bucle
            ILGenerator currentIL = currentMethodBldr.GetILGenerator();
            Label loopStartLabel = currentIL.DefineLabel();
            currentIL.MarkLabel(loopStartLabel);
            
            // Visitar la condición del bucle
            Visit(context.condition());
            
            // Generar la etiqueta de salida del bucle
            Label loopExitLabel = currentIL.DefineLabel();
            
            // Salto condicional al finalizar el bucle si la expresión es falsa
            currentIL.Emit(OpCodes.Brfalse, loopExitLabel);
            
            // Visitar el statement del bucle
            Visit(context.statement());
            
            // Salto incondicional al inicio del bucle
            currentIL.Emit(OpCodes.Br, loopStartLabel);
    
            // Marcar la etiqueta de salida del bucle
            currentIL.MarkLabel(loopExitLabel);
            
            return null;
        }

        public override object VisitReturnStatementAST(MiniCSharpParser.ReturnStatementASTContext context)
        {
            if (context.expr() != null)
            {
                Visit(context.expr());
            }
            return null;
        }

        public override object VisitWhileNumberStatementAST(MiniCSharpParser.WhileNumberStatementASTContext context)
        {
            Visit(context.designator());
            return null;
        }

        public override object VisitWriteNumberStatementAST(MiniCSharpParser.WriteNumberStatementASTContext context)
        {
            Visit(context.expr());
            return null;
        }

        public override object VisitBlockStatementAST(MiniCSharpParser.BlockStatementASTContext context)
        {
            Visit(context.block());
            return null;
        }

        public override object VisitBlockAST(MiniCSharpParser.BlockASTContext context)
        {
            for (int i = 0; i < context.varDecl().Length; i++)
            {
                Visit(context.varDecl(i));
            }

            for (int i = 0; i < context.statement().Length; i++)
            {
                Visit(context.statement(i));
            }
            return null;
        }

        public override object VisitActParsAST(MiniCSharpParser.ActParsASTContext context)
        {
            for (int i = 0; i < context.expr().Length; i++)
            {
                Visit(context.expr(i));
            }

            return null;
        }

        public override object VisitConditionAST(MiniCSharpParser.ConditionASTContext context)
        {
            for (int i = 0; i < context.condTerm().Length; i++)
            {
                Visit(context.condTerm(i));
            }

            return null;
        }

        public override object VisitCondTermAST(MiniCSharpParser.CondTermASTContext context)
        {
            for (int i = 0; i < context.condFact().Length; i++)
            {
                Visit(context.condFact(i));
            }

            return null;
        }

        public override object VisitCondFactAST(MiniCSharpParser.CondFactASTContext context)
        {
            Visit(context.expr(0));
            Visit(context.expr(1));
            return null;
        }

        public override object VisitCastAST(MiniCSharpParser.CastASTContext context)
        {
            return Visit(context.type());
        }

        public override object VisitExprAST(MiniCSharpParser.ExprASTContext context)
        {
            Visit(context.term(0));
            for (int i = 1; i < context.term().Length; i++)
            {
                Visit(context.term(i));
                Visit(context.addop(i - 1));
            }

            return null;
        }

        public override object VisitTermAST(MiniCSharpParser.TermASTContext context)
        {
            Visit(context.factor(0));
            for (int i = 1; i < context.factor().Length; i++)
            {
                Visit(context.factor(i));
                Visit(context.mulop(i - 1));
            }

            return null;
        }

        public override object VisitDesignFactorAST(MiniCSharpParser.DesignFactorASTContext context)
        {
            Visit(context.designator());
            if (context.actPars() != null)
            {
                Visit(context.actPars());
            }

            return null;
        }

        public override object VisitCharconstFactorAST(MiniCSharpParser.CharconstFactorASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitStrconstFactorAST(MiniCSharpParser.StrconstFactorASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitIntFactorAST(MiniCSharpParser.IntFactorASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitDoubFactorAST(MiniCSharpParser.DoubFactorASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitBoolFactorAST(MiniCSharpParser.BoolFactorASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitNewIdentFactorAST(MiniCSharpParser.NewIdentFactorASTContext context)
        {
            Visit(context.ident());
            return null;
        }

        public override object VisitExprInparentFactorAST(MiniCSharpParser.ExprInparentFactorASTContext context)
        {
            return Visit(context.expr());
        }

        public override object VisitIntIdIdentAST(MiniCSharpParser.IntIdIdentASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitCharIdIdentAST(MiniCSharpParser.CharIdIdentASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitDoubIdIdentAST(MiniCSharpParser.DoubIdIdentASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitBoolIdIdentAST(MiniCSharpParser.BoolIdIdentASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitStrIdIdentAST(MiniCSharpParser.StrIdIdentASTContext context)
        {
            return verificarTipoRetorno(context.GetText());
        }

        public override object VisitIdentifierIdentAST(MiniCSharpParser.IdentifierIdentASTContext context)
        {
            return context.IDENTIFIER().Symbol;
        }

        public override object VisitListIdentAST(MiniCSharpParser.ListIdentASTContext context)
        {
            return  context.LIST().Symbol;
        }

        public override object VisitDesignatorAST(MiniCSharpParser.DesignatorASTContext context)
        {
            for (int i = 0; i < context.ident().Length; i++)
            {
                Visit(context.ident(i));
            }

            return null;
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