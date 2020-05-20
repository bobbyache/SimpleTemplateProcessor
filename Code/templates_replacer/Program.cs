using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace templates_replacer
{
    class Program
    {
        private static string prefix = "";
        private static string postfix = "";
        private static string urlEncodedPrefix = "";
        private static string urlEncodedPostfix = "";

        private static void FetchPlaceFixers(XDocument document)
        {
            var normalPrefixElement = document.Element("Settings").Element("Prefixes").Elements("Prefix")
                .Where(p => p.Attribute("Id").Value == "NORMAL").SingleOrDefault();
            prefix = normalPrefixElement.Attribute("Prefix").Value;
            postfix = normalPrefixElement.Attribute("Postfix").Value;

            var urlEncodedPrefixElement = document.Element("Settings").Element("Prefixes").Elements("Prefix")
                .Where(p => p.Attribute("Id").Value == "HTMLENC").SingleOrDefault();
            urlEncodedPrefix = urlEncodedPrefixElement.Attribute("Prefix").Value;
            urlEncodedPostfix = urlEncodedPrefixElement.Attribute("Postfix").Value;
        }

        static void Main(string[] args)
        {
            XDocument document = XDocument.Load("Options.xml");
            FetchPlaceFixers(document);
            var options = GetConfiguredOptions(document);
            
            var result = GetOptionInput(options).ToString();
            if (result == "C")
                return;

            var option = options.Where(opt => opt.Id == result).SingleOrDefault();
            if (option != null)
            {
                ProcessOption(option);
            }
        }

        private static string GetOptionInput(Option[] options, string lastOption = null)
        {
            if (lastOption != null)
            {
                Console.WriteLine("");
                Console.WriteLine("That option isn't valid...");
                Console.WriteLine("");
            }

            Console.WriteLine("");
            Console.WriteLine("What action would you like to take?");

            foreach (Option option in options)
            {
                Console.WriteLine($"\t[{option.Id}]. {option.Name}");
            }

            Console.WriteLine("\t[C]. Cancel");
            Console.WriteLine("");
            Console.Write("Make your choice: ");

            var result = Console.ReadLine();

            if (result.ToLower() == "c")
                return "C";
            else if (options.Any((opt) => opt.Id == result))
                return result;
            else
                return GetOptionInput(options, result);
        }

        private static Option[] GetConfiguredOptions(XDocument document)
        {
            var options = document.Element("Settings").Element("Options").Elements("Option").Select((opt) => 
                new Option
                {
                    Id = opt.Attribute("Id").Value,
                    Name = opt.Attribute("Name").Value,
                    TemplateFolder = opt.Attribute("TemplateFolder").Value,
                    OutputFolder = opt.Attribute("OutputFolder").Value,
                    VariableFile = opt.Attribute("VariableFile").Value,
                    SearchPattern = opt.Attribute("SearchPattern").Value
                }
            );
            return options.ToArray();
        }

        private class Option
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string TemplateFolder { get; set; }
            public string OutputFolder { get; set; }

            public string VariableFile { get; set; }

            public string SearchPattern { get; set; }
        }

        private static void ProcessOption(Option option)
        {
            try
            {
                List<KeyValuePair<string, string>> variables = GetContents(option.VariableFile);
                IEnumerable<string> templateFiles = Directory.EnumerateFiles(option.TemplateFolder, option.SearchPattern);

                if (!Directory.Exists(option.OutputFolder))
                {
                    Directory.CreateDirectory(option.OutputFolder);
                }

                foreach (string templateFile in templateFiles)
                {
                    string resultText = GetTemplateFileText(templateFile);

                    foreach (var variable in variables)
                    {
                        resultText = resultText.Replace(urlEncodedPrefix + variable.Key + urlEncodedPostfix, HttpUtility.HtmlEncode(variable.Value));
                        resultText = resultText.Replace(prefix + variable.Key + postfix, variable.Value);
                    }
                    SaveText(resultText, Path.Combine(option.OutputFolder, Path.GetFileName(templateFile)));
                }

                Console.WriteLine("Done");
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED TO WRITE TO EXECUTE");
            }
        }

        private static void SaveText(string fileText, string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(fileText);
                streamWriter.Flush();
            }
        }

        private static string GetTemplateFileText(string filePath)
        {
            string contents = null;
            
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    contents = sr.ReadToEnd();
                }
            }
            return contents;
        }

        private static List<KeyValuePair<string, string>> GetContents(string filePath)
        {
            List<KeyValuePair<string, string>> idList = new List<KeyValuePair<string, string>>();

            if (File.Exists(filePath))
            {
                using (StreamReader streamReader = File.OpenText(filePath))
                {
                    string input = null;
                    while ((input = streamReader.ReadLine()) != null)
                    {
                        if (!input.Trim().StartsWith("#"))
                        {
                            var keyVal = input.Trim().Split('=', StringSplitOptions.RemoveEmptyEntries);
                            idList.Add(new KeyValuePair<string, string>(keyVal[0], keyVal[1]));
                        }

                    }
                }
            }
            return idList;

        }
    }
}
