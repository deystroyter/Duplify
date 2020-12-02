using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;


namespace Duplify
{
    class DuFinder
    {
        private static List<string> FilesList = new List<string> { };


        private static int FilesCalculated = 0; //сколько обработано файлов
        private static int TotalFiles = 1; //сколько всего файлов

        public static void MainProgram()
        {
            FilesList.Clear();
            GlobalData.hashes.Clear();
            FilesCalculated = 0;
            TotalFiles = 1;

            ProgressChanger("Запуск программы...");
            Travarse(GlobalData.GlPath);


            TotalFiles = FilesList.Count;
            foreach (string file in FilesList)
            {
                CalculateMD5(file);
            }

        }


        //Обход дерева
        private static void Travarse(String workingDirectory)
        {
            try
            {
                String[] files = Directory.GetFiles(workingDirectory);
                String[] subDirectories = Directory.GetDirectories(workingDirectory);

                foreach (String file in files) //для каждого файла в папке
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);

                        //Влияние HiddenFilesCheckBox
                        if (GlobalData.HiddenFilesIgnore == true) 
                        {
                            try
                            {
                                FileAttributes attributes = File.GetAttributes(file);
                                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                                {
                                    ProgressChanger("Hidden file...");
                                    continue;
                                }
                                else if ((attributes & FileAttributes.System) == FileAttributes.System)
                                {
                                    ProgressChanger("System file...");
                                    Console.Beep();
                                    continue;
                                }
                            }
                            catch (DirectoryNotFoundException)
                            {
                                ProgressChanger("DirectoryNotFoundException...");
                            }
                        }

                        //Влияние FileSizeCheckBox
                        if (GlobalData.FileSizeIgnore && Math.Round((double)(fileInfo.Length / 1024)) < 5)
                        {
                            continue;
                        }
                        FilesList.Add(file);
                        ProgressChanger("Идет подсчёт файлов (" + FilesList.Count +")");
                    }
                    catch (FileNotFoundException)
                    {
                        ProgressChanger("FileNotFoundException...");
                    }
                }

                foreach (String subDirectory in subDirectories) //для каждой подпапки в папке
                {
                    Travarse(subDirectory);
                }
            }
            catch (UnauthorizedAccessException)
            {
                ProgressChanger("UnauthorizedAccessExpetion: " + workingDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                ProgressChanger("DirectoryNotFoundException");
            }


        }

        //Изменяет StatusBarText и ProgressBarValue
        public static void ProgressChanger(string text)
        {
            //если текст не помещается в StatusBarText
            if (text.Length >= 50)
            {
                GlobalData.StatusBarText = text.Substring(0, 23) + "......" + text.Substring(text.Length - 23, 23);
            }

            else GlobalData.StatusBarText = text;

            //вычисляем выполненный процент
            GlobalData.ProgressBarValue = (int)Math.Floor((double)FilesCalculated*100/TotalFiles);
        }

        //Хеширование MD5
        private static void CalculateMD5(String filename)
        {
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);

                        String hashString = BitConverter.ToString(hash);

                        //пробуем добавить значение по ключу
                        try
                        {
                            GlobalData.hashes[hashString].Add(filename);


                        }
                        catch (NullReferenceException)
                        {
                            GlobalData.hashes[hashString] = new List<String> { };
                            GlobalData.hashes[hashString].Add(filename);
                        }
                        //если такого ключа ещё не существует
                        catch (KeyNotFoundException)
                        {

                            GlobalData.hashes[hashString] = new List<String> { };
                            GlobalData.hashes[hashString].Add(filename);
                        }
                        FilesCalculated++;
                        ProgressChanger(filename);
                    }
                }
                catch (DirectoryNotFoundException) { ProgressChanger("MD5 DirectoryNotFoundException..."); }
                catch (FileNotFoundException) { ProgressChanger("MD5 FileNotFoundException..."); }



            
            }
        }

    }
}