using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UKeySender
{
    public class Programm
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        const int SW_RESTORE = 9; // Restores the window if it is minimized.

        static void FocusWindow(IntPtr hWnd)
        {
            // Check if the window is minimized and restore it if it is.
            ShowWindow(hWnd, SW_RESTORE);
            // Set the window to be focused
            SetForegroundWindow(hWnd);
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            internal uint type;
            internal InputUnion U;
            internal static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT)); }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)]
            internal MOUSEINPUT mi;
            [FieldOffset(0)]
            internal KEYBDINPUT ki;
            [FieldOffset(0)]
            internal HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal uint mouseData;
            internal uint dwFlags;
            internal uint time;
            internal IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal ushort wVk;
            internal ushort wScan;
            internal uint dwFlags;
            internal uint time;
            internal IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            internal uint uMsg;
            internal ushort wParamL;
            internal ushort wParamH;    
        }

        internal const uint INPUT_KEYBOARD = 1;
        internal const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendKey(ushort key)
        {
            INPUT[] inputs =
            {
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = 0,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                },
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = key,
                            wScan = 0,
                            dwFlags = KEYEVENTF_KEYUP,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                }
            };

            SendInput((uint)inputs.Length, inputs, INPUT.Size);
        }

        static void SendKeyToWindow(string windowTitle, ushort key)
        {
           IntPtr hWnd = FindWindow(null, windowTitle);
            if (hWnd == IntPtr.Zero)
            {
                fatal("Window not found: " + windowTitle);
                return;
            }

            // Only set the window to foreground if it's not already the foreground window
            IntPtr currentWindow = GetForegroundWindow();
            if (currentWindow != hWnd)
            {
                FocusWindow(hWnd);
            }

            Console.WriteLine($"\tSending key: {key.ToString("X")}");
            SendKey(key);
            // Allow time for the window to properly focus before sending input
            Thread.Sleep(120);

            if (currentWindow != hWnd && currentWindow != IntPtr.Zero)
            {
                Thread.Sleep(120);
                FocusWindow(currentWindow);
            }
        }

        static List<Tuple<int, ushort>>? ParseConfigFile(string filePath, out string windowTitle, out int finalDelay) {
            finalDelay = 0;
            windowTitle = "";
            if (!File.Exists(filePath))
            {
                fatal($"The key configuration file '{filePath}' was not found.");
                return null;
            }

            var linesFromFile = File.ReadAllLines(filePath);
            // Filter out empty lines and lines that are just comments
            var rawlines = linesFromFile
                .Select((line, index) => new { line, index })  // Project the line with its index
                .Where(x => !string.IsNullOrWhiteSpace(x.line) && !x.line.TrimStart().StartsWith(";"))
                .ToList();
            
            List<string> lines = new();

            foreach (var xLine in rawlines)
            {
                var line = xLine.line;
                var newLine = (line.Split(';').FirstOrDefault() ?? line).Trim();
                lines.Add(newLine);           
            }

            if (lines.Count < 3 || lines.Count % 2 != 0)
            {
                fatal($"The key configuration file '{filePath}' must contain a window title, followed by pairs of delay and keycode, ending with a delay.");
                return null;
            }

            windowTitle = lines[0];
            
            List<Tuple<int, ushort>> keyDelayPairs = new();
            int lastProcessedLine = 0;
            try
            {
                for (int i = 1; i < lines.Count - 1; i += 2)
                {
                    lastProcessedLine = i;
                    int delay;
                    ushort key;

                    // Parse the delay
                    if (!int.TryParse(lines[i], out delay))
                    {
                        fatal($"Error parsing delay: '{lines[i]}'");
                        return null;
                    }

                    // Parse the key code, assuming it's in hexadecimal format
                    string keyCodeHex = lines[i + 1].Trim(); // Trim any leading/trailing whitespace
                    try
                    {
                        // Convert the hexadecimal string to a decimal number
                        int keyCodeDecimal = Convert.ToInt32(keyCodeHex, 16);
                        if (keyCodeDecimal > ushort.MaxValue || keyCodeDecimal < 0)
                        {
                            fatal($"Key code out of range: '{lines[i + 1]}'");
                            return null;
                        }
                        key = (ushort)keyCodeDecimal;
                    }
                    catch (FormatException)
                    {
                        fatal($"Error parsing keycode: '{lines[i + 1]}'");
                        return null;
                    }
                    catch (OverflowException)
                    {
                        fatal($"Key code overflow: '{lines[i + 1]}'");
                        return null;
                    }

                    // Add the delay and key to the list
                    keyDelayPairs.Add(Tuple.Create(delay, key));
                }

                finalDelay = int.Parse(lines.Last()); // Get the last delay
                keyDelayPairs.Add(Tuple.Create(finalDelay, (ushort)0));
            }
            catch (Exception ex)
            {
                fatal($"Error occurred processing the following line: '{lines[lastProcessedLine]}': {ex.Message}");
                return null;
            }

            return keyDelayPairs;
        }

        static void printExampleConfig()
        {
            Console.WriteLine("Note: for a list of virtual key codes, just google: 'virtual key codes'.");
            Console.WriteLine("      Virtual key codes are in hexadecimal.");
            Console.WriteLine("");
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine("Example keys.txt:");
            Console.WriteLine("");
            Console.WriteLine("MyTargetWindow\t; The title of the target window");
            Console.WriteLine("");
            Console.WriteLine("; Following the delay key pairs below each other:");
            Console.WriteLine("200\t; Delay of 200 milliseconds");
            Console.WriteLine("9\t; Virtual key code for tab-key");
            Console.WriteLine("200\t; Another delay of 200 milliseconds");
            Console.WriteLine("31\t; Virtual key code for 1-key");
            Console.WriteLine("; The final delay. Basically before the sequence should start again:");
            Console.WriteLine("30000\t; Delay of 30000 milliseconds (30 seconds)");
            Console.WriteLine("--------------------------------------------------------------------");
        }

        static void fatal(string message)
        {
            Console.WriteLine($"<ERROR> {message}\n");
            printExampleConfig();
            Console.WriteLine();
            Environment.Exit(0);
        }

        public static void RunCommands()
        {
            Console.WriteLine("###########################################################");
            Console.WriteLine("#                 UNIVERSAL KEY SENDER                    #");
            Console.WriteLine("###########################################################");
            Console.WriteLine("");

            string filePath = "keys.txt";

            var keyDelayPairs = ParseConfigFile(filePath, out var windowTitle, out var finalDelay);

            if (keyDelayPairs == null) {
                fatal($"No keys found in: {filePath}");
                return;
            }

            if (string.IsNullOrWhiteSpace(windowTitle)) {
                fatal($"No window title found in: {filePath}");
                return;
            }

            if (finalDelay <= 0) finalDelay = 0;

            Console.WriteLine($"Target window set to: '{windowTitle}'");
            Console.WriteLine();

            int rotation = 1;
            while (true)
            {
                try
                {
                    Console.WriteLine($"Starting rotation #{rotation}");
                    Console.WriteLine($"---------------------------------");
                    foreach (var (delay, key) in keyDelayPairs)
                    {
                        if (key == 0)
                        {
                            break;
                        }
                        Console.WriteLine($"\tWaiting: {delay} milliseconds...");
                        Thread.Sleep(delay);
                        SendKeyToWindow(windowTitle,  key );
                    }

                    Console.WriteLine($"\tWaiting: {finalDelay} milliseconds...\n");
                    Thread.Sleep(finalDelay);
                    rotation++;
                }
                catch (Exception ex)
                {
                    fatal($"An error occurred during key sending: {ex.Message}");
                    return;
                }
            }
        }


        public static void Main()
        {
            RunCommands();
        }
    }
}
