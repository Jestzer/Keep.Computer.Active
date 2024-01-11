using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

Console.WriteLine("Releasing the chickens...");
string userMessage = string.Empty;
CheckTeamsVersion(out bool acceptedVersion);

while (acceptedVersion == true)
{
    ChangeStateToActive();
    await Task.Delay(2000);
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
    // No process found with the specified name and window title criteria.
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
        string dllName = "textinputframework.dll";
        int processID = GetProcessIdByName("Teams", " | Microsoft Teams classic");
        int offset = 0x13489D;
        if (processID == -1)
        {
            if (userMessage == "Teams Classic is not running.")
            {
                // Don't repeat the same error message.
            }
            else
            {
                userMessage = "Teams Classic is not running.";
                Console.WriteLine(userMessage);
            }
            return;
        }

        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_WM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;

        IntPtr processHandle = OpenProcess(PROCESS_WM_READ | PROCESS_WM_WRITE | PROCESS_VM_OPERATION, false, processID);

        // Get the base address of the DLL in the process's memory space.
        IntPtr dllBaseAddress = GetModuleBaseAddress(processID, dllName);

        if (dllBaseAddress == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to find the base address of {dllName}.");
            return;
        }

        // Calculate the address to write to by adding the offset to the base address.
        IntPtr addressToWriteTo = IntPtr.Add(dllBaseAddress, offset);

        byte valueToWrite = 1;

        // Allocate a buffer with the value to write.
        byte[] buffer = [valueToWrite];

        // Write the value to the calculated address. Let us know what the result was.
        bool result = WriteProcessMemory(processHandle, addressToWriteTo, buffer, buffer.Length, out int bytesWritten);

        if (result && bytesWritten == buffer.Length)
        {
            if (userMessage == "Currently disabling automatic inactivity!")
            {
                // Don't repeat the message.
            }
            else
            {
                userMessage = "Currently disabling automatic inactivity!";
                Console.WriteLine(userMessage);
            }            
        }
        else
        {
            Console.WriteLine("Unable to change the offset value to 1. Do some debugging.");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("You got an error :( " + ex.Message);
    }
}

void CheckTeamsVersion(out bool acceptedVersion)
{
    acceptedVersion = true;
    try
    {
        string dllName = "Teams.exe";
        int processID = GetProcessIdByName("Teams", " | Microsoft Teams classic");
        int offset = 0x89AECE9;
        if (processID == -1)
        {
            if (userMessage == "Teams Classic is not running.")
            {
                // Don't repeat the same error message.
            }
            else
            {
                userMessage = "Teams Classic is not running.";
                Console.WriteLine(userMessage);
            }
            return;
        }

        const int PROCESS_WM_READ = 0x0010;

        IntPtr processHandle = OpenProcess(PROCESS_WM_READ, false, processID);

        // Get the base address of the DLL in the process's memory space.
        IntPtr dllBaseAddress = GetModuleBaseAddress(processID, dllName);

        if (dllBaseAddress == IntPtr.Zero)
        {
            Console.WriteLine($"Failed to find the base address of {dllName}.");
            return;
        }

        // Calculate the address to read by adding the offset to the base address.
        IntPtr addressToRead = IntPtr.Add(dllBaseAddress, offset);

        int stringLength = "1.6.00.35961".Length;

        // Allocate a buffer to store the value to read.
        byte[] buffer = new byte[stringLength];

        // Read the value to the calculated address. Let us know what the result was.
        bool result = ReadProcessMemory(processHandle, addressToRead, buffer, buffer.Length, out int bytesRead);

        // Convert the byte array to a string using the ASCII encoding.
        string readString = Encoding.ASCII.GetString(buffer);

        // Check if the read string matches the expected value.
        if (result && bytesRead == buffer.Length && readString == "1.6.00.35961")
        {
            // Matching Teams version found!
        }
        else
        {
            acceptedVersion = false;
            Console.WriteLine("Your version of Teams is unsupported. Sorry!");
            return;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("You got an error :( " + ex.Message);
    }
}