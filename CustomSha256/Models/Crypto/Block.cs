namespace CustomSha256.Models.Crypto
{
    public class Block
    {
        /// <summary>
        /// Hash предыдущего блока
        /// </summary>
        public byte[] PreviousHash { get; set; }

        /// <summary>
        /// Hash блока
        /// </summary>
        public byte[] Hash { get; set; }

        /// <summary>
        /// Решение proof of work
        /// </summary>
        public int ProofOfWorkCounter { get; set; }

        /// <summary>
        /// Транзакции
        /// </summary>
        public Transaction[] Transactions { get; set; }
    }
}
