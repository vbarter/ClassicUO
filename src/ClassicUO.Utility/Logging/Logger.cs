// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;

namespace ClassicUO.Utility.Logging
{
    public class Logger
    {
        // Mobile platforms have no terminal, reading or setting console colors throws there
        private static readonly bool SupportsConsoleColors = !OperatingSystem.IsIOS() && !OperatingSystem.IsAndroid();

        private static readonly Dictionary<LogTypes, Tuple<ConsoleColor, string>> _logTypesInfo = new Dictionary<LogTypes, Tuple<ConsoleColor, string>>
        {
            {
                LogTypes.None, Tuple.Create(ConsoleColor.White, "")
            },
            {
                LogTypes.Info, Tuple.Create(ConsoleColor.Green, "  Info    ")
            },
            {
                LogTypes.Debug, Tuple.Create(ConsoleColor.DarkGreen, "  Debug   ")
            },
            {
                LogTypes.Trace, Tuple.Create(ConsoleColor.Green, "  Trace   ")
            },
            {
                LogTypes.Warning, Tuple.Create(ConsoleColor.Yellow, "  Warning ")
            },
            {
                LogTypes.Error, Tuple.Create(ConsoleColor.Red, "  Error   ")
            },
            {
                LogTypes.Panic, Tuple.Create(ConsoleColor.Red, "  Panic   ")
            }
        };

        private int _indent;

        private bool _isLogging;
        private readonly object _syncObject = new object();

        // No volatile support for properties, let's use a private backing field.
        public LogTypes LogTypes { get; set; }

        public void Start(LogFile logFile = null)
        {
            _isLogging = true;
        }

        public void Stop()
        {
            _isLogging = false;
        }

        public void Message(LogTypes logType, string text)
        {
            lock (_syncObject)
            {
                SetLogger(logType, text);
            }
        }

        public void NewLine()
        {
            lock (_syncObject)
            {
                SetLogger(LogTypes.None, string.Empty);
            }
        }

        public void Clear()
        {
            if (SupportsConsoleColors)
            {
                Console.Clear();
            }
        }

        public void PushIndent()
        {
            _indent++;
        }

        public void PopIndent()
        {
            _indent--;

            if (_indent < 0)
            {
                _indent = 0;
            }
        }

        private void SetLogger(LogTypes type, string text)
        {
            if (!_isLogging)
            {
                return;
            }

            if ((LogTypes & type) == type)
            {
                if (type == LogTypes.None)
                {
                    if (_indent > 0)
                    {
                        Console.Write(new string('\t', _indent * 2));
                    }

                    Console.WriteLine(text);
                }
                else
                {
                    Console.Write(DateTime.UtcNow);
                    Console.Write(" | ");
                    if (SupportsConsoleColors)
                    {
                        ConsoleColor temp = Console.ForegroundColor;

                        Console.ForegroundColor = _logTypesInfo[type].Item1;
                        Console.Write(_logTypesInfo[type].Item2);
                        Console.ForegroundColor = temp;
                    }
                    else
                    {
                        Console.Write(_logTypesInfo[type].Item2);
                    }
                    Console.Write(" | ");

                    if (_indent > 0)
                    {
                        Console.Write(new string('\t', _indent * 2));
                    }

                    Console.WriteLine(text);
                }
            }
        }
    }
}