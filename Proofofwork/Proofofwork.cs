using System;
using System.Security.Cryptography;
using System.Collections.Generic;

/// Computes and verifies proof-of-work  with Hash256
namespace CustomSha256
{
    public class Proofofwork
    {
        #region " Properties "
        /// Get the the Right hush with the number of 0
        public byte[] right_hush_ {   get {   return _right_hush;  }   }
        /// Get the value of the solved solution.
        public byte[] solution_number_ {   get {   return _solution_number;   }   }
        /// Gets the difficulty, in bits, for the current challenge.
        public byte Difficulty_  {   get {   return _difficulty; }   }
        #endregion

        #region " Members "
        private byte[] _solution_number;//
        private byte[] _right_hush;
        private byte _difficulty;
        private byte[] _hash;
        #endregion

        #region " Constructor " 
        public Proofofwork(byte[] hashAlgorithm, byte difficulty, byte[] right_hush = null)
        {
            Initialize(hashAlgorithm, difficulty, right_hush);
        }

        private void Initialize(byte[] hashAlgorithm, byte difficulty, byte[] right_hush)
        {
            _hash = hashAlgorithm;
            _difficulty = difficulty;
            _right_hush = right_hush ?? CreateNonce(16);
        }

        #endregion

        #region " Solution "
        static byte[] AddByte(byte[] a, byte[] b)
        {
            List<byte> result = new List<byte>();
            if (a.Length < b.Length)
            {
                byte[] t = a;
                a = b;
                b = t;
            }
            int carry = 0;
            for (int i = 0; i < b.Length; ++i)
            {
                int sum = a[i] + b[i] + carry;
                result.Add((byte)(sum & 0xFF));
                carry = sum >> 8;
            }
            for (int i = b.Length; i < a.Length; ++i)
            {
                int sum = a[i] + carry;
                result.Add((byte)(sum & 0xFF));
                carry = sum >> 8;
            }
            if (carry > 0)
            {
                result.Add((byte)carry);
            }
            return result.ToArray();
        }

        public bool FindSolution(byte[] block_string)
        {
            if (_solution_number != null)
            {
                return true;
            }

            byte[] hash = null;
            byte[] buffer = new byte[_difficulty + _right_hush.Length];
            byte[] data;

            //uint maxCounter = GetMaxCounter(_difficulty);
            uint maxCounter = 10000000;
            //Console.WriteLine("number of max repeated generating hash : " + maxCounter);
            Buffer.BlockCopy(_right_hush, 0, buffer, _difficulty, _right_hush.Length);
            for (uint i = 0; i < maxCounter; i++)
            {
                unsafe
                {
                    fixed (byte* ptr = &buffer[0])
                    {
                        *((uint*)ptr) = i;
                    }
                }
                data = AddByte(buffer, block_string);
                hash = CustomSha256.ComputeHash(data);
                if (CountLeadingZeroBits(hash, _difficulty) >= _difficulty)
                    {
                    _solution_number = new byte[4];
                    Buffer.BlockCopy(buffer, 0, _solution_number, 0, _solution_number.Length);
                    Buffer.BlockCopy(hash, 0, _right_hush, 0, _right_hush.Length);
                    string hashString = string.Empty;
                    foreach (var @byte in hash)
                    {
                        hashString += string.Format("{0:X2}", @byte);
                    }
                    return true;
                }
                
            }
            Console.WriteLine("no solution in the time of " + maxCounter + " with dificulty " + _difficulty);
            return false;
        }

        #endregion

        #region " Verification "
        public bool VerifySolution(byte[] solution)/// return one if 
        {
            if (solution == null)
            {
                Console.WriteLine("not axeptable solution : ");
            }

            return CountLeadingZeroBits(solution, _difficulty) >= _difficulty;
        }

        #endregion

        #region " Helpers "

        private static byte[] CreateNonce(int length)//number of ones
        {
            byte[] nonce = new byte[length];

            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(nonce, 0, nonce.Length - 1);

            return nonce;
        }

        private static uint GetMaxCounter(int bits)
        {
            return (uint)Math.Pow(2, bits) * 3;
        }

        private static int CountLeadingZeroBits(byte[] data, int limit)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            int zeros = 0;
            byte value = 0;

            for (int i = 0; i < data.Length; i++)
            {
                value = data[i];

                if (value == 0)// all the byte ==0 so 8 bit == 0
                {
                    zeros += 2;
                }
                else
                {
                    int count = 0;
                    if (value >> 4 == 0) { count += 1; }  // if i have 4 bit == 0 up
                    if (value << 4 == 0) { count += 1; }  // if i have 4 bit == 0 down
                    zeros += count;
                    break;
                }

                if (zeros >= limit)
                {
                    break;
                }
            }

            return zeros;
        }

        #endregion

    }
}