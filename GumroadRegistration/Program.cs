using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.IO;

namespace GumroadRegistration
{
    class Program
    {
        static ManualResetEvent resetEvent = new ManualResetEvent(false);
        private static readonly HttpClient client = new HttpClient();
        private static string responseString = "{'success':false}";

        //Ini Gumroad data
        private static string URL = "https://api.gumroad.com/v2/licenses/verify";
        private static string ProductID = "NLIib";
        private static string LicenseKey = "";
        private static bool isValid = false;
        private static int attempt = 0;
        private static int uses = 0;
        private static int quantity = 0;
        private static int maxUses = 0;
        private static int maxUsesPerLicense = 3;
        private static string UserFolder = "";
        private static string AMK_Dir = "";
        private static string[] HiddenKeys = {"S89C7AD0","8HG056DG","K9W7FS09","Y204IH93","F83C28GH","1VD9ED96","V0Y2F86F","32IB56FS","7TF55DFT","TY8Y8U0P","VI8953RT","L053FT2A","8G4KLC89","Y9C7GH15","P0P3Y9SQ"};


        static void Main(string[] args)
        {
            Console.WriteLine("************************************************");
            Console.WriteLine("Gumroad License setup .....");
            Console.WriteLine("************************************************");
            CheckLicense();
            resetEvent.WaitOne();
        }

        static async void SendPostRequest()
        {
            //License key decrypt
            string[] KeyParts = LicenseKey.Split('-');
            int idx = 0;
            foreach(string Part in KeyParts)
            {
                idx++;
                if(HiddenKeys.Contains(Part))
                {
                    KeyParts = KeyParts.Where(val => val != Part).ToArray();
                    break;
                }
                if(idx == KeyParts.Length)
                {
                    idx = 3;
                }
            }
            maxUsesPerLicense = idx;
            LicenseKey = string.Join("-", KeyParts);

            var parameters = new Dictionary<string, string> { { "product_permalink", ProductID }, { "license_key", LicenseKey } };
            var formData = new FormUrlEncodedContent(parameters);
            var response = await client.PostAsync(URL, formData);
            responseString = await response.Content.ReadAsStringAsync();
            CheckLicense();
        }

        static void CheckLicense()
        {
            dynamic responseObj;
            responseObj = JToken.Parse(responseString);
            if (responseObj.success == true)
                isValid = true;
                
            if (isValid == false)
            {
                Console.WriteLine(attempt == 0 ? "Enter License Key:" : "Invalid License Key, Re-enter Key:");
                LicenseKey = Console.ReadLine();
                SendPostRequest();
                attempt++;
            }
            else
            {
                uses = responseObj.uses;
                quantity = responseObj.purchase.quantity;
                maxUses = quantity * maxUsesPerLicense;
                if(uses > maxUses)
                {
                    Console.WriteLine("This License key exceeded it's maximum number of uses("+maxUses+"), try another one.....");
                    responseString = "{'success':false}";
                    isValid = false;
                    LicenseKey = Console.ReadLine();
                    SendPostRequest();
                }
                else
                {
                    Console.WriteLine("Configuring Asset Manager..........");
                    //Make changes to code here
                    UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    AMK_Dir = UserFolder + "\\.config\\store\\amk";
                    if (!Directory.Exists(AMK_Dir))
                    {
                        Directory.CreateDirectory(AMK_Dir);
                    }
                    ValidateFiles();
                    Console.WriteLine("Asset Manager is all set, you can start using it now");
                    Console.ReadLine();
                    resetEvent.Set();
                }
            }
        }

        static void ValidateFiles()
        {
            string CurrentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string UserFolderName = Path.GetFileName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            string file;
            string matchString;
            string replaceString;

            file = CurrentPath + "\\AssetManager.mel";
            matchString = "substring($amsScriptRootPath,10,10)!=\"\"";
            replaceString = "substring($amsScriptRootPath,10,10)!=\""+ UserFolderName[0] +"\"";
            ReplaceInFile(file, matchString, replaceString);

            file = CurrentPath + "\\PlaceTool.mel";
            matchString = "substring($ptRootPath,10,10)!=\"\"";
            replaceString = "substring($ptRootPath,10,10)!=\"" + UserFolderName[0] + "\"";
            ReplaceInFile(file, matchString, replaceString);

            file = CurrentPath + "\\HelperFuncs.py";
            matchString = "scriptPath[9]!=\"\"";
            replaceString = "scriptPath[9]!=\"" + UserFolderName[0] + "\"";
            ReplaceInFile(file, matchString, replaceString);

        }

        static void ReplaceInFile(string filePath, string matchString, string replaceString)
        {
            string text = File.ReadAllText(filePath);
            text = text.Replace(matchString, replaceString);
            File.WriteAllText(filePath, text);
        }
    }
}
