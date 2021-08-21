using System;

namespace smIRCL.Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Example selection:");
            Console.WriteLine("1. Connect to Libera Chat respond to hello");

            Console.Write("\n> ");
            
            bool selectionSuccess = Int32.TryParse(Console.ReadLine(), out int selection);

            switch (!selectionSuccess ? 0 : selection)
            {
                case 1:
                    Example1.Run();
                    break;
                
                default:
                    Console.WriteLine("Invalid selection");
                    break;
            }
        }
    }
}