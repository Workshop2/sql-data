using System;
using System.Configuration;
using SqlData.Core;

namespace SqlData.Console.Tool
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["database-target"].ConnectionString;
            string directory = ConfigurationManager.AppSettings["directory-target"];

            while (true)
            {
                ConsoleKeyInfo input = GetInput();

                try
                {
                    bool canContinue = ExecuteInput(input, connectionString, directory);

                    if (!canContinue)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("ERROR: " + ex.Message);
                }

                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine("All done :)");
                System.Console.ReadKey();
            }
        }

        private static ConsoleKeyInfo GetInput()
        {
            System.Console.Clear();
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine("What would you like to do?");

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("1= DataToFile");
            System.Console.WriteLine("2= Wipe Data");
            System.Console.WriteLine("3= DataToSql");
            System.Console.WriteLine("4= Exit");
            System.Console.ForegroundColor = ConsoleColor.Yellow;

            ConsoleKeyInfo input = System.Console.ReadKey();
            return input;
        }

        private static bool ExecuteInput(ConsoleKeyInfo input, string connectionString, string directory)
        {
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine();

            switch (input.KeyChar)
            {
                case '1':
                    System.Console.WriteLine("Storing database \n   {0} \nTo directory \n  {1}", connectionString, directory);

                    DataToFile dataToFile = new DataToFile(connectionString, directory);
                    dataToFile.Execute();
                    break;
                case '2':
                    System.Console.WriteLine("Wiping database \n    {0}", connectionString);
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    System.Console.WriteLine("Are you sure you want to continue? Y/N");
                    ConsoleKeyInfo confirmInfo = System.Console.ReadKey();
                    System.Console.WriteLine();
                    System.Console.ForegroundColor = ConsoleColor.Cyan;

                    if (confirmInfo.KeyChar == 'Y' || confirmInfo.KeyChar == 'y')
                    {
                        DataWiper dataWiper = new DataWiper(connectionString);
                        dataWiper.Execute();
                    }
                    else
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine("Aborted");
                    }

                    break;
                case '3':
                    System.Console.WriteLine("Deploying data to database \n {0} From \n {1}", connectionString, directory);

                    DataToSql dataToSql = new DataToSql(connectionString, directory);
                    dataToSql.Execute();
                    break;
                case '4':
                    return false;
                default:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    System.Console.WriteLine("Incorrect input");
                    break;
            }

            return true;
        }
    }
}
