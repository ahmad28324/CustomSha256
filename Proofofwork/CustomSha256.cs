using System;

namespace CustomSha256
{
    public class CustomSha256
    {
        private static readonly uint[] k = 
        {
            0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
            0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
            0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
            0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
            0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
            0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
            0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
            0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2
        };

        private static readonly uint[] H = 
        {
            0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19
        };

        private static uint rotr(uint x, byte n)
        {
            return (x >> n) | (x << (32 - n));
        }

        private static uint Ch(uint x, uint y, uint z)
        {
            return (x & y) ^ ((~x) & z);
        }

        private static uint Maj(uint x, uint y, uint z)
        {
            return (x & y) ^ (x & z) ^ (y & z);
        }

        private static uint S0(uint x)
        {
            return rotr(x, 2) ^ rotr(x, 13) ^ rotr(x, 22);
        }

        private static uint S1(uint x)
        {
            return rotr(x, 6) ^ rotr(x, 11) ^ rotr(x, 25);
        }

        private static uint s0(uint x)
        {
            return rotr(x, 7) ^ rotr(x, 18) ^ (x >> 3);
        }

        private static uint s1(uint x)
        {
            return rotr(x, 17) ^ rotr(x, 19) ^ (x >> 10);
        }


        private byte[] _pendingBlock;
        private uint _pendingBlockOffset;

        private ulong _sourceDataLength;

        private uint[] _h;

        public CustomSha256()
        {
            _pendingBlock = new byte[64];
            _pendingBlockOffset = 0;
            _sourceDataLength = 0;

            _h = new uint[] 
            { 
                H[0], H[1], H[2], H[3], H[4], H[5], H[6], H[7]
            };
        }

        private void ProcessBlock(uint[] block)
        {
            // 1. Prepare the message schedule (W[t]):
            uint[] W = new uint[64];
            for (int t = 0; t < 16; t++)
            {
                W[t] = block[t];
            }

            for (int t = 16; t < 64; t++)
            {
                W[t] = s1(W[t - 2]) + W[t - 7] + s0(W[t - 15]) + W[t - 16];
            }

            // 2. Initialize the eight working variables with the (i-1)-st hash value:
            uint a = _h[0],
                   b = _h[1],
                   c = _h[2],
                   d = _h[3],
                   e = _h[4],
                   f = _h[5],
                   g = _h[6],
                   h = _h[7];

            // 3. For t=0 to 63:
            for (int t = 0; t < 64; t++)
            {
                uint T1 = h + S1(e) + Ch(e, f, g) + k[t] + W[t];
                uint T2 = S0(a) + Maj(a, b, c);
                h = g;
                g = f;
                f = e;
                e = d + T1;
                d = c;
                c = b;
                b = a;
                a = T1 + T2;
            }

            // 4. Compute the intermediate hash value H:
            _h[0] = a + _h[0];
            _h[1] = b + _h[1];
            _h[2] = c + _h[2];
            _h[3] = d + _h[3];
            _h[4] = e + _h[4];
            _h[5] = f + _h[5];
            _h[6] = g + _h[6];
            _h[7] = h + _h[7];
        }

        public void AddData(byte[] data, uint offset, uint len)
        {
            if (len == 0)
                return;

            _sourceDataLength += len * 8;

            while (len > 0)
            {
                uint amount_to_copy;

                if (len < 64)
                {
                    if (_pendingBlockOffset + len > 64)
                        amount_to_copy = 64 - _pendingBlockOffset;
                    else
                        amount_to_copy = len;
                }
                else
                {
                    amount_to_copy = 64 - _pendingBlockOffset;
                }

                Array.Copy(data, offset, _pendingBlock, _pendingBlockOffset, amount_to_copy);
                len -= amount_to_copy;
                offset += amount_to_copy;
                _pendingBlockOffset += amount_to_copy;

                if (_pendingBlockOffset == 64)
                {
                    ProcessBlock(ToUintArray(_pendingBlock));
                    _pendingBlockOffset = 0;
                }
            }
        }

        public byte[] GetHash()
        {
            ulong size_temp = _sourceDataLength;

            // 0x80 == 128 == 10000000
            AddData(new byte[1] { 0x80 }, 0, 1);

            uint available_space = 64 - _pendingBlockOffset;

            if (available_space < 8)
                available_space += 64;

            // 0-initialized
            byte[] padding = new byte[available_space];

            // Insert lenght uint64
            for (uint i = 1; i <= 8; i++)
            {
                padding[padding.Length - i] = (byte)size_temp;
                size_temp >>= 8;
            }

            AddData(padding, 0u, (uint)padding.Length);

            return ToByteArray(_h);
        }

        private static uint[] ToUintArray(byte[] src)
        {
            var resultArray = new uint[src.Length / 4];

            for (int i = 0, j = 0; i < resultArray.Length; i++, j += 4)
            {
                resultArray[i] = ((uint)src[j + 0] << 24) | ((uint)src[j + 1] << 16) | ((uint)src[j + 2] << 8) | ((uint)src[j + 3]);
            }

            return resultArray;
        }

        private static byte[] ToByteArray(uint[] src)
        {
            byte[] resultArray = new byte[src.Length * 4];
            int resultArrayIndex = 0;

            for (int i = 0; i < src.Length; i++)
            {
                resultArray[resultArrayIndex++] = (byte)(src[i] >> 24);
                resultArray[resultArrayIndex++] = (byte)(src[i] >> 16);
                resultArray[resultArrayIndex++] = (byte)(src[i] >> 8);
                resultArray[resultArrayIndex++] = (byte)src[i];
            }

            return resultArray;
        }

        public static byte[] ComputeHash(byte[] data)
        {
            var sha = new CustomSha256();
            sha.AddData(data, 0, (uint)data.Length);

            return sha.GetHash();
        }
    }
}
