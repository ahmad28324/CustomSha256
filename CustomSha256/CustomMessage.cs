namespace CustomSha256
{
    using System.Security.Cryptography;

    public class CustomMessage
    {
        public string Transaction { get; set; }

        public byte[] Signature { get; set; }

        public RSAParameters PublicKeyInfo { get; set; }
    }
}
