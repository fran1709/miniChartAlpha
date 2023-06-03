using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Antlr4.Runtime;
using Microsoft.Win32;
using miniChartAlpha.Logica;

namespace miniChartAlpha
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    ///
    /// Esta interfaz fue creada por Julio Cesar Castro Y Josue Orozco
    /// Se utilizo el lenguaje de programacion C# y el framework de WPF
    /// Tome en cuenta que esta interfaz es solo una propuesta, puede ser modificada
    /// Las funciones aqui implementadas son solo para mostrar como se puede implementar
    /// Las mismas estan vacias dentro de ellas esta el nombre de la funcion y un comentario que indica que es lo que hace la funcion
    /// 
    /// 
    /// </summary>
    public partial class MainWindow
    {
        private Type pointType;
        private object ptInstance;
        private CodeGen visitor;
        public MainWindow() 
        {
            // El System.Diagnostics.Debug.WriteLine es para imprimir en la consola del debugger.
            System.Diagnostics.Debug.WriteLine("System Diagnostics Debug");
            
            InitializeComponent(); 
        }
        private void Add_Tab_Button_Click(object sender, EventArgs e)
        {
            // Aqui va la logica para agregar una nueva pestaña
            // Al agregar la nueva pestaña tome en consideracion
            // que se debe agregar un nuevo TabItem y un nuevo TextBox

            // Crear un nuevo TabItem con un TextBox
            TabItem tabItem = new TabItem();
            tabItem.Header = "New Tab";
            TextBox textBox = new TextBox();
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            textBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            tabItem.Content = textBox;
            Tab.Items.Add(tabItem);

            // Seleccionar la nueva pestaña
            Tab.SelectedItem = tabItem;
            //textB <- text0 que viene en el archivo
            //tabitem <- textB
            //Tab <- tabitem 
            // dentro del TabControl y que dentro del textbox se agrega el texto del archivo.txt que se subio
        }
        
        private void closeButton_Click(object sender, EventArgs e)
        {
            // Eliminar la pestaña seleccionada TabItem
            // Elimina una pestaña del TabControl, tome en cuenta que al eliminar una pestaña 
            // tambien se debe eliminar el TextBox que esta dentro del TabControl
            Tab.Items.Remove(Tab.SelectedItem);
        }

        private void Pantalla_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCursorPosition();
        }
        private void Pantalla_TextChanged(object sender, EventArgs e)
        {
            UpdateCursorPosition();
        }   
        
        private void UpdateCursorPosition()
        {
            int index = Pantalla.SelectionStart;
            int line = Pantalla.GetLineIndexFromCharacterIndex(index) + 1;
            int column = index - Pantalla.GetCharacterIndexFromLineIndex(Pantalla.GetLineIndexFromCharacterIndex(index)) + 1;
         
            // Actualiza el texto de un label o de otro TextBox con el número de línea y columna.
            Output.Content = $"Línea: {line} \nColumna: {column}";
        }
        public void Upload_File_Button_Click(object sender, RoutedEventArgs e) 
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                string fileContent = File.ReadAllText(fileName);

                TabItem activeTab = (TabItem)Tab.SelectedItem;
                var onlyFileName = Path.GetFileName(fileName);
                activeTab.Header = onlyFileName;
                TextBox textBox = (TextBox)activeTab.Content;
                textBox.Text = fileContent;
            }else
            {
                // Manejar el caso en que el usuario no seleccionó un archivo
                Consola consola = new Consola();
                consola.SalidaConsola.Text = "Archivo no seleccionado!";
                consola.Show();
            }
        }
        private void Run_Button_Click(object sender, RoutedEventArgs e) 
        {
            // Aqui va la logica para correr el codigo
            // AL correr el codigo tome en consideracion
            // que el resultado de la c se muestra en una nueva ventana llamada Consola
            // en el texbox SalidaConsola
            TabItem activeTab = (TabItem)Tab.SelectedItem;
            TextBox textBox = (TextBox)activeTab.Content;
            RunMiniChart(CharStreams.fromString(textBox.Text.ToLower()));
        }
        
        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Exit Button Clicked");
            Application.Current.Shutdown();
        }
        
        private void RunMiniChart(ICharStream pCode)
        {
            //ICharStream inputStream = CharStreams.fromPath(@"C:\Users\Mariana Artavia Vene\Documents\I SEMESTRE 2023\Compiladores e Interpretes\ConsoleCompi\ConsoleCompi\test.txt");
            var lexer = new MiniCSharpScanner(pCode);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            MiniCSharpParser parser = new MiniCSharpParser(tokens);
            var errorListener = new MyErrorListener();
            
            parser.ErrorHandler = new MyErrorStrategy();
            // Asigna el ErrorListener personalizado
            // al parser.
            parser.RemoveErrorListeners(); // Elimina el ErrorListener predeterminado.
            parser.AddErrorListener(errorListener);
            
            var treeContext = parser.program();
            
            Consola consola = new Consola();   
            if (parser.NumberOfSyntaxErrors > 0)
            {
                Resultado.Content = "Compilación fallida: " + parser.NumberOfSyntaxErrors +
                                    " error(es) de sintaxis encontrados:";


                consola.SalidaConsola.Text = Resultado.Content.ToString() + "\n";
                foreach (string error in errorListener.SyntaxErrors)
                {
                    consola.SalidaConsola.Text += error + "\n";
                }
                consola.Show();
            }
            else
            {   
                //Ejecución de analisis contextual
                var mv = new CSharpContextual();
                mv.Visit(treeContext);
                
                visitor = new CodeGen();

                // Tercera etapa
                pointType = (Type)visitor.Visit(treeContext);
                ptInstance = Activator.CreateInstance(pointType);
                pointType.InvokeMember("main",
                    BindingFlags.InvokeMethod,
                    null,
                    ptInstance,
                    new object[0]);
                /*Process myProcess = new Process();
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = @"../../bin/Debug/test.exe";
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.Start();
                            
                myProcess.StandardOutput.ReadToEnd();
                myProcess.WaitForExit();*/
                
            }
        }
    }
}