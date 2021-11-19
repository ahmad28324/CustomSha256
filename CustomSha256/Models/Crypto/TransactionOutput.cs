namespace CustomSha256.Models.Crypto
{
    public class TransactionOutput
    {
        /// <summary>
        /// Объём переводимых средств
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Условия траты монет (чаще всего открытый ключ получателя или хэш от него) - адрес
        /// </summary>
        public string ScriptPublicKey { get; set; }
    }
}
