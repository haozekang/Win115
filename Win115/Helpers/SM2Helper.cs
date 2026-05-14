using Org.BouncyCastle.Asn1.GM;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win115.Helpers
{
    public class SM2Helper
    {
        /// <summary>
        /// Server Public Key X(不要改)
        /// </summary>
        private string PublicKeyX { get; set; } = string.Empty;

        /// <summary>
        /// Server Public Key X(不要改)
        /// </summary>
        private string PublicKeyY { get; set; } = string.Empty;

        /// <summary>
        /// Client Private Key
        /// </summary>
        private string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// UserId
        /// </summary>
        private string UserId => "32323334353637383132333435363732";

        /// <summary>
        /// 生成SM2帮助类对象
        /// </summary>
        public SM2Helper()
        {
        }

        /// <summary>
        /// 生成SM2帮助类对象
        /// </summary>
        /// <param name="PrivateKey">Client Private Key</param>
        public SM2Helper(string PrivateKey)
        {
            this.PrivateKey = PrivateKey;
        }

        /// <summary>
        /// 生成SM2帮助类对象
        /// </summary>
        /// <param name="PublicKeyX">Server Public Key X</param>
        /// <param name="PublicKeyY">Server Public Key Y</param>
        public SM2Helper(string PublicKeyX, string PublicKeyY)
        {
            this.PublicKeyX = PublicKeyX;
            this.PublicKeyY = PublicKeyY;
        }

        /// <summary>
        /// 生成SM2帮助类对象
        /// </summary>
        /// <param name="PublicKeyX">Server Public Key X</param>
        /// <param name="PublicKeyY">Server Public Key Y</param>
        /// <param name="PrivateKey">Client Private Key</param>
        public SM2Helper(string PublicKeyX, string PublicKeyY, string PrivateKey)
        {
            this.PublicKeyX = PublicKeyX;
            this.PublicKeyY = PublicKeyY;
            this.PrivateKey = PrivateKey;
        }

        public void SetPublicKey(string x, string y)
        {
            this.PublicKeyX = x;
            this.PublicKeyY = y;
        }

        public void SetPrivateKey(string pri)
        {
            this.PrivateKey = pri;
        }

        /// <summary>
        /// SM2加密
        /// </summary>
        /// <param name="data">（二进制字符串）</param>
        /// <returns></returns>
        public byte[] Encrypt(string data, string? publicKeyX = null, string? publicKeyY = null)
        {
            // 获取X坐标和Y坐标的十六进制字符串
            string xHex = publicKeyX ?? PublicKeyX;
            string yHex = publicKeyY ?? PublicKeyY;

            // 将十六进制字符串转换为BigInteger
            BigInteger x = new BigInteger(xHex, 16);
            BigInteger y = new BigInteger(yHex, 16);

            // SM2 曲线参数
            ECDomainParameters domainParameters = new ECDomainParameters(
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).Curve,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).G,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).H);

            // 使用 SM2 曲线参数创建 ECPoint 对象
            ECPoint ecPoint = domainParameters.Curve.CreatePoint(x, y);

            // 创建 ECPublicKeyParameters 对象
            AsymmetricKeyParameter keyParam = new ECPublicKeyParameters(ecPoint, domainParameters);
            var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            engine.Init(true, new ParametersWithRandom(keyParam));
            var outBytes = engine.ProcessBlock(dataBytes, 0, dataBytes.Length);
            return outBytes;
        }

        /// <summary>
        /// SM2解密
        /// </summary>
        /// <param name="data">（二进制字符串）</param>
        /// <returns></returns>
        public byte[] Decrypt(string data, string? privateKey = null)
        {
            byte[] privateKeyBytes = Hex.Decode(privateKey ?? PrivateKey);

            // SM2 曲线参数
            ECDomainParameters domainParameters = new ECDomainParameters(
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).Curve,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).G,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N,
                GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).H);

            // 使用私钥字节数组和曲线参数创建 ECPrivateKeyParameters 对象
            AsymmetricKeyParameter keyParam = new ECPrivateKeyParameters(new BigInteger(1, privateKeyBytes), domainParameters);
            var engine = new SM2Engine(SM2Engine.Mode.C1C3C2);
            byte[] dataBytes = Hex.Decode(data);
            engine.Init(false, keyParam);
            var outBytes = engine.ProcessBlock(dataBytes, 0, dataBytes.Length);
            return outBytes;
        }

        public bool VerifySignature(byte[] dataBytes, string _r, string _s, string? _x = null, string? _y = null)
        {
            try
            {
                byte[] idBytes = Hex.Decode(UserId);
                // SM2 曲线参数
                ECDomainParameters domainParameters = new ECDomainParameters(
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).Curve,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).G,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).H);

                // 将十六进制字符串转换为BigInteger
                BigInteger x = x = new BigInteger(_x ?? PublicKeyX, 16);
                BigInteger y = y = new BigInteger(_y ?? PublicKeyY, 16);

                // 使用 SM2 曲线参数创建 ECPoint 对象
                var ecPoint = domainParameters.Curve.CreatePoint(x, y);

                // 创建 ECPublicKeyParameters 对象
                ParametersWithID pubKeyParam = new ParametersWithID(new ECPublicKeyParameters(ecPoint, domainParameters), idBytes);

                // 将十六进制字符串转换为BigInteger
                BigInteger r = new BigInteger(_r, 16);
                BigInteger s = new BigInteger(_s, 16);

                var dsaEncoding = StandardDsaEncoding.Instance;
                var signatureBytes = dsaEncoding.Encode(GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N, r, s);

                // 构建 GMSSigner 对象
                SM2Signer signer = new SM2Signer();

                // 初始化签名器
                signer.Init(false, pubKeyParam);
                // 验证签名
                signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
                return signer.VerifySignature(signatureBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return false;
        }

        public (string, string) Sign(string data, string? privateKey = null)
        {
            try
            {
                // SM2 曲线参数
                ECDomainParameters domainParameters = new ECDomainParameters(
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).Curve,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).G,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N,
                    GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).H);
                BigInteger D = new BigInteger(privateKey ?? PrivateKey, 16);
                // 使用私钥字节数组和曲线参数创建 ECPrivateKeyParameters 对象
                var priKeyParam = new ECPrivateKeyParameters(D, domainParameters);
                byte[] dataBytes = Hex.Decode(data);
                byte[] idBytes = Hex.Decode(UserId);

                // 构建 GMSSigner 对象
                SM2Signer signer = new SM2Signer();

                // 初始化签名器
                signer.Init(true, new ParametersWithID(new ParametersWithRandom(priKeyParam), idBytes));
                signer.BlockUpdate(dataBytes, 0, dataBytes.Length);
                var signatureBytes = signer.GenerateSignature();

                var dsaEncoding = StandardDsaEncoding.Instance;
                var signatureArray = dsaEncoding.Decode(GMNamedCurves.GetByOid(GMObjectIdentifiers.sm2p256v1).N, signatureBytes);
                if (signatureArray.Length != 2)
                {
                    throw new Exception($"签名数组长度不正确！");
                }
                var SignatureR = signatureArray[0].ToString(16).PadLeft(64, '0')?.ToUpper() ?? string.Empty;
                var SignatureS = signatureArray[1].ToString(16).PadLeft(64, '0')?.ToUpper() ?? string.Empty;
                return (SignatureR, SignatureS);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return (string.Empty, string.Empty);
        }
    }
}
