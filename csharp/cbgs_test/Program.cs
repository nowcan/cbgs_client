using System;

namespace cbgs_test
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4 && args.Length != 6 && args.Length != 8)
            {
                Console.WriteLine("cbgs_test <CBGS server> <CBGS game type ( CCH | DXQ | OTH )> [[CBGS user] [CBGS password]] <initial time ( minutes ) > <increament time ( seconds )> [<engine file> <engine protocol ( uci | ucci | auto )>]");
            }
            else
            {
                cbgs_main_cch test_cch = new cbgs_main_cch();
                cbgs_main_dxq test_dxq = new cbgs_main_dxq();
                cbgs_main_oth test_oth = new cbgs_main_oth();
                switch (args[1].ToUpper())
                {
                    case "CCH":
                        if (args.Length == 8)
                        {
                            if (test_cch.run(args[0], args[2], args[3], args[4], args[5], args[6], args[7]) == false)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (test_cch.run(args[0], args[2], args[3], args[4], args[5]) == false)
                            {
                                return;
                            }

                        }
                        break;

                    case "DXQ":
                        if (args.Length == 6)
                        {
                            if (test_dxq.run(args[0], args[2], args[3], args[4], args[5]) == false)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (test_dxq.run(args[0], args[2], args[3]) == false)
                            {
                                return;
                            }
                        }
                        break;

                    case "OTH":
                        if (args.Length == 6)
                        {
                            if (test_oth.run(args[0], args[2], args[3], args[4], args[5]) == false)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (test_oth.run(args[0], args[2], args[3]) == false)
                            {
                                return;
                            }
                        }
                        break;
                }

                ConsoleKeyInfo cki;

                // Establish an event handler to process key press events.
                Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
                while (true)
                {
                    Console.WriteLine("Press 'X' key to quit");

                    // Start a console read operation. Do not display the input.
                    cki = Console.ReadKey(true);

                    if (cki.Key == ConsoleKey.O)
                    {
                        switch (args[3].ToUpper())
                        {
                            case "CCH":
                                test_cch.test_board_ocr();
                                break;
                        }
                    }

                    // Exit if the user pressed the 'X' key.
                    if (cki.Key == ConsoleKey.X) break;
                }

                switch (args[3].ToUpper())
                {
                    case "CCH":
                        test_cch.quit();
                        break;

                    case "DXQ":
                        test_dxq.quit();
                        break;

                    case "OTH":
                        test_oth.quit();
                        break;
                }
            }
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            Console.WriteLine("Press 'X' key to quit");
            args.Cancel = true;
        }

    }
}
