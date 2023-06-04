using System;
using System.Collections.Generic;
using System.Linq;
using miniChartAlpha.Logica.TypeManager;

namespace miniChartAlpha.Logica
{

    public class CSTablaSimbolos
    {
        static LinkedList<Object> tabla;
        public int nivelActual;

        public CSTablaSimbolos()
        {
            tabla = new LinkedList<Object>();
            nivelActual = -1;
        }

        public void insertar(Tipo ident)
        {
            ident.nivel = nivelActual;
            tabla.AddLast(ident);
        }

        public T buscarObjetoTipo<T>(string nombre) where T : Tipo
        {
            T localVar = null;
            T globalVar = null;
            foreach (object id in tabla)
            {
                T obj = id as T;
                if (obj != null)
                {
                    if (obj.tok != null && obj.tok.Text.Equals(nombre))
                    {
                        if (obj.nivel == nivelActual)
                        {
                            return obj;
                        }

                        if (localVar == null)
                        {
                            globalVar = obj;
                        }
                    }

                    if (obj.tok == null && obj.MethodNombre.Equals(nombre))
                    {
                        if (obj.nivel == nivelActual)
                        {
                            return obj;
                        }

                        if (localVar == null)
                        {
                            globalVar = obj;
                        }
                    }
                }
            }

            return localVar ?? globalVar;
        }

        public void CastVariable(string nombre, int nuevoType)
        {
            foreach (object id in tabla)
            {
                if (id is TipoBasico variable && variable.tok.Text == nombre)
                {
                    variable.tipoDato = nuevoType;
                    return;
                }
            }
        }

        public void openScope()
        {
            nivelActual++;
        }

        public void CloseScope()
        {
            foreach (var item in tabla.ToList())
            {
                if (((Tipo)item).nivel == nivelActual)
                {
                    tabla.Remove(item);
                }
            }

            nivelActual--;
        }

        public void Imprimir()
        {
            System.Diagnostics.Debug.WriteLine("----- INICIO TABLA ------");
            //consola.SalidaConsola.Text += "----- INICIO TABLA ------\n";
            foreach (object id in tabla)
            {
                System.Diagnostics.Debug.WriteLine(id.ToString());
                //consola.SalidaConsola.Text += id.ToString();
            }

            System.Diagnostics.Debug.WriteLine("----- FIN TABLA ------");
            //consola.SalidaConsola.Text += "----- FIN TABLA ------\n";
        }
    }
}