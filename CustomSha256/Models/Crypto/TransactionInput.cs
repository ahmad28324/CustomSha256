namespace CustomSha256.Models.Crypto
{
    public class TransactionInput
    {
        /// <summary>
        /// Hash транзакции, в ходе которой средства получены
        /// </summary>
        public byte[] PreviousTransactionHash { get; set; }

        /// <summary>
        /// Порядковый номер выхода предыдущей транзакции, в ходе которой средства были получены
        /// </summary>
        public int PreviousTransactionIndex { get; set; }

        /// <summary>
        /// ScriptSig (доказательства владения монетами) – цифровая подпись и открытый ключ к ней
        /// </summary>
        public string ScriptSignature { get; set; }
    }
}
