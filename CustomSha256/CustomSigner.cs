namespace CustomSha256
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public class CustomSigner : IDisposable
    {
        private readonly RSACryptoServiceProvider _rsaCryptoServiceProvider;

        public CustomSigner(string rsaParameters)
        {
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            _rsaCryptoServiceProvider.FromXmlString(rsaParameters);
        }

        public CustomSigner(RSAParameters rsaParameters)
        {
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            _rsaCryptoServiceProvider.ImportParameters(rsaParameters);
        }

        public static RSAParameters GenerateRsaParameters()
        {
            using var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            return rsaCryptoServiceProvider.ExportParameters(true);
        }

        public static string GenerateKeys()
        {
            using var rsaCryptoServiceProvider = new RSACryptoServiceProvider();
            return rsaCryptoServiceProvider.ToXmlString(true);
        }

        public CustomMessage CreateMessage(string transaction)
        {
            var stringBytes = Encoding.UTF8.GetBytes(transaction);

            var encryptedHash = _rsaCryptoServiceProvider.SignData(stringBytes, SHA256.Create());

            return new CustomMessage
            {
                Transaction = transaction,
                Signature = encryptedHash,
                PublicKeyInfo = _rsaCryptoServiceProvider.ExportParameters(false),
            };
        }

        public void Dispose()
        {
            _rsaCryptoServiceProvider?.Dispose();
        }
    }
}
