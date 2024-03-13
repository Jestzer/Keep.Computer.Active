using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// For macOS. This needs to come before the EventHandler below.
Process? caffeinateProcess = null;

// Leaving so soon?
AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

Console.WriteLine("Releasing the chickens...");

string userMessage = string.Empty;
string versionNumber = string.Empty;
int processID = GetProcessIdByName("Teams", " | Microsoft Teams classic");
bool isUsingNewTeams = false;

// Needed to keep the computer from going to sleep on Windows.
const uint ES_CONTINUOUS = 0x80000000;
const uint ES_SYSTEM_REQUIRED = 0x00000001;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    while (true)
    {
        CheckIfTeamsIsRunning(out bool isTeamsRunning);
        if (isTeamsRunning)
        {
            CheckTeamsVersion(out string acceptedVersion, out versionNumber);

            if (acceptedVersion == "unsupported")
            {
                break;
            }

            while (acceptedVersion == "supported" && isTeamsRunning == true)
            {
                ChangeStateToActive();
                await Task.Delay(2000);
            }
        }
    }
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
    while (true)
    {
        CheckIfTeamsIsRunningMacOS(out bool isTeamsRunning);

        if (isTeamsRunning)
        {
            PreventSleepMacOS();
        }
    }
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Console.WriteLine("This application does not support Linux because there is no longer an official Teams application for Linux.");
    Environment.Exit(1);
}
else
{
    Console.WriteLine("Running on an unsupported or unknown platform. Exiting.");
    Environment.Exit(1);
}

[DllImport("kernel32.dll")]
static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

[DllImport("kernel32.dll")]
static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

[DllImport("kernel32.dll", SetLastError = true)]
static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

[DllImport("psapi.dll", SetLastError = true)]
static extern bool EnumProcessModulesEx(IntPtr hProcess, [Out] IntPtr[] lphModule, int cb, out int lpcbNeeded, uint dwFilterFlag);

[DllImport("psapi.dll")]
static extern uint GetModuleBaseName(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, int nSize);

[DllImport("kernel32.dll")]
static extern uint SetThreadExecutionState(uint esFlags);

const int PROCESS_WM_READ = 0x0010;
const int PROCESS_WM_WRITE = 0x0020;
const int PROCESS_VM_OPERATION = 0x0008;

byte valueToWrite = 18;

static int GetProcessIdByName(string processName, string windowTitleContains)
{
    // Get all processes with the specified name.
    Process[] processes = Process.GetProcessesByName(processName);
    foreach (var proc in processes)
    {
        // Check if the process has a main window title containing the specified string.
        // Just calling for "Teams.exe" isn't enough because there are multiple processes running with "Teams.exe".
        if (proc.MainWindowTitle.Contains(windowTitleContains))
        {
            return proc.Id;
        }
    }
    return -1;
}

static IntPtr GetModuleBaseAddress(int processID, string moduleName)
{
    IntPtr moduleBaseAddress = IntPtr.Zero;
    IntPtr[] moduleHandles = new IntPtr[1024];

    if (EnumProcessModulesEx(Process.GetProcessById(processID).Handle, moduleHandles, IntPtr.Size * moduleHandles.Length, out int bytesNeeded, 0x03))
    {
        int numOfModules = bytesNeeded / IntPtr.Size;
        for (int i = 0; i < numOfModules; i++)
        {
            StringBuilder sbModuleName = new StringBuilder(255);
            if (GetModuleBaseName(Process.GetProcessById(processID).Handle, moduleHandles[i], sbModuleName, sbModuleName.Capacity) > 0)
            {
                if (sbModuleName.ToString().Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    moduleBaseAddress = moduleHandles[i];
                    break;
                }
            }
        }
    }
    return moduleBaseAddress;
}

void ChangeStateToActive()
{
    try
    {
        string dllName = string.Empty;
        int offset = 0;

        (dllName, offset) = versionNumber switch
        {
            "1.7.00.156" => ("combase.dll", 0x335B29),
            "1.6.00.35961" => ("textinputframework.dll", 0x13489D), // My guess is the combase one works for this version too...
            "1.6.00.29964" => ("combase.dll", 0x335B29),
            "24033.811.2738.2546" => ("skypert.dll", 0x4A12E1),
            _ => (string.Empty, 0)
        };

        CheckIfTeamsIsRunning(out bool isTeamsRunning);
        if (!isTeamsRunning)
        {
            return;
        }

        _ = double.TryParse(versionNumber, out double versionNumberDouble);

        if (versionNumber == "23320.3027.2591.1505")
        {
            _ = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED); // We only need to prevent the computer from sleeping in this version.
            if (userMessage == "Successfully disabling automatic inactivity!")
            {
                // Don't repeat the message.
            }
            else
            {
                userMessage = "Successfully disabling automatic inactivity!";
                Console.WriteLine(userMessage);
            }
        }
        else
        {
            IntPtr processHandle = OpenProcess(PROCESS_WM_READ | PROCESS_WM_WRITE | PROCESS_VM_OPERATION, false, processID);

            // Get the base address of the DLL in the process's memory space.
            IntPtr dllBaseAddress = GetModuleBaseAddress(processID, dllName);

            if (dllBaseAddress == IntPtr.Zero)
            {
                if (userMessage != $"Failed to find the base address of {dllName}.")
                {
                    userMessage = $"Failed to find the base address of {dllName}.";
                    Console.WriteLine(userMessage);
                }
                // Don't repeat the error message.
                return;
            }

            // Calculate the address to write to by adding the offset to the base address.
            IntPtr addressToWriteTo = IntPtr.Add(dllBaseAddress, offset);

            if (versionNumber == "24004.1307.2669.7070")
            {
                valueToWrite = 1;
            }
            else
            {
                valueToWrite = 1;
            }

            // Allocate a buffer with the value to write.
            byte[] buffer = [valueToWrite];

            // Write the value to the calculated address. Let us know what the result was.
            bool result = WriteProcessMemory(processHandle, addressToWriteTo, buffer, buffer.Length, out int bytesWritten);

            if (result && bytesWritten == buffer.Length)
            {
                if (userMessage == "Successfully disabling automatic inactivity!")
                {
                    // Don't repeat the message.
                }
                else
                {
                    userMessage = "Successfully disabling automatic inactivity!";
                    Console.WriteLine(userMessage);
                }

                // Time to actually enable no-sleep mode.
                _ = SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
            }
            else
            {
                Console.WriteLine("Unable to disable Teams inactivity (couldn't change offset to 1.)");
                return;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("You got an error :( " + ex.Message);
    }
}

void CheckTeamsVersion(out string acceptedVersion, out string versionNumber)
{
    versionNumber = string.Empty;
    acceptedVersion = "undetermined";

    try
    {
        string dllName = string.Empty;
        if (isUsingNewTeams)
        {
            dllName = "EmbeddedBrowserWebView.dll";
        }
        else
        {
            dllName = "Teams.exe";
        }

        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, processID);

        // Get the base address of the DLL in the process's memory space.
        IntPtr dllBaseAddress = GetModuleBaseAddress(processID, dllName);

        if (dllBaseAddress == IntPtr.Zero)
        {
            if (userMessage != $"Failed to find the base address of {dllName}.")
            {
                userMessage = $"Failed to find the base address of {dllName}.";
                Console.WriteLine(userMessage);
            }
            return;
        }

        // Check through the different supported versions of Teams.
        string[] versions = ["1.6.00.35961", "1.6.00.29964", "1.7.00.156", "23320.3027.2591.1505", "24033.811.2738.2546"];
        int[] offsets = [0x89AECE9, 0x89AFCE9, 0x89B0CE9, 0x6813C5, 0x4CB475];

        for (int i = 0; i < versions.Length; i++)
        {

            int stringLength = versions[i].Length;
            IntPtr addressToRead = IntPtr.Add(dllBaseAddress, offsets[i]);

            // Allocate a buffer to store the value to read.
            byte[] buffer = new byte[stringLength];

            // Read the value to the calculated address. Let us know what the result was.
            bool result = ReadProcessMemory(processHandle, addressToRead, buffer, buffer.Length, out int bytesRead);

            // Convert the byte array to a string using the ASCII encoding.
            string readString = Encoding.ASCII.GetString(buffer);

            // Check if the read string matches the expected value.
            if (result && bytesRead == buffer.Length && readString.Equals(versions[i]))
            {
                // Matching Teams version found!
                acceptedVersion = "supported";
                versionNumber = versions[i];
                return;
            }
        }

        // If we reach this point, no supported version was found.
        acceptedVersion = "unsupported";
        Console.WriteLine("Your version of Teams is unsupported. Sorry!");
    }
    catch (Exception ex)
    {
        Console.WriteLine("You got an error :( " + ex.Message);
    }
}

void CheckIfTeamsIsRunning(out bool isTeamsRunning)
{
    // processID may seem redundant here, but it's needs to be updated if the program just opened/restarted.
    processID = GetProcessIdByName("Teams", " | Microsoft Teams classic");
    if (processID == -1)
    {
        // "New" Teams executable.
        processID = GetProcessIdByName("ms-teams", " | Microsoft Teams");
        if (processID == -1)
        {
            isTeamsRunning = false;
            if (userMessage == "Teams is not running.")
            {
                // Don't repeat the same error message.            
            }
            else
            {
                userMessage = "Teams is not running.";
                Console.WriteLine(userMessage);
            }
            return;
        }
        else
        {
            isUsingNewTeams = true;
            isTeamsRunning = true;
        }
    }
    else
    {
        isTeamsRunning = true;
    }
}

void PreventSleepMacOS()
{
    try
    {
        // Start the caffeinate process to prevent the system from sleeping.
        using Process caffeinateProcess = new Process();
        caffeinateProcess.StartInfo.FileName = "/usr/bin/caffeinate";
        caffeinateProcess.StartInfo.Arguments = "-di"; // Prevent the display from sleeping and keep the system awake indefinitely.
        caffeinateProcess.StartInfo.UseShellExecute = false;
        caffeinateProcess.Start();

        Console.WriteLine("System sleep prevented. Press Enter to allow sleep and exit the program.");
        Console.ReadLine();

        // When the user presses Enter, stop the caffeinate process to allow the system to sleep again.
        caffeinateProcess.Kill();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }
}

void CheckIfTeamsIsRunningMacOS(out bool isTeamsRunning)
{
    // Use 'pgrep' to find Teams process by name.
    ProcessStartInfo startInfo = new ProcessStartInfo
    {
        FileName = "/bin/bash",
        Arguments = "-c \"pgrep -l 'Teams'\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        CreateNoWindow = true
    };

    using (Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process"))
    {
        using (StreamReader reader = process.StandardOutput ?? throw new InvalidOperationException("StandardOutput is null"))
        {
            string result = reader.ReadToEnd();

            // Check if the output contains the name of the Teams process.
            // I need to # add some code that discriminates old Teams.
            isTeamsRunning = result.Contains("Teams");
        }
    }

    if (!isTeamsRunning)
    {
        if (userMessage != "Teams is not running.")
        {
            userMessage = "Teams is not running.";
            Console.WriteLine(userMessage);
        }
        // Don't repeat the same error message.        
    }
    else
    {
        Console.WriteLine("Teams is running.");
    }
}

void CurrentDomain_ProcessExit(object? sender, EventArgs e)
{
    Console.WriteLine("Program is exiting. Please wait.");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        _ = SetThreadExecutionState(ES_CONTINUOUS);
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        caffeinateProcess?.Kill();
    }
    else
    {
        Console.WriteLine("How did you end up here?");
    }
}