using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Ookii.Dialogs;

//codeshare: https://codeshare.io/dolY8

/* deserialize osu!mania file and store its data in a class
 * convert mania class to stepmania class
 * serialize stepmania class to file
 */

namespace osutostep
{
    public struct GroupConversionResult
    {
        public int TotalConversions;
        public int SuccessfulConversions;

        private List<ConversionResult> conversions;

        public void AddSuccessfulConversion(ConversionResult result)
        {
            if (conversions.Count >= TotalConversions)
            {
                return;
            }
            SuccessfulConversions++;
            conversions.Add(result);
        }

        public void AddFailedConversion(ConversionResult result)
        {
            if (conversions.Count >= TotalConversions)
            {
                return;
            }
            conversions.Add(result);
        }

        public GroupConversionResult(int totalConversions)
        {
            TotalConversions = totalConversions;
            SuccessfulConversions = 0;
            conversions = new List<ConversionResult>();
        }

        public void PrintConversionResults()
        {
            foreach (ConversionResult r in conversions)
            {
                if (r.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine(r.ToString());
                Console.ResetColor();
            }
            Console.WriteLine($"Successfully converted {SuccessfulConversions} out of {TotalConversions} beatmaps.");
        }
    }

    public struct ConversionResult
    {
        public bool Success;
        public string ConversionMessage;
        public string OsuFilePath;

        public ConversionResult(bool success, string message, string path)
        {
            Success = success;
            ConversionMessage = message;
            OsuFilePath = path;
        }

        public override string ToString()
        {
            return $"\tConverted file: {Path.GetFileName(OsuFilePath)}\n\tStatus: {(Success ? "Success" : "Failure")}\n\tMessage: {ConversionMessage}";
        }
    }

    public class O2SMain
    {
        private INIFile settingsFile;

        private string osuDirectory;
        private string stepDirectory;

        [STAThread]
        public static void Main(string[] args)
        {
            bool status = (new O2SMain()).run();
            if (!status)
            {
                Console.WriteLine("This program probably failed somewhere, that or you just quit\npress any key to exit");
                Console.ReadKey();
            }
            else
            {
                Console.ReadKey();
            }
        }

        //prompt user to find osu folder
        //creates config.ini with default settings
        private bool runSetupINIFileDialogs(out INIFile iniFile)
        {
            iniFile = new INIFile();

            VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();

            Console.WriteLine("Select osu! folder location (probably in \\AppData\\Local)");

            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            string osuPath = fbd.SelectedPath;

            if (!CheckOsuDirectory(osuPath))
            {
                Console.WriteLine($"osu!.exe not found in directory {osuPath}!");
                return false;
            }

            Console.WriteLine("osu!.exe and osu!mania output folder found!");

            iniFile.AddValue("osuDirectory", osuPath, "Directories");

            Console.WriteLine("Select the \"Songs\" folder in Stepmania");

            if (fbd.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            iniFile.AddValue("stepmaniaDirectory", fbd.SelectedPath, "Directories");

            Console.WriteLine($"Set the stepchart output parent directory to {fbd.SelectedPath} (map pack folder will be created)");

            iniFile.SerializeINI("config.ini");
            fbd.Dispose();
            return true;
        }

        private bool CheckOsuDirectory(string directory)
        {
            bool foundOsu = false;
            bool foundOutput = false;
            string[] files = Directory.GetFiles(directory);
            foreach (string f in files)
            {
                if (foundOsu |= Path.GetFileName(f).Equals("osu!.exe"))
                {
                    break;
                }
            }
            string[] folders = Directory.GetDirectories(directory);
            foreach (string f in folders)
            {
                if (foundOutput |= f.Equals(directory + "\\ManiaConverts"))
                {
                    break;
                }
            }
            return foundOsu && foundOutput;
        }

        private string[] getManiaConverFolders()
        {
            VistaFolderBrowserDialog fbdOsu = new VistaFolderBrowserDialog();

            fbdOsu.SelectedPath = osuDirectory + "\\ManiaConverts\\";
            if (fbdOsu.ShowDialog() != DialogResult.OK)
            {
                return null;
            }

            return fbdOsu.SelectedPaths;
        }

        private ConversionResult ConvertOsuManiaFile(string path, string folder)
        {
            OsuManiaMap ommap = new OsuManiaMap(path, folder);
            if (!ommap.Loaded)
            {
                return new ConversionResult(false, ommap.Reason, path);
            }

            Directory.CreateDirectory(stepDirectory + $"\\OsuConversions\\{ommap.FormatFolderName} [{ommap.Contents.DifficultyName}]");

            StepManiaMap smmap = new StepManiaMap(stepDirectory + $"\\OsuConversions\\{ommap.FormatFolderName} [{ommap.Contents.DifficultyName}]");
            bool result;
            try
            {
                result = ManiaConverter.Convert(ommap, ref smmap);
            }
            catch(Exception e)
            {
                return new ConversionResult(false, e.Message, path);
            }

            if (!result)
            {
                return new ConversionResult(false, ManiaConverter.Reason, path);
            }

            ManiaConverter.WriteStepchart(smmap, ommap);
            
            return new ConversionResult(true, $"Successfully converted file to location {smmap.CurrentDirectory}", path);
        }

        private GroupConversionResult ConvertGroup(string folder)
        {
            string[] allFiles = Directory.GetFiles(folder);
            int osuFileCount = 0;
            foreach (string file in allFiles)
            {
                if (file != null && Path.GetExtension(file).Equals(".osu"))
                {
                    osuFileCount++;
                }
            }
            GroupConversionResult results = new GroupConversionResult(osuFileCount);


            foreach (string file in allFiles)
            {
                if (file != null && Path.GetExtension(file).Equals(".osu"))
                {
                    ConversionResult result = ConvertOsuManiaFile(file, folder);
                    if (result.Success)
                    {
                        results.AddSuccessfulConversion(result);
                    }
                    else
                    {
                        results.AddFailedConversion(result);
                    }
                }
            }
            return results;
        }

        public bool run()
        {
            Console.WriteLine("~~~~~~~~~~~~Osu2Mania converter~~~~~~~~~~~~");
            settingsFile = new INIFile("config.ini");
            if (!settingsFile.LoadedSuccessfully)
            {
                Console.WriteLine("config.ini not found, running initial setup...");
                if (!runSetupINIFileDialogs(out settingsFile))
                {
                    return false;
                }
            }

            osuDirectory = settingsFile.GetValue("osuDirectory", "Directories");
            stepDirectory = settingsFile.GetValue("stepmaniaDirectory", "Directories");

            if (osuDirectory == null || stepDirectory == null)
            {
                throw new Exception("Bad config file");
            }
            Console.WriteLine($"Loaded configurations\nosu directory: {osuDirectory}\nstepmania directory: {stepDirectory}\nRequesting folders to convert");

            string[] folders = getManiaConverFolders();

            if (folders == null)
            {
                return false;
            }
            foreach (string folder in folders)
            {
                Console.WriteLine($"Converting all maps in folder: {folder}");
                try
                {
                    GroupConversionResult results = ConvertGroup(folder);
                    results.PrintConversionResults();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to convert maps from folder {folder}. Error message: {e.Message}");
                }
            }
            return true;
        }
    }
}