namespace CustomSha256.Models.Crypto
{
    using System;

    [Serializable]
    public class Block
    {
        /// <summary>
        /// Hash заголовка предыдущего блока
        /// </summary>
        public byte[] PreviousBlockHeaderHash { get; set; }

        /// <summary>
        /// Hash всех транзакций в блоке
        /// </summary>
        public byte[] TransactionsHash { get; set; }

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
