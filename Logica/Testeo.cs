using System;

namespace miniChartAlpha.Logica
{
    public class Testeo
    {
        private double d = 5.3;
        public int num;

        void imprimir(int pParameter)
        {
            Console.WriteLine(pParameter);
        }
        void imprimir2(string pParameter)
        {
            Console.WriteLine(pParameter);
        }

        void ejecutar()
        {
            int x, y;
            string h;
            h = "hola mundo";
            x = 10;
            y = 25;
            imprimir(y);
            imprimir2(h);
            
            Console.WriteLine(false);
        }
    }
}