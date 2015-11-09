using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

public class UploadController : ApiController
{
    // Needs to be server specific as this needs to be the local directory of command line ocr windows application
    // https://github.com/suiliang/CommandLineOcr
    private static string UPLOAD_ROOT = @"C:\Users\Administrator\AppData\Local\Packages\2ca0072b-e230-42c2-a5f2-6ee47ccce84d_yekwsnrkhg0pr\LocalState\";
    private static TimeSpan UPLOAD_TTL = TimeSpan.FromDays(1);

    public async Task<HttpResponseMessage> PostFile()
    {
        // Check if the request contains multipart/form-data.
        if (!Request.Content.IsMimeMultipartContent())
        {
            throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
        }

        var provider = new MultipartFormDataStreamProvider(UPLOAD_ROOT);

        try
        {
            // Read the form data and return an async task.
            await Request.Content.ReadAsMultipartAsync(provider);

            if (provider.FileData.Count > 0)
            {
                JObject jObject = new JObject();
                foreach (MultipartFileData file in provider.FileData)
                {
                    string fileName = file.Headers.ContentDisposition.FileName.Split('"')[1];
                    if (!File.Exists(UPLOAD_ROOT + fileName))
                    {
                        File.Copy(file.LocalFileName, UPLOAD_ROOT + fileName);
                    }

                    string output = getOcr(fileName);
                    removeOldFiles();
                    return new HttpResponseMessage()
                    {
                        Content = new StringContent(output)
                    };
                }
            }
        }
        catch (System.Exception e)
        {
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
        }

        return new HttpResponseMessage()
        {
            Content = new StringContent("{}")
        };
    }

    //This class was built using info from this SO question 
    //http://stackoverflow.com/questions/12925748/iapplicationactivationmanageractivateapplication-in-c
    private static String getOcr(string fileName)
    {
        ApplicationActivationManager appActiveManager = new ApplicationActivationManager();//Class not registered
        uint pid;
        //The first arg in ActivateApplication is found in your registry at
        //HKEY_CURRENT_USER\Software\Classes\ActivatableClasses\Package\Some_Sort_Of_Guid\Server\App.App....\AppUserModelId
        appActiveManager.ActivateApplication("2ca0072b-e230-42c2-a5f2-6ee47ccce84d_yekwsnrkhg0pr!App", fileName, ActivateOptions.NoSplashScreen, out pid);
        System.Diagnostics.Process proc = null;
        foreach (var p in System.Diagnostics.Process.GetProcesses())
        {
            if (p.Id == pid)
            {
                proc = p;
            }
        }
        while (!proc.HasExited)
        {
            System.Threading.Thread.Sleep(100);
        }

        string outputPath = UPLOAD_ROOT + fileName + ".txt";
        if (File.Exists(outputPath))
        {
            Debug.Print(outputPath + " exists");
            return File.ReadAllText(outputPath);

        }
        Debug.Print(outputPath + " does not exists");
        return "{}";
    }

    private static void removeOldFiles()
    {
        var files = new DirectoryInfo(UPLOAD_ROOT).GetFiles("*.log");
        foreach (var file in files)
        {
            if (DateTime.UtcNow - file.CreationTimeUtc > UPLOAD_TTL)
            {
                File.Delete(file.FullName);
            }
        }
    }

}

public enum ActivateOptions
{
    None = 0x00000000,  // No flags set
    DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
    // to create an immersive window. Window creation must be done by design tools which
    // load the necessary components by communicating with a designer-specified service on
    // the site chain established on the activation manager.  The splash screen normally
    // shown when an application is activated will also not appear.  Most activations
    // will not use this flag.
    NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
    NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
}

[ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IApplicationActivationManager
{
    // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
    // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
    IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
    IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
    IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
}

[ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
class ApplicationActivationManager : IApplicationActivationManager
{
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
    public extern IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
    [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
    public extern IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
}