using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Win115.Helpers
{
    public static class SignHelper
    {
        public static bool VerifyCert(X509Certificate? current, X509Certificate? up)
        {
            if (current == null || up == null)
            {
                return false;
            }

            // 提取要验证的证书中的公钥
            var keyInfo = up.CertificateStructure.SubjectPublicKeyInfo;
            var publicKey = PublicKeyFactory.CreateKey(keyInfo);

            // 提取要验证的证书中的签名
            byte[] signature = current.GetSignature();

            // 提取证书中的原始数据
            byte[] certDataToVerify = current.GetTbsCertificate();

            // 构建 Signer 对象
            ISigner signer = SignerUtilities.GetSigner(new DerObjectIdentifier(current.SigAlgOid));

            // 初始化签名器
            signer.Init(false, publicKey);

            if (signer == null)
            {
                return false;
            }

            // 验证签名
            signer.BlockUpdate(certDataToVerify, 0, certDataToVerify.Length);
            if (signer.VerifySignature(signature))
            {
                return true;
            }
            return false;
        }
    }
}
