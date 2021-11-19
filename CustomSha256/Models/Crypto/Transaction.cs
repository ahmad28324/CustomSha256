namespace CustomSha256.Models.Crypto
{
    public class Transaction
    {
        /// <summary>
        /// Входы
        /// </summary>
        public TransactionInput[] Inputs { get; set; }

        /// <summary>
        /// Выходы
        /// </summary>
        public TransactionOutput[] Outputs { get; set; }
    }
}
