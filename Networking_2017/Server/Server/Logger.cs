using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Logger
    {
        public static void Message(string _format, params object[] _args)
        {
            Console.Write("[" + DateTime.Now + "] ");
            Console.Write(_format, _args);
            Console.WriteLine();
        }

        public static void Message(ConsoleColor _color, string _format, params object[] _args)
        {
            Console.Write("[" + DateTime.Now + "] ");

            // Color Change
            ConsoleColor prev = Console.BackgroundColor;
            Console.BackgroundColor = _color;
            // Write Error
            Console.Write(_format, _args);
            // Revert Color
            Console.BackgroundColor = prev;

            Console.WriteLine();
        }

        public static void Error(string _format, params object[] _args)
        {
            Message(ConsoleColor.DarkRed, _format, _args);
        }
    }
}
