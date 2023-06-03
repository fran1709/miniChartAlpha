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

        private List<MethodBuilder> metodosGlobales; 
        
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
            return base.VisitUsignAST(context);
        }

        public override object VisitVarDeclaAST(MiniCSharpParser.VarDeclaASTContext context)
        {
            return base.VisitVarDeclaAST(context);
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
            }

            return null;
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
            return base.VisitFormParsAST(context);
        }

        public override object VisitTypeAST(MiniCSharpParser.TypeASTContext context)
        {
            return verificarTipoRetorno(context.ident().GetText());
        }

        public override object VisitAssignStatementAST(MiniCSharpParser.AssignStatementASTContext context)
        {
            return base.VisitAssignStatementAST(context);
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

        public override object VisitBreakStatementAST(MiniCSharpParser.BreakStatementASTContext context)
        {
            return base.VisitBreakStatementAST(context);
        }

        public override object VisitReturnStatementAST(MiniCSharpParser.ReturnStatementASTContext context)
        {
            return base.VisitReturnStatementAST(context);
        }

        public override object VisitWhileNumberStatementAST(MiniCSharpParser.WhileNumberStatementASTContext context)
        {
            return base.VisitWhileNumberStatementAST(context);
        }

        public override object VisitWriteNumberStatementAST(MiniCSharpParser.WriteNumberStatementASTContext context)
        {
            return base.VisitWriteNumberStatementAST(context);
        }

        public override object VisitBlockStatementAST(MiniCSharpParser.BlockStatementASTContext context)
        {
            return base.VisitBlockStatementAST(context);
        }

        public override object VisitSemicolonStatementAST(MiniCSharpParser.SemicolonStatementASTContext context)
        {
            return base.VisitSemicolonStatementAST(context);
        }

        public override object VisitBlockAST(MiniCSharpParser.BlockASTContext context)
        {
            return base.VisitBlockAST(context);
        }

        public override object VisitActParsAST(MiniCSharpParser.ActParsASTContext context)
        {
            return base.VisitActParsAST(context);
        }

        public override object VisitConditionAST(MiniCSharpParser.ConditionASTContext context)
        {
            return base.VisitConditionAST(context);
        }

        public override object VisitCondTermAST(MiniCSharpParser.CondTermASTContext context)
        {
            return base.VisitCondTermAST(context);
        }

        public override object VisitCondFactAST(MiniCSharpParser.CondFactASTContext context)
        {
            return base.VisitCondFactAST(context);
        }

        public override object VisitCastAST(MiniCSharpParser.CastASTContext context)
        {
            return base.VisitCastAST(context);
        }

        public override object VisitExprAST(MiniCSharpParser.ExprASTContext context)
        {
            return base.VisitExprAST(context);
        }

        public override object VisitTermAST(MiniCSharpParser.TermASTContext context)
        {
            return base.VisitTermAST(context);
        }

        public override object VisitDesignFactorAST(MiniCSharpParser.DesignFactorASTContext context)
        {
            return base.VisitDesignFactorAST(context);
        }

        public override object VisitCharconstFactorAST(MiniCSharpParser.CharconstFactorASTContext context)
        {
            return base.VisitCharconstFactorAST(context);
        }

        public override object VisitStrconstFactorAST(MiniCSharpParser.StrconstFactorASTContext context)
        {
            return base.VisitStrconstFactorAST(context);
        }

        public override object VisitIntFactorAST(MiniCSharpParser.IntFactorASTContext context)
        {
            return base.VisitIntFactorAST(context);
        }

        public override object VisitDoubFactorAST(MiniCSharpParser.DoubFactorASTContext context)
        {
            return base.VisitDoubFactorAST(context);
        }

        public override object VisitBoolFactorAST(MiniCSharpParser.BoolFactorASTContext context)
        {
            return base.VisitBoolFactorAST(context);
        }

        public override object VisitNewIdentFactorAST(MiniCSharpParser.NewIdentFactorASTContext context)
        {
            return base.VisitNewIdentFactorAST(context);
        }

        public override object VisitExprInparentFactorAST(MiniCSharpParser.ExprInparentFactorASTContext context)
        {
            return base.VisitExprInparentFactorAST(context);
        }

        public override object VisitIntIdIdentAST(MiniCSharpParser.IntIdIdentASTContext context)
        {
            return base.VisitIntIdIdentAST(context);
        }

        public override object VisitCharIdIdentAST(MiniCSharpParser.CharIdIdentASTContext context)
        {
            return base.VisitCharIdIdentAST(context);
        }

        public override object VisitDoubIdIdentAST(MiniCSharpParser.DoubIdIdentASTContext context)
        {
            return base.VisitDoubIdIdentAST(context);
        }

        public override object VisitBoolIdIdentAST(MiniCSharpParser.BoolIdIdentASTContext context)
        {
            return base.VisitBoolIdIdentAST(context);
        }

        public override object VisitStrIdIdentAST(MiniCSharpParser.StrIdIdentASTContext context)
        {
            return base.VisitStrIdIdentAST(context);
        }

        public override object VisitIdentifierIdentAST(MiniCSharpParser.IdentifierIdentASTContext context)
        {
            return base.VisitIdentifierIdentAST(context);
        }

        public override object VisitListIdentAST(MiniCSharpParser.ListIdentASTContext context)
        {
            return base.VisitListIdentAST(context);
        }

        public override object VisitDesignatorAST(MiniCSharpParser.DesignatorASTContext context)
        {
            return base.VisitDesignatorAST(context);
        }

        public override object VisitRelop(MiniCSharpParser.RelopContext context)
        {
            return base.VisitRelop(context);
        }

        public override object VisitAddop(MiniCSharpParser.AddopContext context)
        {
            return base.VisitAddop(context);
        }

        public override object VisitMulop(MiniCSharpParser.MulopContext context)
        {
            return base.VisitMulop(context);
        }
    }
}