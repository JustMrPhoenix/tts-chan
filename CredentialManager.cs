using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.ComponentModel;

// Yoinked from https://stackoverflow.com/a/22435672
namespace TTS_Chan
{
    public static class CredentialManager
    {
        public static readonly string AppName = "TTS_Chan.TwitchAuth";
        public static TTS_Chan.Credential ReadCredential(string applicationName)
        {
            var read = CredRead(applicationName, CredentialType.Generic, 0, out var nCredPtr);
            if (read)
            {
                using CriticalCredentialHandle critCred = new(nCredPtr);
                var cred = critCred.GetCredential();
                return ReadCredential(cred);
            }

            return null;
        }

        private static TTS_Chan.Credential ReadCredential(Credential credential)
        {
            var applicationName = Marshal.PtrToStringUni(credential.TargetName);
            var userName = Marshal.PtrToStringUni(credential.UserName);
            string secret = null;
            if (credential.CredentialBlob != IntPtr.Zero)
            {
                secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
            }

            return new TTS_Chan.Credential(credential.Type, applicationName, userName, secret);
        }

        public static int WriteCredential(string applicationName, string userName, string secret, CredentialPersistence persistence = CredentialPersistence.Session)
        {
            var byteArray = Encoding.Unicode.GetBytes(secret);
            if (byteArray.Length > 512)
                throw new ArgumentOutOfRangeException(nameof(secret), "The secret message has exceeded 512 bytes.");

            Credential credential = new();
            credential.AttributeCount = 0;
            credential.Attributes = IntPtr.Zero;
            credential.Comment = IntPtr.Zero;
            credential.TargetAlias = IntPtr.Zero;
            credential.Type = CredentialType.Generic;
            credential.Persist = (uint)persistence;
            credential.CredentialBlobSize = (uint)Encoding.Unicode.GetBytes(secret).Length;
            credential.TargetName = Marshal.StringToCoTaskMemUni(applicationName);
            credential.CredentialBlob = Marshal.StringToCoTaskMemUni(secret);
            credential.UserName = Marshal.StringToCoTaskMemUni(userName ?? Environment.UserName);

            var written = CredWrite(ref credential, 0);
            var lastError = Marshal.GetLastWin32Error();

            Marshal.FreeCoTaskMem(credential.TargetName);
            Marshal.FreeCoTaskMem(credential.CredentialBlob);
            Marshal.FreeCoTaskMem(credential.UserName);

            if (written)
                return 0;

            throw new Exception(string.Format("CredWrite failed with the error code {0}.", lastError));
        }

        public static IReadOnlyList<TTS_Chan.Credential> EnumerateCrendentials()
        {
            List<TTS_Chan.Credential> result = new();

            var ret = CredEnumerate(null, 0, out var count, out var pCredentials);
            if (ret)
            {
                for (var n = 0; n < count; n++)
                {
                    var credential = Marshal.ReadIntPtr(pCredentials, n * Marshal.SizeOf(typeof(IntPtr)));
                    result.Add(ReadCredential((Credential)Marshal.PtrToStructure(credential, typeof(Credential))!));
                }
            }
            else
            {
                var lastError = Marshal.GetLastWin32Error();
                throw new Win32Exception(lastError);
            }

            return result;
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredWrite([In] ref Credential userCredential, [In] uint flags);

        [DllImport("advapi32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CredEnumerate(string filter, int flag, out int count, out IntPtr pCredentials);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        static extern bool CredFree([In] IntPtr cred);


        public enum CredentialPersistence : uint
        {
            Session = 1,
            LocalMachine,
            Enterprise
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public uint Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
        {
            public CriticalCredentialHandle(IntPtr preexistingHandle)
            {
                SetHandle(preexistingHandle);
            }

            public Credential GetCredential()
            {
                if (!IsInvalid)
                {
                    var credential = (Credential)Marshal.PtrToStructure(handle, typeof(Credential))!;
                    return credential;
                }

                throw new InvalidOperationException("Invalid CriticalHandle!");
            }

            protected override bool ReleaseHandle()
            {
                if (!IsInvalid)
                {
                    CredFree(handle);
                    SetHandleAsInvalid();
                    return true;
                }

                return false;
            }
        }
    }

    public enum CredentialType
    {
        Generic = 1,
        DomainPassword,
        DomainCertificate,
        DomainVisiblePassword,
        GenericCertificate,
        DomainExtended,
        Maximum,
        MaximumEx = Maximum + 1000,
    }

    public class Credential
    {
        private readonly string _applicationName;
        private readonly string _userName;
        private readonly string _password;
        private readonly CredentialType _credentialType;

        public CredentialType CredentialType
        {
            get { return _credentialType; }
        }

        public string ApplicationName
        {
            get { return _applicationName; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        public string Password
        {
            get { return _password; }
        }

        public Credential(CredentialType credentialType, string applicationName, string userName, string password)
        {
            _applicationName = applicationName;
            _userName = userName;
            _password = password;
            _credentialType = credentialType;
        }

        public override string ToString()
        {
            return string.Format("CredentialType: {0}, ApplicationName: {1}, UserName: {2}, Password: {3}", CredentialType, ApplicationName, UserName, Password);
        }
    }
}