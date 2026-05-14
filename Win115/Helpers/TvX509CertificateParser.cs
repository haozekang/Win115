using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities.IO;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

namespace Win115.Helpers
{
    internal static class Platform
    {
        private static readonly CompareInfo InvariantCompareInfo = CultureInfo.InvariantCulture.CompareInfo;

        internal static bool EqualsIgnoreCase(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        internal static string? GetEnvironmentVariable(string variable)
        {
            try
            {
                return Environment.GetEnvironmentVariable(variable);
            }
            catch (System.Security.SecurityException)
            {
                // We don't have the required permission to read this environment variable,
                // which is fine, just act as if it's not set
                return null;
            }
        }

        internal static int IndexOf(string source, char value)
        {
            return InvariantCompareInfo.IndexOf(source, value, CompareOptions.Ordinal);
        }

        internal static int IndexOf(string source, string value)
        {
            return InvariantCompareInfo.IndexOf(source, value, CompareOptions.Ordinal);
        }

        internal static int IndexOf(string source, char value, int startIndex)
        {
            return InvariantCompareInfo.IndexOf(source, value, startIndex, CompareOptions.Ordinal);
        }

        internal static int IndexOf(string source, string value, int startIndex)
        {
            return InvariantCompareInfo.IndexOf(source, value, startIndex, CompareOptions.Ordinal);
        }

        internal static bool Is64BitProcess
        {
#if NETCOREAPP2_0_OR_GREATER || NET40_OR_GREATER || NETSTANDARD2_0_OR_GREATER

            get { return Environment.Is64BitProcess; }
#else
			get { return IntPtr.Size == 8; }
#endif
        }

        internal static int LastIndexOf(string source, string value)
        {
            return InvariantCompareInfo.LastIndexOf(source, value, CompareOptions.Ordinal);
        }

        internal static bool StartsWith(string source, string prefix)
        {
            return InvariantCompareInfo.IsPrefix(source, prefix, CompareOptions.Ordinal);
        }

        internal static bool StartsWithIgnoreCase(string source, string prefix)
        {
            return InvariantCompareInfo.IsPrefix(source, prefix, CompareOptions.OrdinalIgnoreCase);
        }

        internal static bool EndsWith(string source, string suffix)
        {
            return InvariantCompareInfo.IsSuffix(source, suffix, CompareOptions.Ordinal);
        }

        internal static string? GetTypeName(object obj)
        {
            return GetTypeName(obj.GetType());
        }

        internal static string? GetTypeName(Type t)
        {
            return t.FullName;
        }
    }

    class PemParser
    {
        private readonly string _header1;
        private readonly string _header2;
        private readonly string _footer1;
        private readonly string _footer2;

        internal PemParser(
            string type)
        {
            _header1 = "-----BEGIN " + type + "-----";
            _header2 = "-----BEGIN X509 " + type + "-----";
            _footer1 = "-----END " + type + "-----";
            _footer2 = "-----END X509 " + type + "-----";
        }

        private string? ReadLine(
            Stream inStream)
        {
            int c;
            StringBuilder l = new StringBuilder();

            do
            {
                while (((c = inStream.ReadByte()) != '\r') && c != '\n' && (c >= 0))
                {
                    if (c == '\r')
                    {
                        continue;
                    }

                    l.Append((char)c);
                }
            }
            while (c >= 0 && l.Length == 0);

            if (c < 0)
            {
                return null;
            }

            return l.ToString();
        }

        internal Asn1Sequence? ReadPemObject(
            Stream inStream)
        {
            string? line;
            StringBuilder pemBuf = new StringBuilder();

            while ((line = ReadLine(inStream)) != null)
            {
                if (Platform.StartsWith(line, _header1) || Platform.StartsWith(line, _header2))
                {
                    break;
                }
            }

            while ((line = ReadLine(inStream)) != null)
            {
                if (Platform.StartsWith(line, _footer1) || Platform.StartsWith(line, _footer2))
                {
                    break;
                }

                pemBuf.Append(line);
            }

            if (pemBuf.Length != 0)
            {
                Asn1Object o = Asn1Object.FromByteArray(Base64.Decode(pemBuf.ToString()));

                if (!(o is Asn1Sequence))
                {
                    throw new IOException("malformed PEM data encountered");
                }

                return (Asn1Sequence)o;
            }

            return null;
        }
    }

    public class TvX509CertificateParser
    {
        private static readonly PemParser PemCertParser = new PemParser("CERTIFICATE");

        private Asn1Set? sData;
        private int sDataObjectCount;
        private Stream? currentStream;

        private X509Certificate? ReadDerCertificate(Asn1InputStream dIn)
        {
            Asn1Sequence seq = (Asn1Sequence)dIn.ReadObject();

            if (seq.Count > 1 && seq[0] is DerObjectIdentifier)
            {
                if (seq[0].Equals(PkcsObjectIdentifiers.SignedData))
                {
                    sData = SignedData.GetInstance(
                        Asn1Sequence.GetInstance((Asn1TaggedObject)seq[1], true)).Certificates;

                    return GetCertificate();
                }
            }

            return new X509Certificate(X509CertificateStructure.GetInstance(seq));
        }

        private X509Certificate? ReadPemCertificate(Stream inStream)
        {
            Asn1Sequence? seq = PemCertParser.ReadPemObject(inStream);

            return seq == null ? null : new X509Certificate(X509CertificateStructure.GetInstance(seq));
        }

        private X509Certificate? GetCertificate()
        {
            if (sData != null)
            {
                while (sDataObjectCount < sData.Count)
                {
                    object obj = sData[sDataObjectCount++];

                    if (obj is Asn1Sequence)
                        return new X509Certificate(X509CertificateStructure.GetInstance(obj));
                }
            }

            return null;
        }

        /// <summary>
        /// Create loading data from byte array.
        /// </summary>
        /// <param name="input"></param>
        public X509Certificate? ReadCertificate(byte[] input)
        {
            using (var stream = new MemoryStream(input, false))
            {
                return ReadCertificate(stream);
            }
            ;
        }

        /// <summary>
        /// Create loading data from byte array.
        /// </summary>
        /// <param name="input"></param>
        public IList<X509Certificate> ReadCertificates(byte[] input)
        {
            using (var stream = new MemoryStream(input, false))
            {
                return ReadCertificates(new MemoryStream(input, false));
            }
            ;
        }

        /**
		 * Generates a certificate object and initializes it with the data
		 * read from the input stream inStream.
		 */
        public X509Certificate? ReadCertificate(Stream inStream)
        {
            if (inStream == null)
                throw new ArgumentNullException("inStream");
            if (!inStream.CanRead)
                throw new ArgumentException("inStream must be read-able", "inStream");

            if (currentStream == null)
            {
                currentStream = inStream;
                sData = null;
                sDataObjectCount = 0;
            }
            else if (currentStream != inStream) // reset if input stream has changed
            {
                currentStream = inStream;
                sData = null;
                sDataObjectCount = 0;
            }

            try
            {
                if (sData != null)
                {
                    if (sDataObjectCount != sData.Count)
                        return GetCertificate();

                    sData = null;
                    sDataObjectCount = 0;
                    return null;
                }

                int tag = inStream.ReadByte();
                if (tag < 0)
                    return null;

                if (inStream.CanSeek)
                {
                    inStream.Seek(-1L, SeekOrigin.Current);
                }
                else
                {
                    PushbackStream pis = new PushbackStream(inStream);
                    pis.Unread(tag);
                    inStream = pis;
                }
                if (tag == 0x4d)
                {
                    var inBytes = new byte[inStream.Length];
                    inStream.ReadExactly(inBytes, 0, inBytes.Length);
                    var outBytes = Convert.FromBase64String(Encoding.UTF8.GetString(inBytes));
                    inStream.Dispose();
                    inStream = new MemoryStream(outBytes, false);
                    tag = outBytes[0];
                }
                if (tag != 0x30)  // assume ascii PEM encoded.
                    return ReadPemCertificate(inStream);

                using (var asn1In = new Asn1InputStream(inStream, int.MaxValue, leaveOpen: true))
                {
                    return ReadDerCertificate(asn1In);
                }
            }
            catch (CertificateException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new CertificateException("Failed to read certificate", e);
            }
        }

        /**
		 * Returns a (possibly empty) collection view of the certificates
		 * read from the given input stream inStream.
		 */
        public IList<X509Certificate> ReadCertificates(Stream inStream)
        {
            return new List<X509Certificate>(ParseCertificates(inStream));
        }

        public IEnumerable<X509Certificate> ParseCertificates(Stream inStream)
        {
            X509Certificate? cert;
            while ((cert = ReadCertificate(inStream)) != null)
            {
                yield return cert;
            }
        }
    }
}
