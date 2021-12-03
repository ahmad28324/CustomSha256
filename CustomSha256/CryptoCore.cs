namespace CustomSha256
{
    using global::CustomSha256.Models;
    using global::CustomSha256.Models.Crypto;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;

    public class CryptoCore
    {
        private const string NodesInfoPath = "nodesInfo.json";

        private readonly List<NodeInfo> _nodesInfo;
        private readonly string _currentNodeKeys;
        private readonly DbAccessor _dbAccessor;

        private List<(Transaction, int)> _myUnspentTransactionOutputs;
        private List<(Transaction, int)> _allUnspentTransactionOutputs;

        public CryptoCore(int nodeId)
        {
            _nodesInfo = JsonConvert.DeserializeObject<List<NodeInfo>>(File.ReadAllText(NodesInfoPath));

            _currentNodeKeys = _nodesInfo.Find(ni => ni.Id == nodeId).RsaKey;
            _dbAccessor = new DbAccessor();

            _myUnspentTransactionOutputs = GetMyUnspentTransactionOutputs();
            _allUnspentTransactionOutputs = GetAllUnspentTransactionOutputs();
        }

        public void InitializeBlockChain()
        {
            var genesisTransactions = new Transaction[]
            {
                new Transaction
                {
                    Inputs = new TransactionInput[] { },
                    Outputs = new TransactionOutput[]
                    {
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 1).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 2).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 3).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 4).RsaKey,
                        },
                        new TransactionOutput
                        {
                            Value = 10,
                            ScriptPublicKey = _nodesInfo.Find(ni => ni.Id == 5).RsaKey,
                        }
                    }
                }
            };            

            var genesisBlock = new Block
            {
                PreviousBlockHeaderHash = new byte[] { },
                TransactionsHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(genesisTransactions)),
                // TODO
                ProofOfWorkCounter = 0,
                Transactions = genesisTransactions,
            };

            _dbAccessor.ClearDb();
            _dbAccessor.AddBlock(genesisBlock);

            // refresh unspent mine and all lists
            RefreshMyUnspentTransactionOutputsFromBlockChain();
            RefreshAllUnspentTransactionOutputsFromBlockChain();
        }

        // TODO call only when there is enough balance on current key (or write validation inside)
        public void CreateTransaction(string recipientKey, int amount)
        {
            var (transactionToSpend, transactionToSpendOutputIndex) = FindTransactionToSpend(amount);

            var newTransaction = new Transaction
            {
                Inputs = new TransactionInput[]
                {
                    new TransactionInput
                    {
                        PreviousTransactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transactionToSpend)),
                        PreviousTransactionOutputIndex = transactionToSpendOutputIndex,
                    }
                },
                Outputs = new TransactionOutput[]
                {
                    new TransactionOutput
                    {
                        Value = amount,
                        ScriptPublicKey = recipientKey,
                    },
                },
            };

            using var signer = new CustomSigner(_currentNodeKeys);
            var signature = signer.CreateSignature(newTransaction);

            foreach(var input in newTransaction.Inputs)
            {
                input.ScriptSignature = Convert.ToBase64String(signature) + " " + _currentNodeKeys;
            }

            // update local mine and all unspent transactions outputs
            //_myUnspentTransactionOutputs.Remove((transactionToSpend, transactionToSpendOutputIndex));
            //_allUnspentTransactionOutputs.Remove((transactionToSpend, transactionToSpendOutputIndex));

            AddTransactionToPool(newTransaction);
            // TODO send newTransaction to all nodes
        }

        public void AddTransactionToPool(Transaction transaction)
        {
            if (!IsTransactionValid(transaction))
            {
                return;
            }

            // update local mine and all unspent transactions outputs
            foreach (var input in transaction.Inputs)
            {
                var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
                if (_myUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                {
                    _myUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                }

                if (_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
                {
                    _allUnspentTransactionOutputs.Remove((previousTransaction, input.PreviousTransactionOutputIndex));
                }
            }

            _dbAccessor.AddTransactionToPool(transaction);
        }

        public int GetBalance()
        {
            var sum = 0;
            foreach (var unspentTransactionOutput in _myUnspentTransactionOutputs)
            {
                var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(unspentTransactionOutput.Item1));

                var transaction = _dbAccessor.GetTransactionByHash(transactionHash);
                sum += transaction.Outputs[unspentTransactionOutput.Item2].Value;
            }

            return sum;
        }

        public void RefreshMyUnspentTransactionOutputsFromBlockChain()
        {
            _myUnspentTransactionOutputs = GetMyUnspentTransactionOutputs();
        }

        public void RefreshAllUnspentTransactionOutputsFromBlockChain()
        {
            _allUnspentTransactionOutputs = GetAllUnspentTransactionOutputs();
        }

        private bool IsTransactionValid(Transaction transaction)
        {
            foreach (var inputToValidate in transaction.Inputs)
            {
                // check for existence
                var trn = _dbAccessor.GetTransactionByHash(inputToValidate.PreviousTransactionHash);
                if (trn == null)
                {
                    return false;
                }

                TransactionOutput output;
                try
                {
                    output = trn.Outputs[inputToValidate.PreviousTransactionOutputIndex];
                }
                catch
                {
                    return false;
                }

                // check for double spending
                if (IsInputDoubleSpent(inputToValidate))
                {
                    return false;
                }

                // signature check and key equality
                var parseArray = inputToValidate.ScriptSignature.Split(" ");
                var (signature, key) = Tuple.Create(parseArray[0], parseArray[1]);

                if (output.ScriptPublicKey != key)
                {
                    return false;
                }

                if (!IsSignatureValid(signature, key, transaction))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSignatureValid(string signature, string key, Transaction transaction)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(key);

            return rsa.VerifyHash(
                Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction)),
                CryptoConfig.MapNameToOID("SHA256"),
                Convert.FromBase64String(signature));
        }

        private bool IsInputDoubleSpent(TransactionInput input)
        {
            var previousTransaction = _dbAccessor.GetTransactionByHash(input.PreviousTransactionHash);
            if (!_allUnspentTransactionOutputs.Contains((previousTransaction, input.PreviousTransactionOutputIndex)))
            {
                return true;
            }

            return false;
        }

        private List<(Transaction, int)> GetMyUnspentTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var myTransactionOutputs = GetAllMyTransactionOutputs();
            foreach (var myTransactionOutput in myTransactionOutputs)
            {
                if (IsTransactionOutputUnspent(myTransactionOutput.Item1, myTransactionOutput.Item2))
                {
                    result.Add(myTransactionOutput);
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllUnspentTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var allTransactionOutputs = GetAllTransactionOutputs();
            foreach (var transactionOutput in allTransactionOutputs)
            {
                if (IsTransactionOutputUnspent(transactionOutput.Item1, transactionOutput.Item2))
                {
                    result.Add(transactionOutput);
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllMyTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    for (var i = 0; i < transaction.Outputs.Length; i++)
                    {
                        var output = transaction.Outputs[i];
                        if (output.ScriptPublicKey == _currentNodeKeys)
                        {
                            result.Add((transaction, i));
                        }
                    }
                }
            }

            return result;
        }

        private List<(Transaction, int)> GetAllTransactionOutputs()
        {
            var result = new List<(Transaction, int)>();

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var transaction in block.Transactions)
                {
                    for (var i = 0; i < transaction.Outputs.Length; i++)
                    {
                        result.Add((transaction, i));
                    }
                }
            }

            return result;
        }

        private bool IsTransactionOutputUnspent(Transaction transaction, int outputIndex)
        {
            var transactionHash = Utils.ComputeSha256Hash(Utils.ObjectToByteArray(transaction));

            var blocks = _dbAccessor.GetAllBlocks();
            foreach (var block in blocks)
            {
                foreach (var trx in block.Transactions)
                {
                    foreach (var input in transaction.Inputs)
                    {
                        if (input.PreviousTransactionHash == transactionHash &&
                            input.PreviousTransactionOutputIndex == outputIndex)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private (Transaction, int) FindTransactionToSpend(int amount)
        {
            Transaction transactionToSpend = null;
            int transactionToSpendOutputIndex = 0;

            foreach (var unSpentTransactionOutput in _myUnspentTransactionOutputs)
            {
                bool isBlockFound = false;

                for (var i = 0; i < unSpentTransactionOutput.Item1.Outputs.Length; i++)
                {
                    var output = unSpentTransactionOutput.Item1.Outputs[i];
                    if (output.ScriptPublicKey == _currentNodeKeys &&
                            output.Value > amount)
                    {
                        transactionToSpend = unSpentTransactionOutput.Item1;
                        transactionToSpendOutputIndex = i;

                        isBlockFound = true;
                        break;
                    }
                }

                if (isBlockFound)
                {
                    break;
                }
            }

            return (transactionToSpend, transactionToSpendOutputIndex);
        }
    }
}
