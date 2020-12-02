using System;
using System.Collections.Generic;

namespace Duplify
{
    static class GlobalData
    {
        public static Dictionary<String, List<String>> hashes = new Dictionary<string, List<String>> { };

        public static string GlPath = @"D:\";
        public static bool FileSizeIgnore;
        public static bool HiddenFilesIgnore;

        public static string StatusBarText = "Запуск программы...";
        public static int ProgressBarValue = 0;
     
    }
}
