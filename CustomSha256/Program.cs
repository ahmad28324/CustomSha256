namespace CustomSha256
{
    using global::CustomSha256.Models.Crypto;
    using LiteDB;
    using System;
    using System.Security.Cryptography;
    using System.Text;

    class Program
    {
        private const string HelloString = @"Hello";
        private const string LoremIpsumString = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";


        static void Main(string[] args)
        {
            /*            using (var db = new LiteDatabase(@"Crypto.db"))
                        {
                            var blocksCollection = db.GetCollection<Block>("blocks");

                            var newBlock = new Block
                            {
                                Hash = new byte[] { },
                                PreviousHash = new byte[] { },
                                ProofOfWorkCount = 3,
                                TransactionsCount = 2,
                                Transactions = new Transaction[] { },
                            };

                            blocksCollection.Insert(newBlock);
                        }

                        using (var db = new LiteDatabase(@"Crypto.db"))
                        {
                            var blocksCollection = db.GetCollection<Block>("blocks");
                            var blocks = blocksCollection.FindAll();

                            foreach (var block in blocks)
                            {
                                var test = block;
                            }
                        }*/

            var rsaParameters = CustomSigner.GenerateRsaParameters();
            using var signer = new CustomSigner(rsaParameters);

            var transaction = LoremIpsumString;
            var message = signer.CreateMessage(transaction);

            // recipient
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(message.PublicKeyInfo);

            var transactionBytes = Encoding.UTF8.GetBytes(message.Transaction);
            var isSignatureValid = rsa.VerifyData(transactionBytes, SHA256.Create(), message.Signature);
        }
    }
}
