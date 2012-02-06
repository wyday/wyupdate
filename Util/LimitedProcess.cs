using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using wyUpdate;

public static class LimitedProcess
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseHandle(IntPtr hObject);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CreateProcessWithTokenW(IntPtr hToken, uint dwLogonFlags, string lpApplicationName, string lpCommandLine, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    const UInt32 INFINITE = 0xFFFFFFFF;
    const UInt32 WAIT_ABANDONED = 0x00000080;
    const UInt32 WAIT_OBJECT_0 = 0x00000000;
    const UInt32 WAIT_TIMEOUT = 0x00000102;
    const UInt32 WAIT_FAILED = 0xFFFFFFFF;

    const short SW_HIDE = 0;
    const short SW_MAXIMIZE = 3;
    const short SW_MINIMIZE = 6;

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    const uint STARTF_USESHOWWINDOW = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public String lpReserved;
        public String lpDesktop;
        public String lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    /// <summary>Starts the limited process.</summary>
    /// <param name="filename">The file to start as a limited process.</param>
    /// <param name="arguments">The arguments to pass to the process.</param>
    /// <param name="fallback">Fallback on Process.Start() if this method fails (for whatever reason). We use a simple Process.Start(filename, arguments) call -- we don't pass in all the details.</param>
    /// <param name="waitForExit"></param>
    /// <param name="windowStyle"></param>
    /// <returns>The exit code if waitForExit = true, otherwise it returns 0.</returns>
    public static uint Start(string filename, string arguments = null, bool fallback = true, bool waitForExit = false, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal)
    {
        bool processCreated = false;
        uint exitCode = 0;
        string errorDetails = null;

        if (VistaTools.AtLeastVista() && VistaTools.IsUserAnAdmin())
        {
            //Get window handle representing the desktop shell.  This might not work if there is no shell window, or when
            //using a custom shell.  Also note that we're assuming that the shell is not running elevated.
            IntPtr hShellWnd = GetShellWindow();
            if (hShellWnd == IntPtr.Zero)
                throw new Exception("Unable to locate shell window; you might be using a custom shell");

            //Get the ID of the desktop shell process.
            int dwShellPID;
            GetWindowThreadProcessId(hShellWnd, out dwShellPID);
            if (dwShellPID != 0)
            {
                //Open the desktop shell process in order to get the process token.
                // PROCESS_QUERY_INFORMATION = 0x00000400
                IntPtr hShellProcess = OpenProcess(0x0400, false, dwShellPID);

                if (hShellProcess != IntPtr.Zero)
                {
                    IntPtr hShellProcessToken;

                    //Get the process token of the desktop shell.

                    //TOKEN_DUPLICATE = 0x0002
                    if (OpenProcessToken(hShellProcess, 0x2, out hShellProcessToken))
                    {
                        IntPtr hPrimaryToken;
                        //Duplicate the shell's process token to get a primary token.

                        //TOKEN_QUERY = 0x0008
                        //TOKEN_ASSIGN_PRIMARY = 0x0001
                        //TOKEN_DUPLICATE = 0x0002
                        //TOKEN_ADJUST_DEFAULT = 0x0080
                        //TOKEN_ADJUST_SESSIONID = 0x0100;
                        const uint dwTokenRights = 0x0008 | 0x0001 | 0x0002 | 0x0080 | 0x0100;
                        //SecurityImpersonation = 2
                        //TokenPrimary = 1
                        if (DuplicateTokenEx(hShellProcessToken, dwTokenRights, IntPtr.Zero, 2, 1, out hPrimaryToken))
                        {
                            //Start the target process with the new token.
                            PROCESS_INFORMATION pi;

                            STARTUPINFO si = new STARTUPINFO();

                            if (windowStyle != ProcessWindowStyle.Normal)
                            {
                                // set how we'll be starting the process
                                si.dwFlags = STARTF_USESHOWWINDOW;

                                switch (windowStyle)
                                {
                                    case ProcessWindowStyle.Hidden:
                                        si.wShowWindow = SW_HIDE;
                                        break;
                                    case ProcessWindowStyle.Maximized:
                                        si.wShowWindow = SW_MAXIMIZE;
                                        break;
                                    case ProcessWindowStyle.Minimized:
                                        si.wShowWindow = SW_MINIMIZE;
                                        break;
                                }
                            }

                            si.cb = Marshal.SizeOf(si);

                            // build the arguments string
                            // filenames must be quoted or else the commandline args get blown
                            if (string.IsNullOrEmpty(arguments))
                                arguments = "\"" + filename + "\"";
                            else
                                arguments = "\"" + filename + "\" " + arguments;

                            processCreated = CreateProcessWithTokenW(hPrimaryToken, 0, filename, arguments, 0, IntPtr.Zero, Path.GetDirectoryName(filename), ref si, out pi);

                            if (processCreated)
                            {
                                //TODO: get the exit code

                                // wait for the process to finish executing
                                if (waitForExit)
                                {
                                    if (WaitForSingleObject(pi.hProcess, INFINITE) == WAIT_FAILED)
                                    {
                                        // handle wait failure
                                        int error = Marshal.GetLastWin32Error();
                                        throw new ExternalException("Failed to wait for the executed process \"" + filename +
                                                            "\". WaitForSingleObject() failed with the error code " + error +
                                                            ".", error);
                                    }

                                    // get the exit code
                                    if (!GetExitCodeProcess(pi.hProcess, out exitCode))
                                    {
                                        int error = Marshal.GetLastWin32Error();
                                        throw new ExternalException(
                                            "Failed to get the exit code fo the executed process \"" + filename +
                                            "\". GetExitCodeProcess() failed with the error code " + error + ".", error);
                                    }
                                }

                                //return Process.GetProcessById(pi.dwProcessId);
                                CloseHandle(pi.hProcess);
                                CloseHandle(pi.hThread);
                            }
                            else if (!fallback)
                            {
                                // if we're not falling back to regular old "Process.Start()",
                                // then throw an error with enough info that the user can do something about it.
                                int error = Marshal.GetLastWin32Error();
                                throw new ExternalException("Failed to execute file \"" + filename +
                                                    "\" as a limited process. CreateProcessWithTokenW() failed with the error code " +
                                                    error + ".", error);
                            }

                            if (hPrimaryToken != IntPtr.Zero)
                                CloseHandle(hPrimaryToken);
                        }
                        else if (!fallback)
                        {
                            int error = Marshal.GetLastWin32Error();
                            errorDetails = "DuplicateTokenEx() on the desktop shell process failed with the error code " + error + ".";
                        }

                        if (hShellProcessToken != IntPtr.Zero)
                            CloseHandle(hShellProcessToken);
                    }
                    else if (!fallback)
                    {
                        int error = Marshal.GetLastWin32Error();
                        errorDetails = "OpenProcessToken() on the desktop shell process failed with the error code " + error + ".";
                    }

                    CloseHandle(hShellProcess);
                }
                else if (!fallback)
                {
                    int error = Marshal.GetLastWin32Error();
                    errorDetails = "OpenProcess() on the desktop shell process failed with the error code " + error + ".";
                }
            }
            else if (!fallback)
            {
                errorDetails = "Unable to get the window thread process ID of the desktop shell process.";
            }
        }

        //TODO: throw an exception if 

        // the process failed to be created for any number of reasons
        // just create it using the regular method
        if (!processCreated)
        {
            if (fallback)
                Process.Start(filename, arguments);
            else
                throw new Exception("Failed to execute file \"" + filename + "\" as a limited process. " + errorDetails);
        }

        return exitCode;
    }
}
